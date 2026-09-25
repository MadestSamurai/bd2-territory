using BD2Territory;
internal static class AdaptiveNavigationTests
{
 public static int Run()
 {
  int checks=0;void Check(bool pass,string label){checks++;if(!pass)throw new Exception("Adaptive navigation: "+label);}
  bool Segment(RoutePoint a,RoutePoint b,Func<RoutePoint,bool> free)
  {int n=Math.Max(1,(int)Math.Ceiling(RoutePoint.Distance(a,b)/.01));for(int i=0;i<=n;i++)if(!free(new RoutePoint(a.X+(b.X-a.X)*i/n,0,a.Z+(b.Z-a.Z)*i/n)))return false;return true;}
  bool Quarry(RoutePoint p)=>p.X<1||p.X>3||Math.Abs(p.Z-.2)<.09;
  var start=new RoutePoint(0,0,0);var end=new RoutePoint(4,0,.2);
  var old=new LocalRouteSearch(start,new[]{end},p=>Quarry(p)?p:null,(a,b)=>Segment(a,b,Quarry));while(old.State==RouteSearchState.Searching)old.Step(256,1000);
  Check(old.State==RouteSearchState.Exhausted,"reproduces coarse lattice missing quarry exit");
  long now=DateTime.UtcNow.Ticks;int casts=0;
  var memo=new RouteMemo<RouteSampleKey,RoutePoint?>(24000,TimeSpan.FromSeconds(30).Ticks);
  RoutePoint? Sample(RoutePoint p)=>memo.Get(new RouteSampleKey(p),now,()=>{casts++;return Quarry(p)?p:null;});
  AdaptiveLocalRoute Solve(RoutePoint a,RoutePoint b)
  {
   var plan=new AdaptiveLocalRoute(a,new[]{b},Sample,(x,y)=>Segment(x,y,Quarry));int frames=0;
   while(plan.State==RouteSearchState.Searching&&frames++<2000){int before=plan.Expanded;plan.Step(97,1000);Check(plan.Expanded-before<=97,"fallback retains per-frame node budget");}
   Check(plan.State==RouteSearchState.Found,"shifted narrow quarry corridor is reachable");
   for(int i=1;i<plan.Path.Length;i++)Check(Segment(plan.Path[i-1],plan.Path[i],Quarry),"refinement cannot cut a corner or skip a collision");return plan;
  }
  var outward=Solve(start,end);Check(outward.Cell==.1,"coarse failure activates fine grid");int cold=casts;Solve(start,end);int warm=casts-cold;Check(warm==0,"same geometry reuses all floor queries across replans");
  Solve(end,start);Check(memo.Hits>0,"reverse route shares terrain cache");
  var timedRefinement=new AdaptiveLocalRoute(start,new[]{end},Sample,(a,b)=>Segment(a,b,Quarry));timedRefinement.Step(1,1000);int expanded=timedRefinement.Expanded;
  Check(timedRefinement.Refine()&&timedRefinement.Expanded==expanded&&timedRefinement.Cell==.1&&!timedRefinement.Refine(),"time-triggered refinement preserves work counters and cannot repeatedly reset search");
  var sealedPlan=new AdaptiveLocalRoute(start,new[]{end},p=>RoutePoint.Distance(p,start)<.3||RoutePoint.Distance(p,end)<.3?p:null,(a,b)=>Segment(a,b,p=>RoutePoint.Distance(p,start)<.3||RoutePoint.Distance(p,end)<.3));
  while(sealedPlan.State==RouteSearchState.Searching)sealedPlan.Step(100,1000);Check(sealedPlan.State==RouteSearchState.Exhausted,"genuine enclosure terminates even after refinement");
  int reads=0;var bounded=new RouteMemo<int,int>(3,10);for(int i=0;i<100;i++)bounded.Get(i,now,()=>++reads);Check(bounded.Count==3,"cache memory is bounded");
  bounded.Get(99,now+9,()=>++reads);Check(reads==100,"cache valid within TTL");bounded.Get(99,now+10,()=>++reads);Check(reads==101,"cache expires at deadline");bounded.RemoveWhere(k=>k==99);bounded.Get(99,now+10,()=>++reads);Check(reads==102,"changed local obstacles invalidate cached entries");bounded.Clear();Check(bounded.Count==0,"scene and actor changes clear geometry");
  var memory=new LocalRouteMemory();memory.Save(5,now,outward.Path);var reused=memory.Reuse(5,now,start,p=>true,(a,b)=>Segment(a,b,Quarry));Check(reused!=null,"nearby saved route joins a validated corridor");
  Check(memory.Reuse(5,now,start,p=>false,(a,b)=>true)==null,"blocked destination invalidates reuse");Check(memory.Reuse(5,now,start,p=>true,(a,b)=>false)==null,"new blocker on connector invalidates reuse");Check(memory.Reuse(6,now,start,p=>true,(a,b)=>true)==null,"target identities cannot share cached route");Check(memory.Reuse(5,now+TimeSpan.FromDays(1).Ticks,start,p=>true,(a,b)=>true)!=null,"unchanged route survives time passage");memory.Save(5,now,outward.Path);memory.Forget(5);Check(memory.Reuse(5,now,start,p=>true,(a,b)=>true)==null,"failed execution cannot reuse the same stale route");
  var staticMemo=new RouteMemo<int,int>(3);int staticReads=0;
  staticMemo.Get(1,now,()=>++staticReads);staticMemo.Get(1,now+TimeSpan.FromDays(3).Ticks,()=>++staticReads);
  Check(staticReads==1,"static terrain has no timer expiration");
  var geometry=new RouteGeometryChanges();
  RouteGeometryStamp Stamp(int id,string rev,double x)=>new(){Id=id,Revision=rev,Region=new(x-1,-1,x+1,1)};
  geometry.Update(new[]{Stamp(1,"placed",0),Stamp(2,"ore",20)});
  Check(geometry.Update(new[]{Stamp(2,"ore",20),Stamp(1,"placed",0)}).Length==0,"scan order and cooking inventory changes do not dirty geometry");
  var moved=geometry.Update(new[]{Stamp(1,"moved",5),Stamp(2,"ore",20)});
  Check(moved.Length==2&&moved.Any(r=>r.Contains(new(0,0,0)))&&moved.Any(r=>r.Contains(new(5,0,0)))&&!moved.Any(r=>r.Contains(new(20,0,0))),"moving one object invalidates both footprints, not other regions");
  var harvested=geometry.Update(new[]{Stamp(1,"moved",5)});Check(harvested.Length==1&&harvested[0].Contains(new(20,0,0)),"harvesting removes only affected collision region");
  Check(geometry.Update(new[]{Stamp(1,"moved",5)}).Length==0,"stable geometry needs no further physics queries");
  var corridors=new LocalRouteMemory();corridors.Save(99,now,new[]{new RoutePoint(0,0,0),new RoutePoint(1,0,0),new RoutePoint(2,0,0),new RoutePoint(3,0,0),new RoutePoint(4,0,0)});
  var back=corridors.ReuseCorridor(new(4.2,0,0),new[]{new RoutePoint(-.2,0,0)},(a,b)=>true);
  Check(back!=null&&back.First().X==4.2&&back.Last().X==-.2,"different target reuses corridor in reverse");
  Check(corridors.ReuseCorridor(new(4.2,0,0),new[]{new RoutePoint(-.2,0,0)},(a,b)=>false)==null,"blocked new connectors reject corridor reuse");
  corridors.Invalidate(new RouteRegion(10,10,11,11));Check(corridors.ReuseCorridor(new(4,0,0),new[]{new RoutePoint(0,0,0)},(a,b)=>true)!=null,"remote geometry change retains existing route");
  corridors.Invalidate(new RouteRegion(1.4,-.1,1.6,.1));Check(corridors.ReuseCorridor(new(4,0,0),new[]{new RoutePoint(0,0,0)},(a,b)=>true)==null,"changed edge invalidates route even when endpoints are outside region");
  // A low approach first rejects an upper cell. Taking the staircase must let the
  // same X/Z be sampled again at the newly reached height.
  var floors=new Dictionary<(int,int),double>{{(0,0),0},{(0,1),.3},{(0,2),.6},{(1,2),.9},{(1,1),1},{(1,0),1},{(2,0),1}};
  int lowFailures=0,highSuccess=0;
  RoutePoint? StairFloor(RoutePoint p)
  {
   if(!floors.TryGetValue(((int)p.X,(int)p.Z),out var h)||Math.Abs(h-p.Y)>.31){if(p.X==1&&p.Z==0)lowFailures++;return null;}
   if(p.X==1&&p.Z==0)highSuccess++;return new RoutePoint(p.X,h,p.Z);
  }
  var layered=new LocalRouteSearch(new(0,0,0),new[]{new RoutePoint(2,1,0)},StairFloor,(a,b)=>RoutePoint.Distance(a,b)<=1.01&&Math.Abs(a.Y-b.Y)<=.31,1,3);
  while(layered.State==RouteSearchState.Searching)layered.Step(256,1000);
  Check(lowFailures>0&&highSuccess>0&&layered.State==RouteSearchState.Found,"failed low-floor sample must not poison the same cell after climbing stairs");
  // A bridge whose axis falls between world-grid rows: native gate guides must
  // make it reachable on the coarse grid, without treating nearby water as ground.
  var bridgeStart=new RoutePoint(-12,0,3);var bridgeEnd=new RoutePoint(8,0,-3);
  bool River(RoutePoint p)=>Math.Abs(p.X)<1.8?Math.Abs(p.Z-.19)<.17:true;
  var bridgeGuides=Enumerable.Range(0,51).Select(i=>new RoutePoint(-2.5+i*.1,0,.19)).ToArray();
  AdaptiveLocalRoute RiverPlan(RoutePoint a,RoutePoint b,RoutePoint[] guides)
  {var route=new AdaptiveLocalRoute(a,new[]{b},p=>River(p)?p:null,(x,y)=>Segment(x,y,River),false,8,guides);int frames=0;while(route.State==RouteSearchState.Searching&&frames++<3000)route.Step(97,1000);return route;}
  var withoutGuides=RiverPlan(bridgeStart,bridgeEnd,null);
  var guided=RiverPlan(bridgeStart,bridgeEnd,bridgeGuides);
  Check(guided.State==RouteSearchState.Found&&guided.Cell==.4,"native bridge axis avoids full-map fine-grid fallback");
  Check(withoutGuides.Cell==.1&&guided.Expanded<withoutGuides.Expanded,"bridge guides reduce real search expansions");
  for(int i=1;i<guided.Path.Length;i++)Check(Segment(guided.Path[i-1],guided.Path[i],River),"guided crossing stays inside bridge");
  var reverse=RiverPlan(bridgeEnd,bridgeStart,bridgeGuides);Check(reverse.State==RouteSearchState.Found&&reverse.Cell==.4,"return from quarry to farm also keeps coarse grid");
  var noBridge=new AdaptiveLocalRoute(bridgeStart,new[]{bridgeEnd},p=>Math.Abs(p.X)>=1.8?p:null,(a,b)=>Segment(a,b,p=>Math.Abs(p.X)>=1.8),false,8,bridgeGuides);
  while(noBridge.State==RouteSearchState.Searching)noBridge.Step(256,1000);
  Check(noBridge.State==RouteSearchState.Exhausted,"stale guide cannot create a bridge over forbidden water");
  var longRoute=new AdaptiveLocalRoute(new(0,0,0),new[]{new RoutePoint(20,0,0)},p=>p,(a,b)=>true);
  for(int i=0;i<45;i++)longRoute.Step(1,1000);
  Check(longRoute.Cell==.4&&longRoute.Expanded==45,"long coarse frontier survives multiple wall-clock slices without restart");
  while(longRoute.State==RouteSearchState.Searching)longRoute.Step(97,1000);
  Check(longRoute.State==RouteSearchState.Found&&longRoute.Expanded<60,"long open route completes prior coarse work");
  Console.WriteLine($"Bridge search expansions: old={withoutGuides.Expanded}, guided={guided.Expanded}, return={reverse.Expanded}");
  Console.WriteLine($"Adaptive navigation checks: {checks}; cold floor queries={cold}, repeat floor queries={warm}, cache hits={memo.Hits}");return checks;
 }
}
