using System;
using System.Collections.Generic;
using System.Linq;
using BD2Territory;
internal static class LocalNavigationTests
{
 public static int Run()
 {
  int checks=0;void Check(bool pass,string label){checks++;if(!pass)throw new Exception("Local navigation: "+label);}
  bool Segment(RoutePoint a,RoutePoint b,Func<RoutePoint,bool> free)
  {int n=Math.Max(1,(int)Math.Ceiling(RoutePoint.Distance(a,b)/.025));for(int i=0;i<=n;i++)if(!free(new RoutePoint(a.X+(b.X-a.X)*i/n,0,a.Z+(b.Z-a.Z)*i/n)))return false;return true;}
  LocalRouteSearch Search(RoutePoint start,RoutePoint[] goals,Func<RoutePoint,bool> free,int cap=18000)
  {
   var search=new LocalRouteSearch(start,goals,p=>free(p)?p:null,(a,b)=>Segment(a,b,free),.4,8,cap);
   int frames=0;while(search.State==RouteSearchState.Searching&&frames++<10000){int old=search.Expanded;search.Step(31,1000);Check(search.Expanded-old<=31,"bounded work per update");}
   Check(search.State!=RouteSearchState.Searching,"finite planning");
   if(search.State==RouteSearchState.Found)for(int i=1;i<search.Path.Length;i++)Check(Segment(search.Path[i-1],search.Path[i],free),"whole capsule corridor, including goal connector");
   return search;
  }
  bool Box(RoutePoint p,double x1,double z1,double x2,double z2)=>p.X>=x1&&p.X<=x2&&p.Z>=z1&&p.Z<=z2;
  var straight=Search(new(0,0,0),new[]{new RoutePoint(5,0,0)},p=>true);Check(straight.State==RouteSearchState.Found,"missing NavMesh still produces a physical walking route");
  var u=Search(new(0,0,-2),new[]{new RoutePoint(0,0,2)},p=>!Box(p,-2.4,-.4,2.4,.4)&&!Box(p,-2.4,0,-1.6,5)&&!Box(p,1.6,0,2.4,5));
  Check(u.State==RouteSearchState.Found&&u.Path.Any(p=>p.Z>5),"walk around U fence, even when initially moving away from goal");
  var zig=Search(new(0,0,0),new[]{new RoutePoint(7,0,0)},p=>!Box(p,1.5,-5,2.5,1)&&!Box(p,4,-1,5,5));
  Check(zig.State==RouteSearchState.Found&&zig.Path.Any(p=>p.Z>1)&&zig.Path.Any(p=>p.Z< -1),"multiple obstacles cannot be solved by one corner; use multiple turns");
  var enclosed=Search(new(0,0,0),new[]{new RoutePoint(4,0,0)},p=>RoutePoint.Distance(p,new RoutePoint(0,0,0))<1.0||RoutePoint.Distance(p,new RoutePoint(4,0,0))<1.0);
  Check(enclosed.State==RouteSearchState.Exhausted,"true enclosure exhausts instead of walking through walls");
  var narrow=Search(new(0,0,0),new[]{new RoutePoint(4,0,0)},p=>Math.Abs(p.Z)<.22);
  Check(narrow.State==RouteSearchState.Found,"narrow clear corridor is retained");
  var capped=Search(new(0,0,0),new[]{new RoutePoint(30,0,0)},p=>true,5);Check(capped.State==RouteSearchState.Exhausted&&capped.Expanded==5,"total expansion cap");
  var changed=Search(new(0,0,0),new[]{new RoutePoint(5,0,0)},p=>!Box(p,1.7,-.6,2.3,.6));
  Check(changed.State==RouteSearchState.Found&&changed.Path.Any(p=>Math.Abs(p.Z)>.6),"new stationary worker replans around live occupancy");
  // Saved scene: 18 ore detectors have radius .8, their rotated boxes have AABB width 1.414.
  Check(.707+.24+.12>.8,"old inflated AABB excluded every .8 m ore stand before pathfinding");
  bool OreFree(RoutePoint p)
  {double x=(p.X+p.Z)/Math.Sqrt(2),z=(p.Z-p.X)/Math.Sqrt(2);double dx=Math.Max(0,Math.Abs(x)-.5),dz=Math.Max(0,Math.Abs(z)-.5);return Math.Sqrt(dx*dx+dz*dz)>.24;}
  foreach(double angle in new[]{0d,.13,.5,1d,2d,3d})
  {
   var start=new RoutePoint(3*Math.Cos(angle),0,3*Math.Sin(angle));
   var goals=LocalStandPoints.Create(new(0,0,0),start,.8).Where(OreFree).ToArray();
   Check(goals.Length>0&&goals.All(p=>RoutePoint.Distance(p,new(0,0,0))<.8),"real rotated box admits reachable stands inside ore sector");
   Check(Search(start,goals,OreFree).State==RouteSearchState.Found,"route into narrow ore interaction stand");
  }
  long now=DateTime.UtcNow.Ticks;var budget=new LocalRecoveryBudget();
  for(int i=0;i<6;i++)Check(budget.Plan(now+i*TimeSpan.TicksPerSecond),"bounded replans admit obstacle changes");
  Check(!budget.Plan(now+7*TimeSpan.TicksPerSecond),"replans cannot spin forever");budget.Reset();Check(budget.Plan(now)&&!budget.Plan(now+TimeSpan.FromSeconds(90).Ticks),"absolute deadline survives replans");
  var memories=Enumerable.Range(0,18).Select(_=>new TargetFailureMemory()).ToArray();foreach(var m in memories){m.Seen(now);m.Failed(now);}
  var visited=new HashSet<int>();
  for(int pick=0;pick<60;pick++)
  {
   long t=now+TimeSpan.FromSeconds(20+pick*2).Ticks;
   if(pick%3!=2)continue;
   int index=Enumerable.Range(0,18).Where(i=>memories[i].ReviewDue(t)).OrderBy(i=>memories[i].WaitingSince).First();
   memories[index].Selected(t);memories[index].Failed(t);visited.Add(index);
  }
  Check(visited.Count==18,"all 18 blocked ores get another turn even with endless fresh crops in other slots");
  var retry=new TargetFailureMemory();retry.Seen(now);Check(!retry.ReviewDue(now+TimeSpan.FromSeconds(119).Ticks)&&retry.ReviewDue(now+TimeSpan.FromSeconds(120).Ticks),"unselected targets cannot starve silently");
  for(int i=0;i<30;i++)retry.Failed(now);Check(retry.Until==now+TimeSpan.FromSeconds(60).Ticks,"no permanent blacklist or ten-minute cooldown after many failures");
  retry.Succeeded();Check(retry.Failures==0&&retry.Available(now),"actual gathering progress resets failure memory");
  Console.WriteLine("Local navigation checks: "+checks);return checks;
 }
}
