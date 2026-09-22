using System;
using System.Linq;
namespace BD2Territory
{
 // One native confirmation plants exactly 100 cells of the same crop. Balance ingredients in recipe-equivalent units.
 public sealed class RecipeBatchProgress
 {
  public int Schema{get;set;}=3;public string Account{get;set;}="";public int RecipeId{get;set;}=3;
  public int SeedId{get;set;}public int Planted{get;set;}public int Batches{get;set;}
  public string[] PendingKeys{get;set;}=new string[0];public int PendingSeed{get;set;}
  public string PendingToken{get;set;}="";public int PendingCost{get;set;}public string PendingOwner{get;set;}="";
  public string BudgetOwner{get;set;}="";public long Spent{get;set;}
  public RecipeBatchProgress Copy(){var next=(RecipeBatchProgress)MemberwiseClone();next.PendingKeys=(string[])PendingKeys.Clone();return next;}
  public string LastReceipt{get;set;}="";
 }
 public static class RecipeBatchPlanner
 {
  public const int BatchSize=100;
  public static int Choose(RecipeBatchProgress progress,CropStock[] crops)
  {
   if(crops==null||crops.Length==0||crops.Length>20||crops.Any(c=>c==null)||crops.Select(c=>c.SeedId).Distinct().Count()!=crops.Length||crops.Any(c=>c.Required<=0||c.Yield<=0||c.Inventory<0||c.Growing<0))throw new InvalidOperationException("料理配方或库存不完整");
   if(!string.IsNullOrEmpty(progress.PendingToken))throw new InvalidOperationException("上次播种尚未确认，不能安排新批次");
   if(progress.Planted!=0&&progress.Planted!=BatchSize)throw new InvalidOperationException("批次进度异常");
   if(progress.SeedId>0&&progress.Planted<BatchSize)
   {if(!crops.Any(c=>c.SeedId==progress.SeedId))throw new InvalidOperationException("当前批次作物不属于配方");return progress.SeedId;}
   // Add the expected output of plants already in the ground exactly once; ignore sell price for crop selection.
   var chosen=crops.OrderBy(c=>(c.Inventory+(double)c.Growing*c.Yield)/c.Required).ThenByDescending(c=>c.Required).ThenBy(c=>c.SeedId).First();
   progress.SeedId=chosen.SeedId;progress.Planted=0;return chosen.SeedId;
  }
  public static bool SwitchRecipe(RecipeBatchProgress p,int recipeId,bool batchActive)
  {
   if(recipeId<=0)throw new ArgumentException("配方编号无效");
   if(p.RecipeId==recipeId)return true;
   if(batchActive||!string.IsNullOrEmpty(p.PendingToken))return false;
   p.RecipeId=recipeId;p.SeedId=0;p.Planted=0;return true;
  }
  public static bool Confirm(RecipeBatchProgress p,int seed,int count,string receipt)
  {
   if(string.IsNullOrEmpty(receipt))throw new ArgumentException("缺少服务器回执身份");
   if(p.LastReceipt==receipt)return false;
   if(seed<=0||seed!=p.SeedId||p.Planted!=0||count!=BatchSize)throw new InvalidOperationException("播种回执与当前批次不一致");
   p.Planted+=count;p.LastReceipt=receipt;if(p.Planted==BatchSize)p.Batches++;return true;
  }
 }
}
