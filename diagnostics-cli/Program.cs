using BD2Territory;
using BD2Territory.Compatibility;
using SharpMonoInjector;
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
