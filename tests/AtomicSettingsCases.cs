using BD2Territory;
using System.Text;
using System.Text.Json;
internal static class AtomicSettingsCases
{
 public static void Run()
 {
  var root=Path.Combine(Path.GetTempPath(),"bd2-territory-atomic-"+Guid.NewGuid().ToString("N"));
  Directory.CreateDirectory(root);var path=Path.Combine(root,"settings.json");int checks=0;
  void Check(bool value,string reason){if(!value)throw new Exception(reason);checks++;}
  void Write(int value)=>AtomicSettingsFile.Write(path,Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new{value})));
  try
  {
   Write(1);using(var doc=JsonDocument.Parse(File.ReadAllText(path)))Check(doc.RootElement.GetProperty("value").GetInt32()==1,"initial atomic settings write");
   using(var held=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read))
   {
    var writing=Task.Run(()=>Write(2));Thread.Sleep(80);
    Check(!writing.IsCompleted,"temporary reader contention waits instead of losing settings");
    held.Dispose();writing.GetAwaiter().GetResult();
   }
   using(var doc=JsonDocument.Parse(File.ReadAllText(path)))Check(doc.RootElement.GetProperty("value").GetInt32()==2,"settings saved after reader closes");
   File.SetAttributes(path,FileAttributes.ReadOnly);
   bool rejected=false;try{Write(3);}catch(Exception e)when(e is IOException or UnauthorizedAccessException){rejected=true;}
   Check(rejected,"permanent read-only restriction remains an error");File.SetAttributes(path,FileAttributes.Normal);
   using(var doc=JsonDocument.Parse(File.ReadAllText(path)))Check(doc.RootElement.GetProperty("value").GetInt32()==2,"failed save preserves last complete settings");
   var heldAgain=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read);
   try
   {
    var elapsed=System.Diagnostics.Stopwatch.StartNew();rejected=false;try{Write(4);}catch(Exception e)when(e is IOException or UnauthorizedAccessException){rejected=true;}
    Check(rejected&&elapsed.ElapsedMilliseconds<1500,"permanent sharing conflict cannot wait forever");
   }
   finally{heldAgain.Dispose();}
   Check(Directory.GetFiles(root,"*.tmp").Length==0,"failed writes leave no temporary settings");
   using var cancellation=new CancellationTokenSource();int reads=0;
   var reader=Task.Run(()=>{while(!cancellation.IsCancellationRequested){try{using var stream=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.ReadWrite|FileShare.Delete);using var document=JsonDocument.Parse(stream);Interlocked.Increment(ref reads);}catch(Exception e)when(e is IOException or UnauthorizedAccessException){Thread.Yield();}}});
   try{for(int i=10;i<60;i++)Write(i);}finally{cancellation.Cancel();reader.GetAwaiter().GetResult();}
   Check(reads>0,"concurrent readers see complete JSON during replacement");
   Console.WriteLine($"Atomic settings checks passed: {checks}");
  }
  finally{if(File.Exists(path))File.SetAttributes(path,FileAttributes.Normal);if(!Path.GetFullPath(root).StartsWith(Path.GetFullPath(Path.GetTempPath()),StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Unexpected test path");Directory.Delete(root,true);}
 }
}
