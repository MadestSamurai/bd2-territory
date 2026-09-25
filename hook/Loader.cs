using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Json;
using System.Runtime.CompilerServices;
using HarmonyLib;
namespace BD2Territory.Runtime
{
 public static class Loader
 {
  private static object engine;private static bool resolver,pending;private static DateTime requested,idleSince;
  private static object[] retiring=new object[0];private static bool retired;
  private static class Pump {internal static readonly Harmony Patch=new Harmony("bd2.territory.bootstrap."+typeof(Loader).Assembly.GetName().Name);}
  [MethodImpl(MethodImplOptions.NoInlining)] private static void Arm()=>Pump.Patch.Patch(typeof(GameCameraManager).GetMethod("LateUpdate",BindingFlags.Instance|BindingFlags.NonPublic),postfix:new HarmonyMethod(typeof(Loader),nameof(ActivateOnFrame)));
  public static void Load()
  {
   lock(typeof(Loader))
   {
    if(!resolver){AppDomain.CurrentDomain.AssemblyResolve+=Resolve;resolver=true;}
    if(engine!=null){WriteStatus("active","");return;}if(pending)return;
    pending=true;retired=false;idleSince=DateTime.MinValue;requested=DateTime.UtcNow;
    try{Arm();}
    catch(Exception e){pending=false;WriteStatus("error",e.GetBaseException().Message);}
   }
  }
  private static object Field(object target,string name)=>target.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(target);
  private static bool ControlActive()
  {
   var path=Path.Combine(LocalStorage.DataRoot,"control.json");if(!File.Exists(path))return false;
   try{using(var f=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.ReadWrite|FileShare.Delete)){var c=(TerritoryControl)new DataContractJsonSerializer(typeof(TerritoryControl)).ReadObject(f);return c.Valid(DateTime.UtcNow.Ticks,System.Diagnostics.Process.GetCurrentProcess().Id);}}
   catch{return true;} // A torn/unreadable control file is not evidence of a pause.
  }
  private static bool SafeToRetire(object old)
  {
   var t=old.GetType();var tool=Field(old,"tool");var progress=Field(old,"progress");var net=Field(old,"network");
   bool busy=tool!=null&&(bool)TerritoryBindings.Read("Tool.Busy",tool);
   bool loading=tool!=null&&(bool)t.GetMethod("ToolLoading",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(old,null);
   bool network=(bool)net.GetType().GetProperty("Waiting",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(net,null)||Convert.ToInt32(TerritoryBindings.Read("Network.QueuedHarvest",null))>0;
   bool planting=progress!=null&&!string.IsNullOrEmpty((string)progress.GetType().GetProperty("PendingToken").GetValue(progress,null));
   var vehicle=t.GetField("vehiclePending",BindingFlags.Instance|BindingFlags.NonPublic);loading|=vehicle!=null&&(bool)vehicle.GetValue(old);
   var sales=t.GetMethod("SalesBusy",BindingFlags.Instance|BindingFlags.NonPublic);loading|=sales!=null&&(bool)sales.Invoke(old,null);
   var cook=t.GetMethod("CookingBusy",BindingFlags.Instance|BindingFlags.NonPublic);loading|=cook!=null&&(bool)cook.Invoke(old,null);
   var layout=t.GetMethod("LayoutBusy",BindingFlags.Instance|BindingFlags.NonPublic);loading|=layout!=null&&(bool)layout.Invoke(old,null);
   return RuntimeHandoffRules.CanStop(ControlActive(),(bool)Field(old,"ownsMove"),busy,loading,network,planting);
  }
  private static void ActivateOnFrame()
  {
   if(!pending)return;
   try
   {
    if((DateTime.UtcNow-requested).TotalSeconds>20)throw new InvalidOperationException("组件更新等待空闲超时。请暂停自动化，等待采集／播种结算后再次连接，无需重启游戏。");
    var own=typeof(Loader).Assembly;
    if(!retired)
    {
     TerritoryBindings.ValidateCompiledClient();TerritoryBindings.Validate();
     var assemblies=AppDomain.CurrentDomain.GetAssemblies();
     var foreign=assemblies.FirstOrDefault(a=>new[]{"BD2Fishing.Runtime","BD2Sichuan.Runtime","BD2ArenaDefenseWatcher.Active.Runtime","BD2ReplayCaptureHook."}.Any(p=>(a.GetName().Name??"").StartsWith(p,StringComparison.Ordinal)));
     if(foreign!=null)throw new InvalidOperationException("游戏内存在其他工具模块，不能作为领地组件更新："+foreign.GetName().Name);
     var loaders=assemblies.Where(a=>a!=own&&(a.GetName().Name??"").StartsWith("BD2Territory.Runtime",StringComparison.Ordinal)).Select(a=>a.GetType("BD2Territory.Runtime.Loader")).Where(t=>t!=null).ToArray();
     retiring=loaders.Select(t=>t.GetField("engine",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null)).Where(e=>e!=null).ToArray();
     if(retiring.Length>0)
     {
      if(ControlActive()||retiring.Any(e=>!SafeToRetire(e))){idleSince=DateTime.MinValue;return;}
      if(idleSince==DateTime.MinValue){idleSince=DateTime.UtcNow;return;}
      if((DateTime.UtcNow-idleSince).TotalMilliseconds<600)return;
     }
     foreach(var old in retiring)
     {
      old.GetType().GetMethod("Release",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(old,null);
      old.GetType().Assembly.GetType("BD2Territory.Runtime.Loader").GetMethod("Unload").Invoke(null,null);
     }
     retired=true;return; // Let any already executing old I/O callback leave before the new writer starts.
    }
    if(retiring.Any(e=>Convert.ToInt32(Field(e,"ioBusy"))!=0))return;
    if(ControlActive())throw new InvalidOperationException("组件更新时自动化被重新开启；已保持暂停，请关闭旧工具后重试连接。");
    var next=Activator.CreateInstance(own.GetType("BD2Territory.Runtime.RuntimeEngine",true),true);
    engine=next;next.GetType().GetMethod("Start",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(next,null);
    LocalStorage.Log("组件已在同一游戏进程更新 old="+retiring.Length+" new="+own.GetName().Name+" pid="+System.Diagnostics.Process.GetCurrentProcess().Id);
    pending=false;Pump.Patch.UnpatchAll(Pump.Patch.Id);WriteStatus("active","");
   }
   catch(Exception e)
   {
    pending=false;try{if(engine!=null)engine.GetType().GetMethod("Stop",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(engine,null);}catch{}engine=null;
    Pump.Patch.UnpatchAll(Pump.Patch.Id);WriteStatus("error",e.GetBaseException().Message);
   }
  }
  // Called on the game's main thread by a replacement loader after all native actions have settled.
  public static void Unload()
  {lock(typeof(Loader)){pending=false;Pump.Patch.UnpatchAll(Pump.Patch.Id);if(engine!=null)engine.GetType().GetMethod("Stop",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(engine,null);engine=null;WriteStatus("inactive","");}}
  internal static void WriteStatus(string state,string error)
  {try{LocalStorage.WriteJsonAtomically(Path.Combine(LocalStorage.DataRoot,"runtime.json"),new TerritoryRuntimeStatus{State=state,Error=error,Runtime=TerritoryIdentity.RuntimeName,AtUtc=DateTime.UtcNow.ToString("O"),ProcessId=System.Diagnostics.Process.GetCurrentProcess().Id});}catch(Exception e){LocalStorage.Log(e.Message);}}
  private static Assembly Resolve(object sender,ResolveEventArgs args)
  {if(new AssemblyName(args.Name).Name!="0Harmony")return null;using(var s=typeof(Loader).Assembly.GetManifestResourceStream("BD2Territory.Harmony.dll"))using(var b=new MemoryStream()){s.CopyTo(b);return Assembly.Load(b.ToArray());}}
 }
}
