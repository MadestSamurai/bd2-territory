using BD2Territory;
internal static class WorkCycleTests {
 internal static int Run(){
 int checks=0;void C(bool x,string why){checks++;if(!x)throw new Exception("Work cycle: "+why);}
 var w=new TerritoryWorkCycle();var crops=Enumerable.Range(1,100).Select(i=>"crop"+i).ToList();var fields=new List<string>();var resources=new[]{"ore","tree"};
 C(w.Advance(crops,resources,fields)==TerritoryWorkPhase.Harvesting,"crops before mining");
 foreach(var c in crops.ToArray()){
  C(w.AcceptsResource(c)&&!w.AcceptsResource("ore"),"harvest only, no mining detour");w.Gathered(c);crops.Remove(c);fields.Add("field"+c);
  C(w.Advance(crops,resources,fields)==(crops.Count>0?TerritoryWorkPhase.Harvesting:TerritoryWorkPhase.Planting),"finish whole crop batch before replanting");
 }
 C(w.Remaining==100,"all fields in planting phase");
 foreach(int count in new[]{37,23,39,1}){var batch=fields.Take(count).ToArray();C(batch.All(w.AcceptsField),"disconnected fields accepted");C(w.Advance(["new-crop"],resources,fields,true)==TerritoryWorkPhase.Planting,"in-flight planting freezes phase");w.Planted(batch);fields.RemoveRange(0,count);var p=w.Advance(["new-crop"],resources,fields);C(p==(fields.Count==0?TerritoryWorkPhase.Gathering:TerritoryWorkPhase.Planting),"finish replant before mining");}
 C(w.AcceptsResource("ore")&&!w.AcceptsResource("new-crop"),"mining while new crops grow");
 w.Gathered("ore");w.Gathered("tree");C(w.Advance(["new-crop"],["ore","tree","respawn"],["new-field"])==TerritoryWorkPhase.Processing,"completed resources and respawns do not extend workset");
 w.FinishProcessing();C(w.Advance(["new-crop"],["respawn"],["new-field"])==TerritoryWorkPhase.Harvesting,"next crop batch");
 w.Reset();C(w.Advance([],resources,["empty"])==TerritoryWorkPhase.Planting,"already empty fields planted first");w.Planted(["empty"]);C(w.Advance([],resources,["empty"])==TerritoryWorkPhase.Gathering,"stale field cache does not replant twice");
 w.Reset();C(w.Advance([],resources,[])==TerritoryWorkPhase.Gathering,"farming off still gathers");C(w.Advance([],[],[])==TerritoryWorkPhase.Processing,"cooldown does not block workset");w.FinishProcessing();C(w.Advance([],resources,[])==TerritoryWorkPhase.Gathering,"cooldown targets can return");
 w.Reset();C(w.Advance([],[],[])==TerritoryWorkPhase.Processing,"sales and cooking only");
 w.Reset();w.Advance([],resources,["expensive"]);w.Planted(["expensive"]);C(w.Advance([],resources,[])==TerritoryWorkPhase.Gathering,"unaffordable planting still allows mining");
 w.Reset();C(w.Remaining==0&&w.Phase==TerritoryWorkPhase.Idle,"scene reset discards only volatile schedule");
 Console.WriteLine("Work-cycle checks: "+checks);return checks;
 }
}
