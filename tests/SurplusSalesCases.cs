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
  Reject(()=>SurplusSales.Confirm(Pending(),reply,new Dictionary<long,int>{{1,9999}},1297));
  Reject(()=>SurplusSales.Confirm(Pending(),reply,new Dictionary<long,int>{{1,9900}},1296));
  Reject(()=>SurplusSales.Confirm(Pending(),new(){Token="other",Accepted=true,Matches=true,Reward=297},new Dictionary<long,int>{{1,9900}},1297));
  Reject(()=>SurplusSales.Validate(Pending(),"other"));
  state=Pending();SurplusSales.Confirm(state,new(){Token="token",Rejected=true,Matches=true},new Dictionary<long,int>(),0);C(state.State=="rejected"&&state.ConfirmedItems==0,"rejection is not a sale");
  C(SurplusSales.Matches(lines,lines),"native request matches");C(!SurplusSales.Matches(lines,new[]{new SaleLine{Index=1,Group=1,Row=2,Count=100}}),"over-sale request rejected");
  C(!new TerritorySettings{SellThreshold=9901}.ValidSettings()&&!new TerritorySettings{SellThreshold=99}.ValidSettings(),"control channel enforces limits");C(new TerritorySettings().SellThreshold==9900&&!new TerritorySettings().AutoSell,"safe defaults");
  Console.WriteLine("Surplus sales: "+n+" checks passed");
 }
}
