using BD2Territory;
internal static class WorkCycleTests
{
 internal static int Run()
 {
  int checks=0;void Check(bool ok,string why){checks++;if(!ok)throw new Exception("Work cycle: "+why);}
  var cycle=new TerritoryWorkCycle();
  var resources=Enumerable.Range(1,100).Select(i=>"crop:"+i).ToList();var empty=new List<string>();
  Check(cycle.Advance(resources,empty)==TerritoryWorkPhase.Gathering,"harvest starts before planting");
  foreach(var key in resources.ToArray()){
   Check(cycle.AcceptsResource(key),"all original harvest targets remain in this pass");
   cycle.Gathered(key);resources.Remove(key);empty.Add("field:"+key);
   var phase=cycle.Advance(resources,empty);
   Check(phase==(resources.Count>0?TerritoryWorkPhase.Gathering:TerritoryWorkPhase.Planting),"one newly empty cell never interrupts remaining harvests");
  }
  Check(cycle.Remaining==100,"all hundred empty fields become one planting pass");
  resources.Add("new-tree");resources.Add("new-ore");
  Check(cycle.Advance(resources,empty)==TerritoryWorkPhase.Planting&&!cycle.AcceptsResource("new-tree"),"new resources do not interrupt planting");
  cycle.Planted(empty); // Complete native group: stale cache may still call these empty.
  Check(cycle.Advance(resources,empty)==TerritoryWorkPhase.Processing,"confirmed fields not planted twice even with stale cache");
  cycle.FinishProcessing();Check(cycle.Advance(resources,[])==TerritoryWorkPhase.Gathering&&cycle.AcceptsResource("new-ore"),"next pass includes new ore and tree");
  // Continuous spawns cannot keep adding work to the current pass.
  cycle.Reset();Check(cycle.Advance(["original"],[])==TerritoryWorkPhase.Gathering,"begin finite pass");
  Check(cycle.Advance(["original","respawn"],["first-empty"])==TerritoryWorkPhase.Gathering&&!cycle.AcceptsResource("respawn"),"do not extend harvest pass for spawns");
  cycle.Gathered("original");Check(cycle.Advance(["respawn"],["first-empty"])==TerritoryWorkPhase.Planting,"planting gets its turn despite fresh resources");
  cycle.Reset();cycle.Advance(["same-object"],[]);cycle.Gathered("same-object");
  Check(!cycle.AcceptsResource("same-object")&&cycle.Advance(["same-object"],["plot"])==TerritoryWorkPhase.Planting,"completed target reusing its object after a long input interval cannot restart gathering in this pass");
  // Failed/cooling targets are deferred, never forgotten globally or allowed to stall farming.
  cycle.Reset();cycle.Advance(["ore","crop"],[]);cycle.Gathered("crop");
  Check(cycle.Advance([],["plot"])==TerritoryWorkPhase.Planting,"cooling ore does not block planting");
  cycle.Planted(["plot"]);cycle.Advance([],[]);cycle.FinishProcessing();
  Check(cycle.Advance(["ore"],[])==TerritoryWorkPhase.Gathering&&cycle.AcceptsResource("ore"),"ore can return after cooldown");
  // Split fields remain one phase; a maturing crop cannot send the actor back to harvesting.
  cycle.Reset();var islands=new[]{37,23,9,1};var fields=Enumerable.Range(1,70).Select(i=>"f"+i).ToList();
  Check(cycle.Advance([],fields)==TerritoryWorkPhase.Planting,"empty-only territory plants immediately");int planted=0,batches=0;
  foreach(int size in islands){
   var group=fields.Take(size).ToArray();Check(group.All(cycle.AcceptsField),"disconnected group remains eligible");
   Check(cycle.Advance(["matured-during-planting"],fields,true)==TerritoryWorkPhase.Planting,"pending preview/payment cannot advance phase");
   cycle.Planted(group);fields.RemoveRange(0,size);planted+=size;batches++;
   var phase=cycle.Advance(["matured-during-planting"],fields);
   Check(phase==(fields.Count==0?TerritoryWorkPhase.Processing:TerritoryWorkPhase.Planting),"fill all four groups without harvest detour");
  }
  Check(planted==70&&batches==4,"batch follows native groups, no 100-field minimum");
  cycle.FinishProcessing();Check(cycle.Advance(["matured-during-planting"],[])==TerritoryWorkPhase.Gathering,"deferred crop harvested next pass");
  // Existing/unknown actions and option changes preserve boundaries.
  Check(cycle.Advance([],[],true)==TerritoryWorkPhase.Gathering&&cycle.Remaining==1,"in-flight action not treated as completed on empty observation");
  cycle.Reset();Check(cycle.Phase==TerritoryWorkPhase.Idle&&cycle.Remaining==0,"pause/scene/account release clears only volatile phase");
  Check(cycle.Advance([],[])==TerritoryWorkPhase.Processing,"cooking/sales-only mode remains runnable");
  cycle.FinishProcessing();cycle.Advance([], ["expensive","cheap"]);
  Check(cycle.Advance([], ["cheap"])==TerritoryWorkPhase.Planting&&cycle.AcceptsField("cheap"),"failed or unaffordable group does not abandon other fields");
  Check(cycle.Advance([],[])==TerritoryWorkPhase.Processing,"no affordable planting target ends pass rather than blocking gathering");
  cycle.FinishProcessing();Check(cycle.Advance(["tree"],[])==TerritoryWorkPhase.Gathering,"budget exhaustion still permits resource collection");
  Console.WriteLine("Work-cycle checks: "+checks);return checks;
 }
}
