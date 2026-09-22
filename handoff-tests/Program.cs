using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using BD2Territory;
using BD2Territory.Runtime;
using HarmonyLib;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

int checks=0;
void Check(bool ok,string message){checks++;if(!ok)throw new Exception(message);}
const BindingFlags instance=BindingFlags.Instance|BindingFlags.NonPublic;
const BindingFlags stat=BindingFlags.Static|BindingFlags.NonPublic;
void Set(object obj,string field,object value)=>obj.GetType().GetField(field,instance).SetValue(obj,value);
object Get(object obj,string field)=>obj.GetType().GetField(field,instance).GetValue(obj);
object Active(Type t)=>t.GetField("engine",stat).GetValue(null);
void Frame()=>GameCameraManager.Pump();
Directory.CreateDirectory(LocalStorage.DataRoot);
void Control(bool active){File.WriteAllText(Path.Combine(LocalStorage.DataRoot,"control.json"),JsonSerializer.Serialize(new TerritoryControl{OwnerId="fixture",Enabled=active,ProcessId=Environment.ProcessId,UntilUtcTicks=DateTime.UtcNow.AddSeconds(10).Ticks}));}
var oldSource="""
using System;
namespace BD2Territory.Runtime {
 public class FakeTool { public bool Busy {get;set;} }
 public class FakeProgress { public string PendingToken {get;set;}=""; }
 public class FakeNetwork { internal bool Waiting {get;set;} }
 public class OldEngine {
  private object tool=new FakeTool();private object progress=new FakeProgress();private object network=new FakeNetwork();
  private bool layoutBusy;private bool LayoutBusy()=>layoutBusy;private bool ownsMove;private bool loading;private bool vehiclePending;private int ioBusy;public bool stopped;public int releases;
  private bool ToolLoading()=>loading;private void Release(){releases++;ownsMove=false;}internal void Stop(){stopped=true;}
 }
 public static class Loader {
  private static object engine=new OldEngine();public static void Unload(){((OldEngine)engine).Stop();engine=null;}
 }
}
""";
var refs=((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")).Split(Path.PathSeparator).Select(p=>MetadataReference.CreateFromFile(p));
var compilation=CSharpCompilation.Create("BD2Territory.Runtime6.Fixture",new[]{CSharpSyntaxTree.ParseText(oldSource)},refs,new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
using var buffer=new MemoryStream();var emit=compilation.Emit(buffer);Check(emit.Success,"legacy fixture compiles");
var oldAssembly=Assembly.Load(buffer.ToArray());var oldLoader=oldAssembly.GetType("BD2Territory.Runtime.Loader");var old=Active(oldLoader);
var newLoader=typeof(Loader);var tool=Get(old,"tool");var net=Get(old,"network");var progress=Get(old,"progress");
Control(true);Loader.Load();Frame();Check(Active(oldLoader)==old&&Active(newLoader)==null,"active control must not retire old engine");
Control(false);tool.GetType().GetProperty("Busy").SetValue(tool,true);Frame();Check(Active(oldLoader)==old,"busy tool blocks handoff");tool.GetType().GetProperty("Busy").SetValue(tool,false);
Set(old,"loading",true);Frame();Check(Active(oldLoader)==old,"pending async tool load blocks handoff");Set(old,"loading",false);
Set(old,"vehiclePending",true);Frame();Check(Active(oldLoader)==old,"pending mount callback blocks handoff even after control is paused");Set(old,"vehiclePending",false);
net.GetType().GetProperty("Waiting",instance).SetValue(net,true);Frame();Check(Active(oldLoader)==old,"pending server response blocks handoff");net.GetType().GetProperty("Waiting",instance).SetValue(net,false);
progress.GetType().GetProperty("PendingToken").SetValue(progress,"unresolved");Frame();Check(Active(oldLoader)==old,"unknown planting transaction blocks handoff");progress.GetType().GetProperty("PendingToken").SetValue(progress,"");
Set(old,"layoutBusy",true);Frame();Check(Active(oldLoader)==old,"layout preview or unconfirmed placement blocks handoff");Set(old,"layoutBusy",false);
Set(old,"ownsMove",true);Frame();Check(Active(oldLoader)==old,"movement must be released before handoff");Set(old,"ownsMove",false);
File.WriteAllText(Path.Combine(LocalStorage.DataRoot,"control.json"),"broken");Frame();Check(Active(oldLoader)==old,"unreadable control is not an acknowledged pause");Control(false);
TerritoryBindings.Queued=1;Frame();Check(Active(oldLoader)==old,"unsent native harvest queue blocks handoff");TerritoryBindings.Queued=0;
Frame();Check(Active(oldLoader)==old,"one idle frame is not sufficient for handoff");typeof(Loader).GetField("idleSince",stat).SetValue(null,DateTime.UtcNow.AddSeconds(-1));
Set(old,"ioBusy",1);Frame();Check(Active(oldLoader)==null&&(bool)old.GetType().GetField("stopped").GetValue(old),"idle legacy engine retired on game frame");Check(Active(newLoader)==null,"new writer cannot start in retirement frame");
Frame();Check(Active(newLoader)==null,"wait for old in-flight IO writer");Set(old,"ioBusy",0);Frame();Check(Active(newLoader)!=null&&RuntimeEngine.Starts==1,"exactly one replacement engine starts after IO drain");
Check(LocalStorage.Status.State=="active","successful handoff published");Loader.Load();Frame();Check(RuntimeEngine.Starts==1,"repeat connect cannot start duplicate engine");
Loader.Unload();Check(Active(newLoader)==null&&RuntimeEngine.Stops==1,"unload stops replacement");Loader.Load();Frame();Frame();Check(RuntimeEngine.Starts==2,"inactive legacy assemblies do not prevent another update");Loader.Unload();
RuntimeEngine.FailStart=true;Loader.Load();Frame();Frame();Check(Active(newLoader)==null&&LocalStorage.Status.State=="error","failed new start leaves no active replacement");
Check(!Harmony.GetAllPatchedMethods().Any(m=>Harmony.GetPatchInfo(m).Owners.Any(x=>x.StartsWith("bd2.territory.bootstrap"))),"bootstrap callbacks removed after success and failure");
Console.WriteLine(JsonSerializer.Serialize(new{status="passed",assertions=checks,legacy="Runtime6 fixture",realGame=false,output=LocalStorage.DataRoot}));

public class GameCameraManager {
 [MethodImpl(MethodImplOptions.NoInlining)] private void LateUpdate() { GC.KeepAlive(this); }
 public static void Pump()=>new GameCameraManager().LateUpdate();
}
namespace BD2Territory.Runtime {
 internal static class LocalStorage {
  internal static string DataRoot=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"handoff-tests",Guid.NewGuid().ToString("N")));
  internal static TerritoryRuntimeStatus Status;
  internal static void WriteJsonAtomically(string path,object value){Status=(TerritoryRuntimeStatus)value;File.WriteAllText(path,JsonSerializer.Serialize(value));}
  internal static void Log(string s){File.AppendAllText(Path.Combine(DataRoot,"log.txt"),s+Environment.NewLine);}
 }
 internal static class TerritoryBindings {
  internal static void ValidateCompiledClient(){}internal static void Validate(){}
  internal static int Queued;
  internal static object Read(string key,object value)=>key=="Network.QueuedHarvest"?Queued:value.GetType().GetProperty("Busy").GetValue(value);
 }
 internal class RuntimeEngine {
  internal static int Starts,Stops;internal static bool FailStart;private int ioBusy;
  internal void Start(){if(FailStart)throw new InvalidOperationException("start failed");Starts++;}
  internal void Stop(){Stops++;}private void Release(){}
 }
}
