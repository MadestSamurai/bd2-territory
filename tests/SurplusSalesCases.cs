using BD2Territory;
static class SurplusSalesCases
{
 public static void Run()
 {
  int n=0;void C(bool v,string m){n++;if(!v)throw new Exception(m);}void Reject(Action a){bool rejected=false;try{a();}catch(ArgumentException){rejected=true;}catch(InvalidOperationException){rejected=true;}C(rejected,"unsafe sale accepted");}
  SaleStock Stock(long index=1,int count=9999)=>new(){Index=index,Item=10,Count=count,Kind=66,Group=1,Row=2,UnitCount=1,Price=3,Eligible=true};
  foreach(int threshold in new[]{100,101,5000,9899,9900}){var p=SurplusSales.Plan(new[]{Stock()},threshold);C(p.Sum(x=>x.Count)==9999-threshold,"only surplus sold");C(p[0].Before-p[0].Count==threshold,"reserve retained");}
  foreach(int threshold in new[]{-1,0,99,9901,int.MaxValue})Reject(()=>SurplusSales.Plan(new[]{Stock()},threshold));
  C(SurplusSales.Plan(new[]{Stock(1,9900)},9900).Length==0,"at threshold does not sell");
  var locked=Stock();locked.Locked=true;C(SurplusSales.Plan(new[]{locked},100).Length==0,"locked inventory preserved");
  var a=Stock(1,6000);var b=Stock(2,4500);b.Locked=true;var pooled=SurplusSales.Plan(new[]{a,b},9900);C(pooled.Single().Count==600,"pooled stacks use total reserve");
  foreach(var mutate in new Action<SaleStock>[] {s=>s.Kind=65,s=>s.Kind=1,s=>s.Group=2,s=>s.Eligible=false,s=>s.UnitCount=2,s=>s.Price=0}){var x=Stock();mutate(x);C(SurplusSales.Plan(new[]{x},100).Length==0,"non-territory or unsupported item excluded");}
  Reject(()=>SurplusSales.Plan(new[]{Stock(),Stock()},100));
  C(SurplusSales.Plan(Enumerable.Range(1,50).Select(i=>{var x=Stock(i);x.Item=i;return x;}),9900).Length==32,"bounded native batch");
  var lines=SurplusSales.Plan(new[]{Stock()},9900);SalesProgress Pending()=>new(){Account="a",Token="token",State="pending",Lines=lines,Threshold=9900,SubmittedTicks=1,CurrencyBefore=1000};
  var reply=new SalesReply{Token="token",Accepted=true,Matches=true,Reward=297};var state=Pending();SurplusSales.Validate(state,"a");
  SurplusSales.Confirm(state,reply,new Dictionary<long,int>{{1,9900}},1297);C(state.State=="confirmed"&&state.ConfirmedItems==99&&state.ConfirmedCurrency==297,"receipt and balances verified");Reject(()=>SurplusSales.Confirm(state,reply,new Dictionary<long,int>{{1,9900}},1297));
  state=Pending();C(SurplusSales.Confirm(state,reply,new Dictionary<long,int>{{1,9999}},1297)&&!state.BalancesMatched&&state.ConfirmedItems==99,"accepted receipt survives later gathering");
  state=Pending();C(SurplusSales.Confirm(state,reply,new Dictionary<long,int>{{1,7000}},1296)&&!state.BalancesMatched&&state.ObservedCurrency==1296&&state.ObservedItems.Single().Count==7000,"later manual sale or cooking is recorded without deadlock");
  Reject(()=>SurplusSales.Confirm(Pending(),new(){Token="other",Accepted=true,Matches=true,Reward=297},new Dictionary<long,int>{{1,9900}},1297));
  Reject(()=>SurplusSales.Validate(Pending(),"other"));
  state=Pending();SurplusSales.Confirm(state,new(){Token="token",Rejected=true,Matches=true},new Dictionary<long,int>(),0);C(state.State=="rejected"&&state.ConfirmedItems==0,"rejection is not a sale");
  C(SurplusSales.Matches(lines,lines),"native request matches");C(!SurplusSales.Matches(lines,new[]{new SaleLine{Index=1,Group=1,Row=2,Count=100}}),"over-sale request rejected");
  C(!new TerritorySettings{SellThreshold=9901}.ValidSettings()&&!new TerritorySettings{SellThreshold=99}.ValidSettings(),"control channel enforces limits");C(new TerritorySettings().SellThreshold==9900&&new TerritorySettings{AutoSell=false}.AutoSell,"safe defaults");
  C(SurplusSales.ProbeDue(100,100,1,0),"new receipt checks capacity before the next target");
  C(!SurplusSales.ProbeDue(100,100,1,1),"idle repeated frame reuses probe");
  C(SurplusSales.ProbeDue(TimeSpan.FromSeconds(6).Ticks,0,1,1),"external stock changes are refreshed");
  SalesSnapshot Snapshot(int count=7000)=>new(){Token="token",Account="a",RequestedTicks=2,CapturedTicks=3,Currency=9123,Accepted=true,Items=new[]{Stock(1,count)}};
  state=Pending();state.ConfirmedItems=13;state.ConfirmedCurrency=17;
  C(!SurplusSales.Reconcile(state,Snapshot(),"a",false)&&state.Pending,"live native requests prevent recovery");
  C(SurplusSales.Reconcile(state,Snapshot(),"a",true)&&state.State=="reconciled"&&state.ConfirmedItems==13&&state.ConfirmedCurrency==17,"manual cleanup recovers without fabricating sale totals");
  C(SurplusSales.Plan(state.Recovery.Items,9900).Length==0,"cleaned inventory does not replay the old sale");
  C(!SurplusSales.Reconcile(state,Snapshot(),"a",true),"recovery is idempotent");
  state=System.Text.Json.JsonSerializer.Deserialize<SalesProgress>(System.Text.Json.JsonSerializer.Serialize(state));SurplusSales.Validate(state,"a");C(!state.Pending&&state.Recovery.Items[0].Count==7000,"recovery survives process restart");
  foreach(var mutate in new Action<SalesSnapshot>[]{s=>s.Accepted=false,s=>s.Token="other",s=>s.Account="other",s=>s.RequestedTicks=1,s=>s.CapturedTicks=1,s=>s.Currency=-1,s=>s.Items=null,s=>s.Items=new[]{Stock(),Stock()},s=>s.Items[0].Count=-1}){
   var snapshot=Snapshot();mutate(snapshot);state=Pending();C(!SurplusSales.Reconcile(state,snapshot,"a",true)&&state.Pending,"untrusted or stale snapshot cannot authorize planning");
  }
  Reject(()=>SurplusSales.Reconcile(Pending(),Snapshot(),"other",true));
  foreach(int count in new[]{0,7000,9900,9910,9999}){state=Pending();C(SurplusSales.Reconcile(state,Snapshot(count),"a",true),"fresh server stock resolves missing receipt");C(SurplusSales.Plan(state.Recovery.Items,9900).Sum(x=>x.Count)==Math.Max(0,count-9900),"only current excess is eligible after reconciliation");C(state.ConfirmedItems==0&&state.ConfirmedCurrency==0,"unknown sale stays unknown");}
  state=Pending();var empty=Snapshot();empty.Items=Array.Empty<SaleStock>();C(SurplusSales.Reconcile(state,empty,"a",true),"authoritative empty inventory is valid");
  state=Pending();C(!SurplusSales.Confirm(state,new(){Token="token",Matches=true,Accepted=true,Reward=298},new Dictionary<long,int>(),0)&&state.Pending,"unexpected reward requires inventory reconciliation");
  // Old Runtime journals lack all new diagnostic fields. Exercise the runtime serializer too.
  var legacy=System.Text.Json.JsonSerializer.Serialize(new {Schema=1,Account="a",Token="token",State="pending",Threshold=9900,SubmittedTicks=1L,CurrencyBefore=1000L,ConfirmedItems=13L,ConfirmedCurrency=17L,Lines=lines});
  var serializer=new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(SalesProgress));
  using(var bytes=new MemoryStream(System.Text.Encoding.UTF8.GetBytes(legacy)))state=(SalesProgress)serializer.ReadObject(bytes);
  C(state.Pending&&SurplusSales.Reconcile(state,Snapshot(),"a",true)&&state.ConfirmedItems==13,"legacy Runtime journal recovers with runtime serializer");
  using(var bytes=new MemoryStream()){serializer.WriteObject(bytes,state);bytes.Position=0;state=(SalesProgress)serializer.ReadObject(bytes);C(state.State=="reconciled"&&state.Recovery.Items[0].Count==7000,"runtime serializer preserves recovery evidence");}
  Console.WriteLine("Surplus sales: "+n+" checks passed");
 }
}
