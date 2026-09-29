using BD2Territory;
using BD2Territory.Compatibility;
using SharpMonoInjector;
if(args.Length==2&&args[0]=="connect-check"){
 var output=Path.GetFullPath(args[1]);Directory.CreateDirectory(output);
 try{
  using var connectedGame=TerritoryConnection.FindGame();var start=connectedGame.StartTime.ToUniversalTime().Ticks;
  // Refuse to displace an active automation lease; reading does not acquire its ownership.
  foreach(var name in new[]{"BD2Territory","BD2ApostleDefensePrivate","BD2ApostleDefense","BD2Fishing","BD2Daily"}){
   try{var peer=new BD2.LocalIpc.PipeClient(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),name),connectedGame.Id,start);var bytes=peer.Read("control.json");if(bytes!=null){using var d=System.Text.Json.JsonDocument.Parse(bytes);var enabled=d.RootElement.TryGetProperty("Enabled",out var flag)&&flag.ValueKind==System.Text.Json.JsonValueKind.True;if(enabled)throw new InvalidOperationException("Active automation: "+name);}}
   catch(IOException){}catch(TimeoutException){}
  }
  string message=new TerritoryConnection().Connect(Console.WriteLine);int samples=0;long previous=0;TerritorySnapshot? latest=null;
  var deadline=DateTime.UtcNow.AddSeconds(5);
  while(DateTime.UtcNow<deadline){var value=TerritoryJson.Read<TerritorySnapshot>(Path.Combine(TerritoryIdentity.DataRoot,"latest.json"));if(TerritoryControlLink.Fresh(value,DateTime.UtcNow)&&value!.ProcessId==connectedGame.Id&&value.CapturedUtcTicks>previous){previous=value.CapturedUtcTicks;samples++;latest=value;}await Task.Delay(200);}
  if(samples<5||latest==null||latest.Error.Length>0)throw new InvalidOperationException("Fresh connection snapshots missing or in error: "+latest?.Error);
  TerritoryJson.Write(Path.Combine(output,"live-check.json"),new{status="passed",message,connectedGame.Id,start,latest.Runtime,latest.Ready,latest.Scene,latest.Reason,samples,automationStarted=false,atUtc=DateTime.UtcNow});
  Console.WriteLine("PASS "+latest.Runtime+" samples="+samples+" ready="+latest.Ready);
 }catch(Exception ex){TerritoryJson.Write(Path.Combine(output,"live-check.json"),new{status="failed",error=ex.ToString(),automationStarted=false});Console.Error.WriteLine(ex);Environment.ExitCode=1;}return;
}
if(args.Length==1&&args[0]=="connect"){Console.WriteLine(new TerritoryConnection().Connect(Console.WriteLine));return;}
if(args.Length!=1 || args[0]!="inspect")throw new ArgumentException("inspect only");
using var game=TerritoryConnection.FindGame();
var control=TerritoryJson.Read<TerritoryControl>(Path.Combine(TerritoryIdentity.DataRoot,"control.json"));
// This command only reads scene state on the main thread; it does not start an engine.
var exe=game.MainModule!.FileName;var managed=Path.Combine(Path.GetDirectoryName(exe)!,Path.GetFileNameWithoutExtension(exe)+"_Data","Managed");
var prepared=HookCompiler.Prepare(managed);
using var injector=new Injector(game.Id);
injector.Inject(prepared.Payload,"BD2Territory.Runtime","SceneProbe","Inspect");
Console.WriteLine("Read-only scene probe scheduled for game main thread");
