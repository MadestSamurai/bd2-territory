using BD2Territory.Compatibility;
using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using SharpMonoInjector;
namespace BD2Territory;
public sealed class TerritoryConnectionState
{
    public int ProcessId {get;set;}
    public long ProcessStartTicks {get;set;}
    public string HookSha256 {get;set;}="";
    public string ToolFingerprint {get;set;}="";
    public long Address {get;set;}
}
public sealed class TerritoryConnection
{
    private readonly string root;
    public TerritoryConnection(string? root=null){this.root=root??TerritoryIdentity.DataRoot;}
    public static bool SameProcess(TerritoryConnectionState s,int pid,long start)=>s.ProcessId==pid&&s.ProcessStartTicks==start;
    public static bool Fresh(TerritoryRuntimeStatus? status,int pid,DateTime since)=>status!=null&&status.ProcessId==pid&&status.Runtime==TerritoryIdentity.RuntimeName&&DateTime.TryParse(status.AtUtc,null,DateTimeStyles.RoundtripKind,out var when)&&when>=since&&when<=DateTime.UtcNow.AddSeconds(2);
    public string Connect(Action<string>? progress=null)
    {
        using var game=FindGame();int pid=game.Id;long start=game.StartTime.ToUniversalTime().Ticks;
        string fingerprint=HookCompiler.ToolFingerprint;var path=Path.Combine(root,"connection.json");
        var old=TerritoryJson.Read<TerritoryConnectionState>(path);
        if(old!=null&&SameProcess(old,pid,start))
        {
            var report=TerritoryJson.Read<TerritoryRuntimeStatus>(Path.Combine(root,"runtime.json"));
            if(old.ToolFingerprint==fingerprint&&Fresh(report,pid,DateTime.UtcNow.AddSeconds(-5))&&report!.State=="active")return $"已连接游戏 {pid} · 领地独立组件";
            var control=TerritoryJson.Read<TerritoryControl>(Path.Combine(root,"control.json"));
            if(control==null||control.Valid(DateTime.UtcNow.Ticks,pid))throw new InvalidOperationException("请先暂停旧领地工具，等待当前采集／播种结算，再点击连接更新组件；游戏可以保持打开。");
            if(old.ToolFingerprint==fingerprint&&old.Address==0&&report?.Runtime!=TerritoryIdentity.RuntimeName)throw new InvalidOperationException("上次连接尚无确定结果，请等待组件状态，避免重复注入。");

        }
        // Resolve the required interfaces and compile against installed metadata before any injection.
        string exe;
        try{exe=game.MainModule?.FileName??throw new InvalidOperationException("无法读取游戏路径。");}
        catch(System.ComponentModel.Win32Exception e) when(e.NativeErrorCode==5){throw new InvalidOperationException("Windows 拒绝访问游戏进程。请用与游戏相同的权限打开领地工具；若游戏以管理员权限运行，也请以管理员身份运行本工具。无需重启游戏。",e);}
        var client=Path.Combine(Path.GetDirectoryName(exe)!,Path.GetFileNameWithoutExtension(exe)+"_Data","Managed","Assembly-CSharp.dll");
        progress?.Invoke("正在识别领地接口并生成适配组件，首次连接可能需要数秒…");
        PreparedHook prepared;
        try { prepared=HookCompiler.Prepare(Path.GetDirectoryName(client)!);TerritoryJson.Write(Path.Combine(root,"compatibility.json"),prepared.Report); }
        catch(CompatibilityException ex) { TerritoryJson.Write(Path.Combine(root,"compatibility.json"),ex.Report);throw; }
        catch(Exception ex) { TerritoryJson.Write(Path.Combine(root,"compatibility.json"),new{Status="unsupported",Error=ex.Message,Injection=false});throw; }
        var payload=prepared.Payload;string sha=Convert.ToHexString(SHA256.HashData(payload));
        if(game.HasExited || game.StartTime.ToUniversalTime().Ticks!=start)throw new InvalidOperationException("游戏进程已变化，请重新连接。");
        progress?.Invoke("接口检查通过，正在连接／更新领地组件，等待原动作结算…");
        var state=new TerritoryConnectionState{ProcessId=pid,ProcessStartTicks=start,HookSha256=sha,ToolFingerprint=fingerprint};
        using var injector=new Injector(pid);
        TerritoryJson.Write(path,state); // In-flight marker prevents a blind duplicate load after an ambiguous injector failure.
        var attempt=DateTime.UtcNow;
        state.Address=injector.Inject(payload,"BD2Territory.Runtime","Loader","Load").ToInt64();
        TerritoryJson.Write(path,state);
        for(int i=0;i<250;i++)
        {
            var report=TerritoryJson.Read<TerritoryRuntimeStatus>(Path.Combine(root,"runtime.json"));
            if(Fresh(report,pid,attempt))
            {
                if(report!.State=="error")throw new InvalidOperationException(report.Error);
                if(report.State=="active")return $"已连接游戏 {pid} · 领地独立组件";
            }
            Thread.Sleep(100);
        }
        throw new InvalidOperationException("组件尚未返回连接状态，请确认游戏已完成加载且旧工具已暂停，再次点击连接查看结果。");
    }
    public static Process FindGame()
    {
        var games=new List<Process>();
        foreach(var p in Process.GetProcesses())try{if(TerritoryIdentity.IsGameProcessName(p.ProcessName))games.Add(p);else p.Dispose();}catch{p.Dispose();}
        if(games.Count==1)return games[0];foreach(var p in games)p.Dispose();
        throw new InvalidOperationException(games.Count==0?"请先启动 BrownDust II，再点击连接游戏。":"检测到多个游戏实例，请只保留需要操作的一个。");
    }
}
