using System;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
namespace BD2Territory.Runtime
{
 internal sealed partial class RuntimeEngine
 {
  private readonly RouteMemo<RouteSampleKey,RoutePoint?> localGround=new RouteMemo<RouteSampleKey,RoutePoint?>(24000,TimeSpan.FromSeconds(30).Ticks);
  private readonly RouteMemo<RouteEdgeKey,bool> localEdges=new RouteMemo<RouteEdgeKey,bool>(48000,TimeSpan.FromSeconds(30).Ticks);
  private readonly LocalRouteMemory localPaths=new LocalRouteMemory();private string localCacheContext="";private int localCacheReplies=-1,warmIndex;
  private RoutePoint? CachedGround(RoutePoint p)
  {return localGround.Get(new RouteSampleKey(p),DateTime.UtcNow.Ticks,()=>{Vector3 ground;return GroundPoint(Vector(p),out ground,false)?(RoutePoint?)Point(ground):null;});}
  private void InvalidateLocalArea(Vector3 point)
  {var p=Point(point);localGround.RemoveWhere(k=>RoutePoint.Distance(k.Point,p)<2);localEdges.RemoveWhere(k=>k.Near(p,2));}
  private void RefreshLocalCacheContext()
  {
   // World keys include chunk, building ID and position. Moving/building/removing a facility
   // invalidates cached geometry. NPC transforms are deliberately absent from this key.
   int world=0;foreach(var key in worldObjects.Keys)world^=key.GetHashCode();
   var body=Body;
   string shape=body==null?"default":body.radius.ToString("R")+"/"+body.height.ToString("R")+"/"+body.center.ToString("F4")+"/"+body.transform.lossyScale.ToString("F4")+"/"+StepHeight.ToString("R")+"/"+body.slopeLimit.ToString("R")+"/"+body.skinWidth.ToString("R");
   string context=farmScene+"/"+account+"/"+player.GetInstanceID()+"/"+RootLift.ToString("R")+"/"+shape+"/"+world+"/"+worldObjects.Count;
   if(context!=localCacheContext){localCacheContext=context;localGround.Clear();localEdges.Clear();localPaths.Clear();warmIndex=0;}
   if(localCacheReplies!=network.GatherReplies)
   {
    if(targetNode!=null)InvalidateLocalArea(targetNode.transform.position);else {localGround.Clear();localEdges.Clear();}
    localCacheReplies=network.GatherReplies;
   }
  }
  private void WarmLocalGrid()
  {
   // Only idle time, bounded to four samples / 1 ms. Do not front-load a full territory scan.
   var watch=Stopwatch.StartNew();var from=player.transform.position;
   for(int n=0;n<4&&(n==0||watch.Elapsed.TotalMilliseconds<1);n++)
   {int i=warmIndex++%121;CachedGround(new RoutePoint(Math.Round(Math.Round(from.x/.4)*.4+(i%11-5)*.4,5),from.y,Math.Round(Math.Round(from.z/.4)*.4+(i/11-5)*.4,5)));}
  }
 }
}
