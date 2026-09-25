using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
namespace BD2Territory
{
 public struct RoutePoint
 {
  public double X,Y,Z;public RoutePoint(double x,double y,double z){X=x;Y=y;Z=z;}
  public static double Distance(RoutePoint a,RoutePoint b){double x=a.X-b.X,z=a.Z-b.Z;return Math.Sqrt(x*x+z*z);}
 }
 public static class LocalStandPoints
 {
  public static IEnumerable<RoutePoint> Create(RoutePoint center,RoutePoint from,double reach,bool nearSurface=false)
  {
   if(reach<=0||reach>10)yield break;double angle=Math.Atan2(from.Z-center.Z,from.X-center.X);
   foreach(double fraction in nearSurface?new[]{.96,.9,.8,.65,.5,.35}:new[]{.96,.9,.8,.65})for(int i=0;i<32;i++)
   {double a=angle+i*Math.PI/16;yield return new RoutePoint(center.X+Math.Cos(a)*reach*fraction,from.Y,center.Z+Math.Sin(a)*reach*fraction);}
  }
 }
 public enum RouteSearchState { Searching, Found, Exhausted }
 // Incremental A*: actual terrain/capsule queries supplied by the main-thread adapter, no NavMesh dependency.
 public sealed class LocalRouteSearch
 {
  private sealed class Node { public int X,Z;public RoutePoint P;public double Cost;public Node Parent; }
  private sealed class Entry {public Node Node;public double Cost,Score;public long Order;}
  private readonly SortedSet<Entry> open=new SortedSet<Entry>(Comparer<Entry>.Create((a,b)=>{int c=a.Score.CompareTo(b.Score);return c!=0?c:a.Order.CompareTo(b.Order);}));
  private readonly Dictionary<long,Node> nodes=new Dictionary<long,Node>();private readonly Dictionary<long,RoutePoint?> samples=new Dictionary<long,RoutePoint?>();
  private readonly Func<RoutePoint,RoutePoint?> sample;private readonly Func<RoutePoint,RoutePoint,bool> clear;
  private readonly RoutePoint origin,center;private readonly RoutePoint[] goals;private readonly double step,radius,minX,maxX,minZ,maxZ;private readonly int limit;private long serial;
  public RouteSearchState State{get;private set;}public RoutePoint[] Path{get;private set;}=new RoutePoint[0];public int Expanded{get;private set;}
  public override string ToString()=>"state="+State+" expanded="+Expanded+" open="+open.Count+" samples="+samples.Count+" goals="+goals.Length+" path="+Path.Length;
  private static long Key(int x,int z)=>((long)x<<32)^(uint)z;
  public LocalRouteSearch(RoutePoint start,RoutePoint[] destinations,Func<RoutePoint,RoutePoint?> samplePoint,Func<RoutePoint,RoutePoint,bool> edgeClear,double cell=.4,double margin=8,int maxNodes=18000,bool worldAligned=false)
  {
   if(cell<=0||margin<0||maxNodes<1)throw new ArgumentException("Invalid local route bounds");
   origin=worldAligned?new RoutePoint(Math.Round(start.X/cell)*cell,start.Y,Math.Round(start.Z/cell)*cell):start;goals=destinations;sample=samplePoint;clear=edgeClear;step=cell;limit=maxNodes;
   if(goals.Length==0){State=RouteSearchState.Exhausted;return;}
   center=new RoutePoint(goals.Average(p=>p.X),start.Y,goals.Average(p=>p.Z));radius=goals.Max(p=>RoutePoint.Distance(p,center));
   minX=Math.Min(start.X,goals.Min(p=>p.X))-margin;maxX=Math.Max(start.X,goals.Max(p=>p.X))+margin;
   minZ=Math.Min(start.Z,goals.Min(p=>p.Z))-margin;maxZ=Math.Max(start.Z,goals.Max(p=>p.Z))+margin;
   var first=new Node{P=start};nodes[Key(0,0)]=first;samples[Key(0,0)]=start;Push(first);
  }
  private double Estimate(RoutePoint p)=>Math.Max(0,RoutePoint.Distance(p,center)-radius);
  private void Push(Node n){open.Add(new Entry{Node=n,Cost=n.Cost,Score=n.Cost+Estimate(n.P),Order=serial++});}
  public RouteSearchState Step(int budget,double milliseconds=6)
  {
   if(State!=RouteSearchState.Searching)return State;var clock=Stopwatch.StartNew();int used=0;
   while(open.Count>0&&used<budget&&(used==0||clock.Elapsed.TotalMilliseconds<milliseconds))
   {
    var entry=open.Min;open.Remove(entry);var n=entry.Node;if(entry.Cost!=n.Cost)continue;used++;Expanded++;
    foreach(var goal in goals)if(RoutePoint.Distance(n.P,goal)<=step*1.8&&clear(n.P,goal))
    {var path=new List<RoutePoint>{goal};for(var back=n;back!=null;back=back.Parent)path.Add(back.P);path.Reverse();Path=path.ToArray();State=RouteSearchState.Found;return State;}
    if(Expanded>=limit){State=RouteSearchState.Exhausted;return State;}
    for(int dx=-1;dx<=1;dx++)for(int dz=-1;dz<=1;dz++)
    {
     if(dx==0&&dz==0)continue;int x=n.X+dx,z=n.Z+dz;long key=Key(x,z);var p=new RoutePoint(Math.Round(origin.X+x*step,5),n.P.Y,Math.Round(origin.Z+z*step,5));
     if(p.X<minX||p.X>maxX||p.Z<minZ||p.Z>maxZ)continue;
     if(!samples.TryGetValue(key,out var ground)){ground=sample(p);samples[key]=ground;}
     if(!ground.HasValue)continue;p=ground.Value;double cost=n.Cost+RoutePoint.Distance(n.P,p);
     if(nodes.TryGetValue(key,out var next)&&next.Cost<=cost)continue;
     // Sweep the whole edge; diagonal corners cannot cut through a fence or a worker.
     if(!clear(n.P,p))continue;
     if(next==null){next=new Node{X=x,Z=z};nodes[key]=next;}
     next.P=p;next.Cost=cost;next.Parent=n;Push(next);
    }
   }
   if(open.Count==0)State=RouteSearchState.Exhausted;return State;
  }
 }
 public sealed class LocalRecoveryBudget
 {
  public int Plans{get;private set;}private long started;
  public override string ToString()=>"plans="+Plans+" startedUtcTicks="+started;
  public void Reset(){Plans=0;started=0;}
  public bool Expired(long now)=>Plans>0&&now-started>=TimeSpan.FromSeconds(90).Ticks;
  public bool Plan(long now){if(Plans>=6||Expired(now))return false;if(Plans==0)started=now;Plans++;return true;}
 }
}
