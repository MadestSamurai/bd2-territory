using BD2Territory;
using System.Runtime.Serialization.Json;
using System.Text;
internal static class RecipePaymentTests
{
 internal static int Run()
 {
  int checks=0;void Check(bool ok,string why){checks++;if(!ok)throw new Exception("Recipe payment: "+why);}
  string[] Fields(int count)=>Enumerable.Range(0,count).Select(i=>"field:"+i).ToArray();
  // Anonymized Runtime16 feedback: recipe 2, completed previous work, no pending payment.
  const string journal="""
  {"Account":"feedback-account","Batches":368,"BudgetOwner":"previous-run","LastReceipt":"previous-receipt","PendingCost":0,"PendingKeys":[],"PendingOwner":"","PendingSeed":0,"PendingToken":"","Planted":0,"RecipeId":2,"Schema":3,"SeedId":0,"Spent":100}
  """;
  using var stream=new MemoryStream(Encoding.UTF8.GetBytes(journal));
  var restored=RecipeBatchPlanner.Restore((RecipeBatchProgress)new DataContractJsonSerializer(typeof(RecipeBatchProgress)).ReadObject(stream),"feedback-account");
  Check(restored.Schema==4&&restored.RecipeId==2&&restored.Batches==368&&restored.Spent==100&&restored.LastReceipt=="previous-receipt","upgrade preserves recipe and previous accounting");
  var crops=new[]{new CropStock{SeedId=9,Required=10,Inventory=0,Price=10},new CropStock{SeedId=3,Required=10,Inventory=100,Price=2},new CropStock{SeedId=8,Required=5,Inventory=0,Price=20}};
  Check(RecipeBatchPlanner.Choose(restored,crops)==9,"fried-rice feedback chooses rice first");
  var intent=PlantingTransaction.Begin(restored,Fields(100),10,"current-run",31000,"rice-payment");
  Check(intent.PendingSeed==9&&intent.PendingCost==1000&&intent.PendingKeys.Length==100&&intent.RecipeId==2,"non-default recipe reaches native payment with exact rice cost");
  Check(restored.Spent==100&&restored.PendingToken==""&&intent.Spent==0,"new run budget does not mutate existing journal before save");
  var cells=intent.PendingKeys.Select(key=>new PlantingCell{Key=key,Seed=9,Cost=10,CurrencyType=64,CurrencyId=0}).ToArray();
  Check(PlantingTransaction.MatchesRequest(intent,cells),"native request validates non-default recipe");
  var reply=new PlantingReply{Token=intent.PendingToken,Keys=intent.PendingKeys,Seed=9,Cost=1000,RequestMatches=true,Accepted=true};
  var done=PlantingTransaction.Settle(intent,reply);
  Check(done.Batches==369&&done.Planted==100&&done.Spent==1000&&done.PendingToken=="","recipe 2 settles and clears payment once");
  Check(ReferenceEquals(done,PlantingTransaction.Settle(done,reply)),"replayed confirmation does not charge twice");
  crops[0].Growing=100;
  Check(RecipeBatchPlanner.Choose(done,crops)==8,"recipe continues with missing pepper instead of stopping after rice");
  var pepper=PlantingTransaction.Begin(done,Fields(37),20,"current-run",31000,"pepper-payment");
  Check(pepper.PendingCost==740&&pepper.RecipeId==2&&pepper.PendingSeed==8,"same recipe supports smaller disconnected native groups");
  // Exercise transaction entry, not only crop selection / Confirm, for arbitrary recipe IDs.
  foreach(int recipe in Enumerable.Range(1,13).Append(99)){
   var state=new RecipeBatchProgress{Account="test",RecipeId=recipe,SeedId=9};
   var payment=PlantingTransaction.Begin(state,Fields(23),10,"run",1000,"recipe-"+recipe);
   Check(payment.RecipeId==recipe&&payment.PendingCost==230,"payment accepts validated recipe "+recipe);
  }
  var pending=restored.Copy();pending.PendingToken="unresolved";
  bool rejected=false;try{PlantingTransaction.Begin(pending,Fields(100),10,"run",31000,"new");}catch(InvalidOperationException){rejected=true;}
  Check(rejected,"unresolved payment protection remains in place");
  Console.WriteLine("Recipe payment regression checks: "+checks);return checks;
 }
}