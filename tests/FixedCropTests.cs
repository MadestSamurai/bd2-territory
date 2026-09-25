using BD2Territory;
using System.Text.Json;
internal static class FixedCropTests
{
 public static int Run()
 {
  int checks=0;void Check(bool pass,string label){checks++;if(!pass)throw new Exception(label);}
  var legacy=JsonSerializer.Deserialize<TerritorySettings>("{\"RecipeId\":3}");Check(!legacy.FixedCrop&&legacy.FixedSeedId==0&&legacy.ValidSettings(),"legacy settings retain recipe mode");
  var progress=new RecipeBatchProgress{Account="a",RecipeId=3,SeedId=2,Spent=300,Batches=2,BudgetOwner="run"};
  Check(!RecipeBatchPlanner.SwitchPlanting(progress,3,4,true)&&progress.FixedSeedId==0&&progress.SeedId==2,"an active preview cannot change seed");
  Check(RecipeBatchPlanner.SwitchPlanting(progress,3,4,false)&&progress.FixedSeedId==4&&progress.SeedId==0,"switch to a fixed crop between batches");
  Check(progress.Spent==300&&progress.Batches==2&&progress.BudgetOwner=="run","switching modes cannot reset expense or batch totals");
  var crops=new[]{new CropStock{SeedId=4,Required=1,Yield=3,Inventory=100000,Growing=500,Price=3}};
  for(int i=0;i<5;i++)
  {
   Check(RecipeBatchPlanner.Choose(progress,crops)==4,"fixed crop does not rebalance to a different ingredient");
   var keys=Enumerable.Range(0,100).Select(n=>"field"+n).ToArray();
   var pending=PlantingTransaction.Begin(progress,keys,3,"run",0,"fixed-"+i);
   Check(!RecipeBatchPlanner.SwitchPlanting(pending,7,1,false)&&!RecipeBatchPlanner.SwitchRecipe(pending,3,false),"unknown receipt blocks both seed and mode changes");
   var copy=JsonSerializer.Deserialize<RecipeBatchProgress>(JsonSerializer.Serialize(pending));
   Check(copy.FixedSeedId==4,"restart preserves fixed crop transaction");
   progress=PlantingTransaction.Settle(copy,new PlantingReply{Token=copy.PendingToken,Seed=4,Keys=keys,Cost=300,RequestMatches=true,Accepted=true});
  }
  Check(progress.Spent==1800&&progress.Batches==7,"fixed crop uses native 100-cell costs exactly once");
  Check(RecipeBatchPlanner.SwitchRecipe(progress,7,false)&&progress.FixedSeedId==0&&progress.SeedId==0,"switch back to recipe clears fixed selection for planning");
  var settings=new TerritoryControl{FixedCrop=true,FixedSeedId=4,RecipeId=7,Cooking=true};
  using(var stream=new MemoryStream(System.Text.Encoding.UTF8.GetBytes(JsonSerializer.Serialize(settings))))
  {var wire=(TerritoryControl)new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(TerritoryControl)).ReadObject(stream);Check(wire.FixedCrop&&wire.FixedSeedId==4&&wire.Cooking&&wire.RecipeId==7,"Mono wire keeps fixed planting independent from cooking recipe");}
  Console.WriteLine("Fixed crop checks: "+checks);return checks;
 }
}
