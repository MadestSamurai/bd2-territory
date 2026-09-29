using System.Reflection;
using System.Reflection.Emit;
using System.Text.Json;
using BD2Territory;
using BD2Territory.Runtime;
using BD2.LocalIpc;
int checks=0;
void Check(bool ok,string text){checks++;if(!ok)throw new Exception(text);}
object Active()=>typeof(Loader).GetField("engine",BindingFlags.Static|BindingFlags.NonPublic)!.GetValue(null);
void Frame()=>UnityEngine.Canvas.Pump();
void Finish(){Frame();Thread.Sleep(220);Frame();Frame();}
Loader.Load();Check(Active()==null,"load schedules on main thread");Finish();
Check(Active()!=null&&RuntimeEngine.Starts==1,"component starts after handoff frames");
Check(LocalStorage.Status.State=="active","ready published after start");
Loader.Load();Frame();Check(RuntimeEngine.Starts==1,"same component reconnect does not duplicate");
Check(UnityEngine.Canvas.Count==0,"bootstrap callback removed after ready");
int otherStarts=0;string state="";var other=new Handoff("other-new","fishing","fishing",false,()=>otherStarts++,()=>{},()=>"",()=>{},(s,e)=>state=s);
var now=DateTime.UtcNow;
foreach(var reason in new[]{"harvest response","planting receipt","native tool action","vehicle transition","sale receipt","cooking receipt","layout receipt","snapshot writer"}){
 RuntimeEngine.Busy=reason;other.Request(now);other.Tick(now);other.Tick(now.AddMilliseconds(100));
 Check(RuntimeEngine.Paused&&Active()!=null&&otherStarts==0,"cross-tool waits for "+reason);
}
RuntimeEngine.Busy="";other.Tick(now.AddSeconds(1));other.Tick(now.AddSeconds(2));
Check(Active()==null&&otherStarts==0,"old engine stopped before replacement starts");
other.Tick(now.AddSeconds(3));Check(otherStarts==1&&state=="active","other tool takes over after drain");
Loader.Load();Finish();Check(Active()!=null&&RuntimeEngine.Starts==2,"explicit reconnect resumes territory without game restart");
Loader.Unload();Check(Active()!=null,"unload also schedules on main thread");Frame();Check(Active()==null,"unload removes engine");
Check(UnityEngine.Canvas.Count==0,"unload callback removed");
RuntimeEngine.FailStart=true;Loader.Load();Finish();Check(Active()==null&&LocalStorage.Status.State=="error","failed start cleans partial engine");
Check(UnityEngine.Canvas.Count==0,"failed start removes callback");
RuntimeEngine.FailStart=false;
AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("BD2Fishing.Runtime0.LegacyFixture"),AssemblyBuilderAccess.Run);
Loader.Load();Frame();Check(Active()==null&&LocalStorage.Status.State=="error","old unsupported component requires initial migration instead of reflective unhook");
Check(UnityEngine.Canvas.Count==0,"legacy rejection leaves no bootstrap callback");
Console.WriteLine(JsonSerializer.Serialize(new{status="passed",assertions=checks,legacyCompatibility=false,realGame=false}));
namespace UnityEngine {
 public static class Canvas { public static event Action willRenderCanvases;public static int Count=>willRenderCanvases?.GetInvocationList().Length??0;public static void Pump()=>willRenderCanvases?.Invoke(); }
}
namespace BD2Territory.Runtime {
 internal static class Build { internal const string Fingerprint="territory-handoff-fixture"; }
 internal static class LocalStorage {
  internal static string DataRoot=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"handoff-tests",Guid.NewGuid().ToString("N")));
  internal static TerritoryRuntimeStatus Status;
  internal static void WriteJsonAtomically(string path,object value){Status=(TerritoryRuntimeStatus)value;RuntimeFiles.Write(path,JsonSerializer.SerializeToUtf8Bytes(value));}
  internal static void Log(string s){}
 }
 internal class RuntimeEngine {
  internal static int Starts,Stops;internal static bool FailStart,Paused;internal static string Busy="";
  internal void Start(){if(FailStart)throw new InvalidOperationException("start failed");Paused=false;Starts++;}
  internal void PrepareHandoff(){Paused=true;}internal string HandoffBusy()=>Busy;
  internal void Stop(){Stops++;}
 }
}
