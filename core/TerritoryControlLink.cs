using System.Text.Json;
namespace BD2Territory;
public static class TerritoryJson
{
 public static T? Read<T>(string path) where T:class
 {try{using var s=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.ReadWrite|FileShare.Delete);return JsonSerializer.Deserialize<T>(s);}catch(Exception e)when(e is IOException or UnauthorizedAccessException or JsonException){return null;}}
 public static void Write<T>(string path,T value)
 {var dir=Path.GetDirectoryName(path)!;Directory.CreateDirectory(dir);var tmp=Path.Combine(dir,Guid.NewGuid().ToString("N")+".tmp");try{File.WriteAllText(tmp,JsonSerializer.Serialize(value));File.Move(tmp,path,true);}finally{if(File.Exists(tmp))File.Delete(tmp);}}
}
public sealed class TerritoryControlLink:IDisposable
{
 private readonly object sync=new();private readonly Timer timer;private readonly string root;
 private TerritoryControl command=new();private bool disposed;
 public string Error {get;private set;}="";
 public string OwnerId {get{lock(sync)return command.OwnerId;}}
 public bool Enabled {get{lock(sync)return command.Enabled;}}
 public TerritoryControlLink(string root){this.root=root;timer=new(_=>Pulse(),null,500,500);}
 public void Configure(TerritorySettings s)
 {
  if(!s.ValidSettings())throw new ArgumentException("操作间隔应为 100–60000 毫秒，播种预算不能为负。");
  lock(sync){command.Cooking=s.Cooking;command.CookingBatch=s.CookingBatch;command.RecipeId=s.RecipeId;command.Logging=s.Logging;command.Mining=s.Mining;command.Farming=s.Farming;command.DashRecovery=s.DashRecovery;command.UseVehicle=s.UseVehicle;command.UseNavMesh=s.UseNavMesh;command.IntervalMs=s.IntervalMs;command.PlantingBudget=s.PlantingBudget;TerritoryJson.Write(Path.Combine(root,"settings.json"),s);Write();}
 }
 public void Start(int pid,string layoutToken=""){lock(sync){if(disposed)throw new ObjectDisposedException(nameof(TerritoryControlLink));command.LayoutToken=layoutToken;command.OwnerId=Guid.NewGuid().ToString("N");command.ProcessId=pid;command.Enabled=true;try{Write();}catch{command.Enabled=false;throw;}}}
 public void Stop(){lock(sync){command.Enabled=false;Write();}}
 private void Write(){command.UntilUtcTicks=command.Enabled?DateTime.UtcNow.AddSeconds(10).Ticks:0;TerritoryJson.Write(Path.Combine(root,"control.json"),command);Error="";}
 private void Pulse(){lock(sync){if(disposed || !command.Enabled)return;try{Write();}catch(Exception e)when(e is IOException or UnauthorizedAccessException){Error=e.Message;}}}
 public void Dispose(){lock(sync){if(disposed)return;disposed=true;timer.Dispose();try{Stop();}catch(Exception e){Error=e.Message;}}}
 public static bool Fresh(TerritorySnapshot? s,DateTime now)=>s!=null && s.Schema==1 && s.Runtime==TerritoryIdentity.RuntimeName && s.ProcessId>0 && s.CapturedUtcTicks<=now.AddSeconds(2).Ticks && s.CapturedUtcTicks>=now.AddSeconds(-3).Ticks;
}
