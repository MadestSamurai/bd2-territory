using BD2Territory;
using System.Text.Json;
static class CookingCases
{
 public static void Run()
 {
  int checks=0;void Check(bool v){checks++;if(!v)throw new Exception("Cooking regression "+checks);}void Reject(Action a){bool caught=false;try{a();}catch(ArgumentException){caught=true;}catch(InvalidOperationException){caught=true;}Check(caught);}
  Check(CookingPlanner.Count(100,9999,new long[]{501,302,200},new[]{5,3,2})==100);
  Check(CookingPlanner.Count(1000,9999,new long[]{500,299,200},new[]{5,3,2})==99);
  Check(CookingPlanner.Count(100,3,new long[]{500,300,200},new[]{5,3,2})==3);
  Check(CookingPlanner.Count(100,0,new long[]{500,300,200},new[]{5,3,2})==0);
  Check(CookingPlanner.Count(10,99,new long[]{0},new[]{1})==0);
  Check(CookingPlanner.Count(1000,9999,new long[]{long.MaxValue},new[]{1})==1000);
  Reject(()=>CookingPlanner.Count(0,1,new long[]{1},new[]{1}));Reject(()=>CookingPlanner.Count(1001,1,new long[]{1},new[]{1}));Reject(()=>CookingPlanner.Count(1,1,new long[]{1},new[]{0}));
  var state=new CookingProgress{Account="A",Token="T",Recipe=3,ResultItem=3031,Count=100,State="pending",ConfirmedTotal=5};
  var saved=JsonSerializer.Deserialize<CookingProgress>(JsonSerializer.Serialize(state))!;Check(saved.Pending&&saved.Account=="A"&&saved.Count==100);
  Reject(()=>CookingPlanner.Confirm(saved,new(){Token="other",Recipe=3,Count=100,Accepted=true}));Check(saved.Pending&&saved.ConfirmedTotal==5);
  Reject(()=>CookingPlanner.Confirm(saved,new(){Token="T",Recipe=2,Count=100,Accepted=true}));
  Reject(()=>CookingPlanner.Confirm(saved,new(){Token="T",Recipe=3,Count=99,Accepted=true}));
  Reject(()=>CookingPlanner.Confirm(saved,new(){Token="T",Recipe=3,Count=100}));Check(saved.Pending);
  CookingPlanner.Confirm(saved,new(){Token="T",Recipe=3,Count=100,Accepted=true});Check(!saved.Pending&&saved.ConfirmedTotal==105);
  Reject(()=>CookingPlanner.Confirm(saved,new(){Token="T",Recipe=3,Count=100,Accepted=true}));Check(saved.ConfirmedTotal==105);
  CookingPlanner.Confirm(state,new(){Token="T",Recipe=3,Count=100,Rejected=true});Check(!state.Pending&&state.ConfirmedTotal==5&&state.State=="rejected");
  Check(!new TerritorySettings().Cooking&&new TerritorySettings().CookingBatch==100);Check(!new TerritorySettings{CookingBatch=0}.ValidSettings());
  Console.WriteLine("Cooking checks: "+checks);
 }
}
