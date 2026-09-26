using BD2Territory;
using System.Text.Json;
using System.Runtime.Serialization.Json;
internal static class DynamicPlantingTests
{
 internal static int Run()
 {
  int checks=0;void Check(bool ok,string why){checks++;if(!ok)throw new Exception("Dynamic planting: "+why);}
  void Reject(Action action,string why){bool rejected=false;try{action();}catch(InvalidOperationException){rejected=true;}Check(rejected,why);}
  string[] Keys(int count,string group="island")=>Enumerable.Range(0,count).Select(i=>group+":"+i).ToArray();
  PlantingCell[] Cells(RecipeBatchProgress p)=>p.PendingKeys.Select(k=>new PlantingCell{Key=k,Seed=p.PendingSeed,Cost=p.PendingCost/p.PlannedCount,CurrencyType=64}).ToArray();
  PlantingReply Reply(RecipeBatchProgress p)=>new(){Token=p.PendingToken,Keys=p.PendingKeys.Reverse().ToArray(),Seed=p.PendingSeed,Cost=p.PendingCost,Accepted=true,RequestMatches=true};
  var preview=new PlantingPreviewProgress();
  foreach(int count in new[]{1,9,23,37,84,99,100,101})
  {
   var keys=Keys(count);preview.Reset();var readiness=Enumerable.Repeat(FarmReadiness.Empty,count).ToArray();
   Check(preview.Evaluate(true,keys,readiness)==PlantingPreviewDecision.Ready,"accept the actual native group of "+count+" fields without refreshing it");
   readiness[0]=FarmReadiness.ReconcilingCache;
   Check(preview.Evaluate(true,keys,readiness)==PlantingPreviewDecision.Wait,"small groups still wait for authoritative empty-field evidence");
   var p=new RecipeBatchProgress{Account="a",SeedId=4};
   Check(PlantingTransaction.WithinBudget(p,"run",count*3,3,count),"budget covers precisely the group");
   Check(!PlantingTransaction.WithinBudget(p,"run",count*3-1,3,count),"not even one currency over the budget");
   var intent=PlantingTransaction.Begin(p,keys,3,"run",count*3,"batch-"+count);
   Check(intent.PlannedCount==count&&intent.PendingCost==count*3&&p.PlannedCount==0,"persist actual count and exact cost without mutating original");
   Check(PlantingTransaction.MatchesRequest(intent,Cells(intent)),"actual native request matches");
   var wrong=Cells(intent);wrong[0].Key="other-island";
   Check(!PlantingTransaction.MatchesRequest(intent,wrong),"equal count from a different group cannot pass");
   var partial=Reply(intent);partial.Keys=partial.Keys.Skip(1).ToArray();Reject(()=>PlantingTransaction.Settle(intent,partial),"partial response cannot finish a variable-size batch");
   var extra=Reply(intent);extra.Keys=extra.Keys.Concat(new[]{"extra"}).ToArray();Reject(()=>PlantingTransaction.Settle(intent,extra),"superset response cannot finish a batch");
   // Exercise the runtime serializer, which does not execute default property initializers on load.
   using var stream=new MemoryStream();new DataContractJsonSerializer(typeof(RecipeBatchProgress)).WriteObject(stream,intent);stream.Position=0;
   var restored=RecipeBatchPlanner.Restore((RecipeBatchProgress)new DataContractJsonSerializer(typeof(RecipeBatchProgress)).ReadObject(stream),"a");
   var done=PlantingTransaction.Settle(restored,Reply(intent));
   Check(done.Planted==count&&done.PlannedCount==count&&done.Batches==1&&done.Spent==count*3,"restart finishes the precise smaller or larger group");
   Check(ReferenceEquals(done,PlantingTransaction.Settle(done,Reply(intent))),"receipt replay never spends again");
   Check(RecipeBatchPlanner.Restore(done,"a").Planted==count,"completed non-100 journal reloads");
   var stocks=new[]{new CropStock{SeedId=4,Required=1,Growing=count},new CropStock{SeedId=2,Required=1}};
   Check(RecipeBatchPlanner.Choose(done,stocks)==2&&done.PlannedCount==0,"after a small completed batch choose the next deficient ingredient instead of waiting to reach 100");
   Reject(()=>PlantingTransaction.Begin(done,Keys(1),1,"run",count*3,"exhausted"),"smaller later group cannot bypass spent budget");
  }
  // Walk three separate islands and a single isolated field; bill each group once and finish all 70.
  var scattered=new RecipeBatchProgress{Account="a",FixedSeedId=2};var fixedCrop=new[]{new CropStock{SeedId=2,Required=1,Yield=2}};
  foreach(int count in new[]{37,23,9,1})
  {
   RecipeBatchPlanner.Choose(scattered,fixedCrop);
   var p=PlantingTransaction.Begin(scattered,Keys(count,"group-"+count),2,"run",140,"island-"+count);
   scattered=PlantingTransaction.Settle(p,Reply(p));fixedCrop[0].Growing+=count;
  }
  Check(scattered.Batches==4&&scattered.Spent==140&&fixedCrop[0].Growing==70,"disconnected fields finish in native groups, including the isolated remainder");
  Check(!PlantingTransaction.WithinBudget(scattered,"run",140,2,1),"total budget exactly consumed");
  Check(PlantingTransaction.WithinBudget(scattered,"new-run",2,2,1),"new budget owner starts with its own allowance");
  Check(!PlantingTransaction.WithinBudget(scattered,"run",0,int.MaxValue,2),"overflow rejected before preview payment");
  // Different native group sizes and yields must balance recipe output rather than number of batches.
  var crops=new[]{new CropStock{SeedId=1,Required=5,Yield=2,Inventory=150},new CropStock{SeedId=2,Required=3,Yield=1,Growing=30},new CropStock{SeedId=4,Required=2,Yield=3}};
  var progress=new RecipeBatchProgress{Account="ratio"};var sizes=new[]{37,9,23,1,64};long plants=0;
  for(int i=0;i<300;i++)
  {
   int size=sizes[i%sizes.Length];int seed=RecipeBatchPlanner.Choose(progress,crops);
   var p=PlantingTransaction.Begin(progress,Keys(size),1,"ratio-run",0,"ratio-"+i);progress=PlantingTransaction.Settle(p,Reply(p));
   crops.Single(c=>c.SeedId==seed).Growing+=size;plants+=size;
   if(i%7==0){foreach(var c in crops){c.Inventory+=(long)c.Growing*c.Yield;c.Growing=0;}}
   if(i%11==0){long dishes=crops.Min(c=>c.Inventory/c.Required);foreach(var c in crops)c.Inventory-=dishes*c.Required;}
  }
  var equivalents=crops.Select(c=>(c.Inventory+(double)c.Growing*c.Yield)/c.Required).ToArray();
  Check(equivalents.Max()-equivalents.Min()<=96,"long-run 5:3:2 yield deviation bounded by one actual native batch, even across cooking and harvesting");
  Check(progress.Batches==300&&progress.Spent==plants,"irregular group budget is total fields, never batches times 100");
  // The existing released journal must retain an unresolved 100-field payment when upgraded.
  var legacy=PlantingTransaction.Begin(new(){Account="legacy",SeedId=2,Spent=7,BudgetOwner="owner"},Keys(100),1,"owner",200,"old-token");legacy.Schema=3;legacy.PlannedCount=0;
  string oldJson=JsonSerializer.Serialize(legacy);var oldNode=System.Text.Json.Nodes.JsonNode.Parse(oldJson);oldNode.AsObject().Remove("PlannedCount");
  using(var stream=new MemoryStream(System.Text.Encoding.UTF8.GetBytes(oldNode.ToJsonString())))
  {
   var old=(RecipeBatchProgress)new DataContractJsonSerializer(typeof(RecipeBatchProgress)).ReadObject(stream);
   var restored=RecipeBatchPlanner.Restore(old,"legacy");
   Check(restored.Schema==4&&restored.PlannedCount==100&&restored.PendingToken=="old-token"&&restored.Spent==7,"old intent upgraded without discarding charge or identity");
   var done=PlantingTransaction.Settle(restored,Reply(restored));Check(done.Spent==107&&done.Planted==100,"old intent settles exactly once after upgrade");
   done.Schema=3;done.PlannedCount=0;Check(RecipeBatchPlanner.Restore(done,"legacy").PlannedCount==100,"legacy completed journal remains completed");
  }
  Reject(()=>RecipeBatchPlanner.Restore(legacy,"another-account"),"cross-account journal rejected");
  var broken=legacy.Copy();broken.PendingKeys=Keys(99);Reject(()=>RecipeBatchPlanner.Restore(broken,"legacy"),"old partial intent cannot silently become a new small batch");
  broken=legacy.Copy();broken.Schema=4;broken.PlannedCount=99;Reject(()=>RecipeBatchPlanner.Restore(broken,"legacy"),"new intent count mismatch rejected");
  broken=legacy.Copy();broken.Schema=4;broken.PlannedCount=100;broken.PendingCost=99;Reject(()=>RecipeBatchPlanner.Restore(broken,"legacy"),"invalid recorded unit charge rejected");
  var partialProgress=new RecipeBatchProgress{Account="a",SeedId=2,PlannedCount=37,Planted=9};Reject(()=>RecipeBatchPlanner.Restore(partialProgress,"a"),"partial local progress still rejected");
  Console.WriteLine("Dynamic planting checks: "+checks);return checks;
 }
}
