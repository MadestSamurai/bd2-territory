using System;
using System.Linq;
using System.Text.Json;
using System.Runtime.Serialization.Json;
using System.IO;
using BD2Territory;
internal static class LayoutRecipeTests
{
 internal static int Run()
 {
  int n=0;void Check(bool ok,string why){n++;if(!ok)throw new Exception(why);}void Reject(Action a,string why){bool caught=false;try{a();}catch(InvalidOperationException){caught=true;}Check(caught,why);}
  var crops=new[]{new CropStock{SeedId=2,Required=40,Yield=1},new CropStock{SeedId=15,Required=20,Yield=1}};var p=new RecipeBatchProgress{RecipeId=7,Account="one"};
  for(int i=0;i<120;i++){int id=RecipeBatchPlanner.Choose(p,crops);RecipeBatchPlanner.Confirm(p,id,100,"receipt-"+i);crops.Single(v=>v.SeedId==id).Inventory+=100;}
  Check(crops[0].Inventory==8000&&crops[1].Inventory==4000,"two-ingredient recipe maintains 2:1 across 120 batches");
  Check(RecipeBatchPlanner.Choose(new(),new[]{new CropStock{SeedId=1,Required=1,Yield=2}})==1,"one-ingredient future recipe supported");
  Reject(()=>RecipeBatchPlanner.Choose(new(),Array.Empty<CropStock>()),"empty recipe rejected");
  Reject(()=>RecipeBatchPlanner.Choose(new(),new CropStock[]{null}),"null ingredient rejected");
  Reject(()=>RecipeBatchPlanner.Choose(new(),new[]{new CropStock{SeedId=1,Required=1},new CropStock{SeedId=1,Required=2}}),"ambiguous seed rejected");
  p.Spent=713;p.BudgetOwner="lease";p.PendingToken="pending";int old=p.RecipeId;
  Check(!RecipeBatchPlanner.SwitchRecipe(p,13,false)&&p.RecipeId==old,"cannot switch during unresolved payment");p.PendingToken="";
  Check(!RecipeBatchPlanner.SwitchRecipe(p,13,true)&&p.RecipeId==old,"cannot switch live preview");
  Check(RecipeBatchPlanner.SwitchRecipe(p,13,false)&&p.SeedId==0&&p.Planted==0&&p.Spent==713&&p.BudgetOwner=="lease"&&p.Batches==120,"recipe switch preserves account spending and completed batches");
  var world=new LayoutWorld{Account="a",WorldId=3,Chunks=new[]{new LayoutChunk{Id=1},new LayoutChunk{Id=2,X=1}},Catalog=new[]{new LayoutItem{Id=30008,Name="农田",Function=3,Layer=3,MaxCount=100,Unlocked=true,Costs=new[]{new LayoutCost{Type=66,Id=1011,Name="木材",Count=5,Owned=2000},new LayoutCost{Type=66,Id=1061,Name="石材",Count=5,Owned=2000},new LayoutCost{Type=66,Id=1071,Name="材料",Count=3,Owned=2000}}}}};
  world.Objects=Enumerable.Range(0,9).Select(i=>new LayoutEntry{ObjectId=30008,ChunkId=2,X=i,Y=0}).ToArray();
  var doc=LayoutPlanner.Template(world,1,3);var quote=LayoutPlanner.Quote(world,doc);
  Check(doc.Objects.Length==100&&doc.Objects.Select(v=>v.Key).Distinct().Count()==100,"contiguous hundred-cell template");
  Check(quote.Moved==9&&quote.Purchased==91&&quote.Existing==0,"starting nine fields are moved rather than buying over cap");
  Check(quote.Costs.Single(v=>v.Id==1011).Count==455&&quote.Costs.Single(v=>v.Id==1071).Count==273,"cost based only on 91 new fields");
  Check(quote.Sources.Where(v=>v!=null).Select(v=>v.Key).Distinct().Count()==9,"each source reused at most once");
  var signature=quote.Signature;world.Catalog[0].Costs[0].Owned++;Check(LayoutPlanner.Quote(world,doc).Signature==signature,"extra income need not invalidate approved fixed cost");
  world.Catalog[0].Costs[0].Count++;Check(LayoutPlanner.Quote(world,doc).Signature!=signature,"changed price requires new preview");world.Catalog[0].Costs[0].Count--;
  world.Account="b";Check(LayoutPlanner.Quote(world,doc).Signature!=signature,"account cannot reuse another quote");world.Account="a";
  for(int i=0;i<quote.Missing.Length;i++)
  {
   var objects=world.Objects.ToList();if(quote.Sources[i]!=null)objects.RemoveAll(v=>v.Key==quote.Sources[i].Key);objects.Add(quote.Missing[i]);world.Objects=objects.ToArray();
   var remaining=LayoutPlanner.Quote(world,doc);Check(remaining.Missing.Length==99-i,"resume skips every confirmed placement "+i);
   Check(remaining.Purchased<=91,"partial resume never repurchases a completed field");
  }
  var completed=LayoutPlanner.Quote(world,doc);Check(completed.Existing==100&&completed.Missing.Length==0&&completed.Costs.Length==0,"repeat import is free no-op");
  var portable=JsonSerializer.Deserialize<LayoutDocument>(JsonSerializer.Serialize(doc));Check(LayoutPlanner.Quote(world,portable).Missing.Length==0,"file export/import roundtrip");
  using(var stream=new MemoryStream()){new DataContractJsonSerializer(typeof(LayoutQuote)).WriteObject(stream,quote);stream.Position=0;var rt=(LayoutQuote)new DataContractJsonSerializer(typeof(LayoutQuote)).ReadObject(stream);Check(rt.Moved==9&&rt.Purchased==91,"runtime serializer preserves null/new vs move sources");}
  var original=doc.Objects;doc.Objects=new[]{original[0],original[0]};Reject(()=>LayoutPlanner.Quote(world,doc),"duplicate input rejected");doc.Objects=original;
  original[0].X=-1;Reject(()=>LayoutPlanner.Quote(world,doc),"negative coordinate rejected");original[0].X=0;
  original[0].Rotate=90;Reject(()=>LayoutPlanner.Quote(world,doc),"rotation uses native index not arbitrary degrees");original[0].Rotate=0;
  doc.WorldId=4;Reject(()=>LayoutPlanner.Quote(world,doc),"wrong world rejected");doc.WorldId=3;
  world.Chunks=Array.Empty<LayoutChunk>();Reject(()=>LayoutPlanner.Quote(world,doc),"locked chunk rejected");world.Chunks=new[]{new LayoutChunk{Id=1},new LayoutChunk{Id=2}};
  var extra=new LayoutDocument{WorldId=3,Objects=original.Concat(new[]{new LayoutEntry{ObjectId=30008,ChunkId=2}}).ToArray()};Reject(()=>LayoutPlanner.Quote(world,extra),"101 fields rejected");
  world.Objects=Array.Empty<LayoutEntry>();world.Catalog[0].Costs[0].Owned=2;Check(LayoutPlanner.Quote(world,doc).Costs.Single(v=>v.Id==1011).Missing==498,"missing currency/materials shown without charging");
  world.Catalog[0].Unlocked=false;Reject(()=>LayoutPlanner.Quote(world,doc),"locked facility rejected");world.Catalog[0].Unlocked=true;
  world.Catalog=Array.Empty<LayoutItem>();Reject(()=>LayoutPlanner.Quote(world,doc),"unsupported facility explicitly rejected");
  return n;
 }
}
