using BD2Territory;
using System.Text.Json;

static class CookingSalesCases
{
 public static void Run()
 {
  int checks=0;void Check(bool ok,string reason){checks++;if(!ok)throw new Exception(reason);}
  SaleStock Stock(long index,int item,int count,bool locked=false)=>new(){Index=index,Item=item,Count=count,Locked=locked,Eligible=true,Kind=66,Group=1,Row=item,UnitCount=1,Price=7};
  var stocks=new[]{Stock(1,3001,205),Stock(2,3002,500),Stock(3,3001,99,true)};
  var batch=CookingSales.Plan(stocks,3001,100,0);
  Check(batch.Length==1&&batch[0].Count==200&&batch[0].Before==205,"full batches retain remainder and exclude other recipes and locked food");
  var funds=CookingSales.Plan(stocks,3001,100,29);
  Check(funds.Single().Count==5,"planting funding uses the minimum whole dishes, not threshold batches");
  Check(funds.Sum(x=>(long)x.Count*x.Price)>=29&&funds.Sum(x=>(long)x.Count*x.Price)-7<29,"funding excess is less than one dish's value");
  Check(CookingSales.Plan(new[]{Stock(1,3001,99)},3001,100,0).Length==0,"wait below accumulated threshold");
  Check(CookingSales.Plan(new[]{Stock(1,3001,30),Stock(2,3001,175)},3001,100,0).Sum(x=>x.Count)==200,"multiple unlocked stacks form one complete batch");
  Check(CookingSales.Plan(Enumerable.Range(1,40).Select(i=>Stock(i,3001,7)),3001,100,0).Sum(x=>x.Count)==200,"native line limit preserves complete batches");
  Check(CookingSales.Plan(new[]{Stock(1,3001,0)},3001,100,1).Length==0,"empty stacks do not consume native request lines");
  Check(CookingSales.Plan(stocks,3001,100,long.MaxValue).Sum(x=>x.Count)==205,"insufficient food sells at most the available unlocked stock without overflow");
  Check(CookingSales.Plan(new[]{Stock(1,3001,300,true)},3001,100,5).Length==0,"locked food never funds planting");
  var defaults=JsonSerializer.Deserialize<TerritorySettings>("{\"Cooking\":true}")!;
  Check(defaults.SellCookingBatches&&defaults.SellCookingForPlanting&&defaults.CookingSaleBatch==100,"existing auto-cooking settings adopt approved sale defaults");
  Check(!new TerritorySettings{CookingSaleBatch=0}.ValidSettings()&&!new TerritorySettings{CookingSaleBatch=9901}.ValidSettings(),"invalid sale batch is rejected");
  var p=new RecipeBatchProgress{Account="A",BudgetOwner="run",Spent=90};
  Check(!PlantingTransaction.WithinBudget(p,"run",100,3,4),"fundraising cannot make an over-budget planting batch legal");
  // Simulate repeated full groups with a small starting balance. Sell only enough
  // food for each group; no use of unrelated high-value dishes is permitted.
  long points=2;int dishes=100;long sold=0;
  for(int i=0;i<20;i++)
  {
   long missing=Math.Max(0,30-points);
   if(missing>0){var sale=CookingSales.Plan(new[]{Stock(1,3001,dishes),Stock(2,3002,9999)},3001,100,missing);int n=sale.Sum(x=>x.Count);points+=n*7;dishes-=n;sold+=n;}
   Check(points>=30,"small balance sustains the next native planting group");points-=30;dishes+=5;
  }
  Check(points>=0&&dishes>0&&sold>0,"cooking and sales maintain a positive planting cycle");
  Console.WriteLine("Cooking sale checks: "+checks);
 }
}
