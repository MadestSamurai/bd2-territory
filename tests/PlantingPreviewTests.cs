using BD2Territory;
using System.Text.Json;
internal static class PlantingPreviewTests
{
 internal static int Run()
 {
  int checks=0;void Check(bool value,string why){checks++;if(!value)throw new Exception("Planting preview: "+why);}
  void Reject(Action action,string why){bool rejected=false;try{action();}catch(InvalidOperationException){rejected=true;}Check(rejected,why);}
  long now=DateTime.UtcNow.Ticks;long At(int ms)=>now+TimeSpan.FromMilliseconds(ms).Ticks;
  var keys=Enumerable.Range(0,100).Select(n=>"field-"+n).ToArray();
  FarmEmptyProgress[] NewFields()=>Enumerable.Range(0,100).Select(_=>new FarmEmptyProgress()).ToArray();
  FarmReadiness[] Observe(FarmEmptyProgress[] source,int ms)
  {return source.Select((f,i)=>{f.Observe(At(ms),true,i<5,false,false,true,false,false);return f.Status;}).ToArray();}
  var fields=NewFields();Observe(fields,0);
  Check(Observe(fields,3000).Count(f=>f==FarmReadiness.Empty)==100,"paused observation has reconciled the five stale cache fields");
  // Screenshot sequence: a new owner clears evidence. The old count is 100, but the current count
  // must be 95; no UI should open until a freshly observed count is sufficient.
  fields=NewFields();var first=Observe(fields,3100);
  Check(first.Count(f=>f==FarmReadiness.Empty)==95,"restarting must not reuse the pre-reset 100-empty count");
  var preview=new PlantingPreviewProgress();
  Check(preview.Evaluate(true,keys,Observe(fields,3500))==PlantingPreviewDecision.Wait,"native preview of 100 is not a connectivity failure when cache proof is rebuilding");
  Check(preview.Reason.Contains("缓存 5")&&!preview.Reason.Contains("农田连接"),"report the actual five pending fields");
  Check(preview.Evaluate(true,keys,Observe(fields,5000))==PlantingPreviewDecision.Wait,"no approval before stable native evidence");
  Check(preview.Evaluate(true,keys,Observe(fields,5100))==PlantingPreviewDecision.Ready,"same fields become eligible after stable agreement without changing layout");
  Check(preview.Reason=="","clear obsolete wait reason when ready");
  var ready=Enumerable.Repeat(FarmReadiness.Empty,100).ToArray();
  foreach(var state in new[]{FarmReadiness.Unknown,FarmReadiness.PendingRequest,FarmReadiness.ReconcilingCache})
  {var a=(FarmReadiness[])ready.Clone();a[9]=state;Check(preview.Evaluate(true,keys,a)==PlantingPreviewDecision.Wait,"transient evidence not confirmed: "+state);}
  foreach(var state in new[]{FarmReadiness.Occupied,FarmReadiness.LiveCrop,FarmReadiness.NativeUnavailable,(FarmReadiness)99})
  {var a=(FarmReadiness[])ready.Clone();a[9]=state;Check(preview.Evaluate(true,keys,a)==PlantingPreviewDecision.Reject,"actual blocked or unknown field cannot be overwritten: "+state);Check(!preview.Reason.Contains("农田连接"),"blocked field must not be described as disconnected");}
  Check(preview.Evaluate(false,keys,ready)==PlantingPreviewDecision.Reject,"single mode cannot submit even if stale preview has 100 cells");
  Check(preview.Evaluate(true,null,ready)==PlantingPreviewDecision.Reject,"missing preview is not an empty batch");
  var duplicated=(string[])keys.Clone();duplicated[99]=keys[0];Check(preview.Evaluate(true,duplicated,ready)==PlantingPreviewDecision.Reject,"exactly 100 entries must also be distinct");
  var missing=(string[])keys.Clone();missing[99]="";Check(preview.Evaluate(true,missing,ready)==PlantingPreviewDecision.Reject,"all identities required");
  Check(preview.Evaluate(true,keys,ready.Take(99).ToArray())==PlantingPreviewDecision.Reject,"availability array must match native preview");
  foreach(int count in new[]{0,1,99,101})
  {
   preview.Reset();var k=Enumerable.Range(0,count).Select(n=>"field-"+n).ToArray();var states=Enumerable.Repeat(FarmReadiness.Empty,count).ToArray();
   Check(preview.Evaluate(true,k,states)==PlantingPreviewDecision.Refresh,"one native refresh for size "+count);
   Check(preview.Evaluate(true,k,states)==PlantingPreviewDecision.Reject,"no repeated refresh or partial payment for size "+count);
   Check(preview.Evaluate(true,keys,ready)==PlantingPreviewDecision.Ready,"fresh valid preview can recover");
  }
  var interrupted=new FarmEmptyProgress();interrupted.Observe(At(6000),true,true,false,false,true,false,false);
  interrupted.Observe(At(7000),false,false,false,false,false,false,false);
  Check(!interrupted.Observe(At(8100),true,true,false,false,true,false,false),"missing world data resets stable-observation evidence");
  Check(interrupted.Observe(At(10100),true,true,false,false,true,false,false),"fresh stable observations restore readiness");
  // Carry non-default recipes and fixed planting all the way through payment, serialization and receipt.
  foreach(int recipe in new[]{1,3,7,13})foreach(int fixedSeed in new[]{0,4})
  {
   var p=new RecipeBatchProgress{Account="test-account",RecipeId=recipe,FixedSeedId=fixedSeed,SeedId=4};
   var pending=PlantingTransaction.Begin(p,keys,3,"owner",300,"recipe-"+recipe+"-"+fixedSeed);
   var wire=JsonSerializer.Deserialize<RecipeBatchProgress>(JsonSerializer.Serialize(pending));
   Check(PlantingTransaction.MatchesRequest(wire,keys.Select(k=>new PlantingCell{Key=k,Seed=4,Cost=3,CurrencyType=64,CurrencyId=0}).ToArray()),"non-default recipe and fixed crop preserve actual native costs");
   var reply=new PlantingReply{Token=wire.PendingToken,Keys=keys,Seed=4,Cost=300,RequestMatches=true,Accepted=true};
   var done=PlantingTransaction.Settle(wire,reply);
   Check(done.RecipeId==recipe&&done.FixedSeedId==fixedSeed&&done.Batches==1&&done.Planted==100&&done.Spent==300,"selected recipe can finish an actual batch");
   Check(ReferenceEquals(done,PlantingTransaction.Settle(done,reply)),"repeated receipt cannot duplicate payment");
   RecipeBatchPlanner.Choose(done,new[]{new CropStock{SeedId=4,Required=1,Yield=1}});
   Reject(()=>PlantingTransaction.Begin(done,keys,3,"owner",599,"second"),"dynamic recipe does not bypass accumulated budget");
  }
  foreach(int recipe in new[]{0,-1})Reject(()=>PlantingTransaction.Begin(new RecipeBatchProgress{Account="a",RecipeId=recipe,SeedId=4},keys,3,"owner",0,"bad"),"invalid recipe identity still rejected");
  Console.WriteLine("Planting preview and recipe transaction checks: "+checks);return checks;
 }
}
