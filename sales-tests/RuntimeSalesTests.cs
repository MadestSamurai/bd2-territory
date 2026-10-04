using BD2Territory;
using System.Text.Json;
namespace Proto.Net {
 public class UserDBInfo {public long OwnerIndex=1;}
 public class ItemDBInfo {public long InvenIndex=1;public int Id=10,Count=7000,Type=66,KeepFlag;public bool IsDisableSlot;public ItemDBInfo Clone()=>(ItemDBInfo)MemberwiseClone();}
 public class SellItemInfo {public long InvenIndex;public int GroupId,Id,SellCount;}
}
namespace Proto.Design.common {
 public class LifeSellItemTable {public int GroupId=1,ItemType=66,PriceType=64,PriceId,ItemCount=1,PriceCount=3,ItemId=10,Id=2;}
 public class LifeItemTable {public int Type=1;}
}
namespace BD2Territory.Runtime {
 using Proto.Net;using Proto.Design.common;
 internal static class TerritoryBindings {
  internal static int Refreshes,Sales;internal static List<SellItemInfo> Request;internal static ItemDBInfo Stock=new();internal static long Currency=1000;internal static UserDBInfo User=new();
  internal static object Read(string role,object owner)=>role=="Account.User"?User:new[]{Stock};
  internal static object EnumObject(string a,string b)=>b;
  internal static object Invoke(string role,params object[] args){switch(role){case "Sales.Refresh":Refreshes++;return null;case "Sales.Send":Sales++;Request=(List<SellItemInfo>)args[0];return null;case "Inventory.Currency":return Currency;case "Sales.Rows":return new[]{new LifeSellItemTable()};case "Tables.ItemById":return new LifeItemTable();default:throw new Exception(role);}}
 }
 internal static class LocalStorage {
  internal static string DataRoot=Path.Combine(Path.GetTempPath(),"BD2Territory-sales-test-"+Guid.NewGuid().ToString("N"));
  internal static Dictionary<string,string> Files=new();internal static List<string> Logs=new();
  internal static void WriteJsonAtomically(string path,object value){Files[path]=JsonSerializer.Serialize(value);}
  internal static void Log(string text)=>Logs.Add(text);
 }
 internal sealed class TerritoryNetwork {
  internal bool NativeBusy,Waiting,OtherPending;internal int GatherReplies;internal SalesReply LastSaleReply;internal SalesSnapshot LastSalesSnapshot;internal string Armed="";
  internal bool BeginSalesRefresh(SalesProgress intent)=>!NativeBusy&&!OtherPending;
  internal void FinishSalesRecovery(string token){Waiting=false;}
  internal void ArmSales(SalesProgress intent){Armed=intent.Token;Waiting=true;}
 }
 internal sealed partial class RuntimeEngine {
  private TerritoryNetwork network=new();private string account="1";private int pid=42;private long lastInput,lastCrops,lastCatalog,lastWorldRead;
  private TerritoryControl control=new(){Enabled=true,OwnerId="owner",ProcessId=42,UntilUtcTicks=DateTime.UtcNow.AddSeconds(10).Ticks};
  private void StopMotion(){}
  private T ReadLayoutFile<T>(string path)=>LocalStorage.Files.TryGetValue(path,out var text)?JsonSerializer.Deserialize<T>(text):default;
  internal static int RunTests(){
   int count=0;void Check(bool yes,string text){count++;if(!yes)throw new Exception(text);}
   long now=DateTime.UtcNow.Ticks;
   SalesProgress Stale()=>new(){Account="1",Token="old",State="pending",Threshold=9900,SubmittedTicks=now-TimeSpan.FromDays(3).Ticks,CurrencyBefore=1000,Lines=new[]{new SaleLine{Index=1,Item=10,Before=9999,Count=99,Price=3,Group=1,Row=2}}};
   RuntimeEngine Load(){var e=new RuntimeEngine();LocalStorage.WriteJsonAtomically(Path.Combine(LocalStorage.DataRoot,"sales-1.json"),Stale());e.LoadSales("1");return e;}
   SalesSnapshot Snapshot()=>new(){Token="old",Account="1",Accepted=true,RequestedTicks=now,CapturedTicks=now+1,Currency=1200,Items=new[]{new SaleStock{Index=1,Item=10,Count=7000}}};
   var e=Load();var s=new TerritorySnapshot();e.network.NativeBusy=true;e.SalesTick(s,e.control,now);
   Check(TerritoryBindings.Refreshes==0&&TerritoryBindings.Sales==0,"do not race in-flight native sale");
   e.network.NativeBusy=false;e.SalesTick(s,e.control,now);Check(TerritoryBindings.Refreshes==1&&TerritoryBindings.Sales==0,"stale journal triggers read only refresh");
   e.SalesTick(s,e.control,now+1);Check(TerritoryBindings.Refreshes==1,"do not poll refresh every frame");
   e.network.LastSalesSnapshot=Snapshot();e.network.NativeBusy=true;e.SalesTick(s,e.control,now+2);Check(e.sales.Pending,"do not resume while the refresh is applying");
   e.network.NativeBusy=false;e.SalesTick(s,e.control,now+3);Check(!e.sales.Pending&&e.sales.State=="reconciled","unknown sale recovered");
   Check(LocalStorage.Files.Count(p=>p.Key.Contains("sales-history"))==1,"preserve recovery evidence");
   Check(e.sales.ConfirmedItems==0&&e.sales.ConfirmedCurrency==0,"do not claim unknown sale succeeded");
   e.SalesTick(s,e.control,now+4);Check(TerritoryBindings.Sales==0,"manual removal leaves no surplus, no sale");
   var restart=new RuntimeEngine();restart.LoadSales("1");Check(!restart.sales.Pending,"restart does not restore stale blockage");
   TerritoryBindings.Stock.Count=9910;e.SalesTick(s,e.control,now+TimeSpan.FromSeconds(6).Ticks);
   Check(TerritoryBindings.Sales==1&&TerritoryBindings.Request.Single().SellCount==10&&e.sales.Token!="old","new excess replanned with new identity and correct reserve");
   e.network.LastSaleReply=new(){Token=e.sales.Token,Matches=true,Accepted=true,Reward=30};TerritoryBindings.Stock.Count=6000;TerritoryBindings.Currency=2000;e.SettleSales();
   Check(!e.sales.Pending&&!e.sales.BalancesMatched&&e.sales.ConfirmedItems==10,"accepted delayed receipt survives unrelated stock changes");
   e.SettleSales();Check(e.sales.ConfirmedItems==10,"accepted receipt counted once");
   e=Load();int previous=TerritoryBindings.Refreshes;e.network.OtherPending=true;e.SalesTick(s,e.control,now);Check(TerritoryBindings.Refreshes==previous,"other territory transaction blocks refresh");
   e.network.OtherPending=false;e.SalesTick(s,e.control,now);e.network.LastSalesSnapshot=new(){Token="old",Account="1",Accepted=false};e.SalesTick(s,e.control,now+TimeSpan.FromSeconds(31).Ticks);
   Check(e.sales.Pending&&TerritoryBindings.Refreshes==previous+2&&TerritoryBindings.Sales==1,"failed read retries only the read, never the sale");
   e.network.LastSalesSnapshot=Snapshot();e.network.LastSalesSnapshot.Token="wrong";e.SalesTick(s,e.control,now+TimeSpan.FromSeconds(32).Ticks);Check(e.sales.Pending,"stale refresh for another transaction rejected");
   e.network.LastSalesSnapshot=Snapshot();TerritoryBindings.User.OwnerIndex=2;bool rejected=false;try{e.SalesTick(s,e.control,now+TimeSpan.FromSeconds(33).Ticks);}catch(InvalidOperationException){rejected=true;}Check(rejected&&e.sales.Pending,"cross-account refresh rejected");
   return count;
  }
 }
}