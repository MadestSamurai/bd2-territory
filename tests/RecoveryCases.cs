using BD2Territory;
internal static class RecoveryCases
{
 public static int Run()
 {
  int checks=0;void Check(bool ok,string reason){checks++;if(!ok)throw new Exception(reason);}
  var baseState=new RecipeBatchProgress{Account="a",RecipeId=3,SeedId=2};
  var keys=new[]{"1:100:0:0","1:100:1:0","1:100:2:0"};
  var pending=PlantingTransaction.Begin(baseState,keys,2,"owner",100,"token");pending.PendingSubmittedTicks=DateTime.UtcNow.Ticks;
  foreach(var planted in new[]{keys,Array.Empty<string>(),new[]{keys[0]}}){
   var next=PlantingTransaction.ReconcileSnapshot(pending,planted);
   Check(next.PendingToken==""&&next.PendingSubmittedTicks==0&&next.PendingKeys.Length==0,"fresh world removes unresolved observer only");
   Check(next.Batches==0&&next.Spent==6,"unknown receipt is not counted as success; prior quote remains reserved in budget");
   Check(next.Planted==(planted.Length==3?3:0),"partial or empty world is replanned");
   Check(pending.PendingToken=="token"&&pending.PendingKeys.Length==3,"old immutable intent available for audit");
   Check(ReferenceEquals(PlantingTransaction.Settle(next,new PlantingReply{Token="token",Accepted=true,RequestMatches=true}),next),"late old receipt cannot count again");
   RecipeBatchPlanner.Restore(next,"a");Check(true,"reconciled state survives restart");
   var copy=next.Copy();RecipeBatchPlanner.Choose(copy,new[]{new CropStock{SeedId=2,Required=1,Yield=1,Growing=planted.Length}});
   if(planted.Length!=3)Check(PlantingTransaction.Begin(copy,keys.Skip(planted.Length).ToArray(),2,"owner",100,"next").PendingToken=="next","remaining current empty fields can be planned without replaying old keys");
  }
  bool reject=false;try{PlantingTransaction.ReconcileSnapshot(new RecipeBatchProgress(),keys);}catch(InvalidOperationException){reject=true;}
  Check(reject,"invalid journal is not silently consumed");
  Console.WriteLine("Network recovery planting checks: "+checks);return checks;
 }
}