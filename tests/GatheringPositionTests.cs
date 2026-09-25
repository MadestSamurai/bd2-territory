using System;
using System.Linq;
using BD2Territory;
using UnityEngine;
public class LifeGatheringObject:Component {public int Function=1;}
namespace BD2Territory.Runtime
{
 internal sealed partial class RuntimeEngine
 {
  private bool gatheringReposition,inRange=true,standClear=true;
  private float BodyRadius=>.24f;
  private static int Kind(LifeGatheringObject n)=>n.Function;
  private static float FlatDistance(Vector3 a,Vector3 b)=>(float)RoutePoint.Distance(Point(a),Point(b));
  private bool InInteractionRange(Component target,Vector3 point)=>inRange;
  private bool StandClear(Vector3 point)=>standClear;
  internal int ExerciseGatheringPositions()
  {
   int checks=0;void Check(bool pass,string label){checks++;if(!pass)throw new Exception("Gathering stand: "+label);}
   // Recorded tree: narrow collider shifted 0.15 m from its detector root. Axe hits the collider.
   var tree=new LifeGatheringObject{Id=701,HitBox=new Collider{Closest=p=>new Vector3(Math.Clamp(p.x,-.2f,.2f),p.y,Math.Clamp(p.z,-.35f,.05f))}};
   var edge=new Vector3(0,0,.96f);var near=new Vector3(0,0,.4f);
   Check(!CanGatherAt(tree,edge),"inside 1m detector but too far from offset trunk");
   Check(CanGatherAt(tree,near),"capsule-clear close stand reaches actual trunk");
   var goals=LocalStandPoints.Create(new(0,0,0),new(0,0,2),1,true).Where(p=>p.Z>0&&Math.Abs(p.X)<.01&&p.Z>=.29&&CanGatherAt(tree,new((float)p.X,0,(float)p.Z))).ToArray();
   Check(goals.Length>0,"nearby approach does not force a detour to opposite side of offset trunk");
   Check(!GatherStandReached(tree,edge,false,true),"normal detector arrival is not axe reach");
   Check(GatherStandReached(tree,near,false,true),"ordinary route may finish early at a valid stand");
   gatheringReposition=true;
   Check(!GatherStandReached(tree,near,false,true),"regression: new path generated while still at old detected position is not arrival");
   Check(GatherStandReached(tree,near,true,true),"forced reposition completes at a genuinely reached valid destination");
   long now=DateTime.UtcNow.Ticks;gatheringStands.Record(tree.Id,Point(near),now);
   Check(!GatherStandReached(tree,near,true,true),"old empty-swing position remains rejected even if planner calls it arrived");
   var opposite=new Vector3(0,0,-.7f);Check(GatherStandReached(tree,opposite,true,true),"new side can be accepted");
   var neighbor=new LifeGatheringObject{Id=702,HitBox=tree.HitBox};Check(CanGatherAt(neighbor,near),"failed stance is scoped to target");
   Check(!CanGatherAt(tree,near),"switching back cannot forget the failed stance");
   gatheringStands.Succeeded(tree.Id);Check(CanGatherAt(tree,near),"real target hit resets stance memory");
   standClear=false;Check(GatherStandReached(tree,near,true,true),"actual game detection and reach override a speculative occupancy rejection");standClear=true;
   inRange=false;Check(!CanGatherAt(tree,near),"trunk clearance never bypasses native detection range");inRange=true;
   tree.HitBox.enabled=false;Check(!CanGatherAt(tree,near),"missing hit surface cannot become a successful arrival");tree.HitBox.enabled=true;
   tree.Function=2;Check(CanGatherAt(tree,edge),"mining keeps its own range, not the narrow tree rule");
   var memory=new GatheringStandMemory();memory.Record(701,new(1,0,2),now);
   Check(!memory.Allows(701,new(1.1,9,2),now),"stance separation uses planar distance, not sampled ground height");
   Check(memory.Allows(701,new(1.3,0,2),now),"different stance is available");
   Check(memory.Allows(701,new(1,0,2),now+TimeSpan.FromMinutes(5).Ticks),"temporary memory can recover from changed terrain");
   memory.Clear();Check(memory.Allows(701,new(1,0,2),now),"scene change clears old instance identities");
   // An adjacent reward settles safely but cannot reset repeated misses against our selected tree.
   var g=new GatheringProgress();g.Select(7);g.Reposition();g.Issued(now,38,10);
   g.Observe(now+TimeSpan.FromMilliseconds(100).Ticks,true,false,false,38,10);
   g.Observe(now+TimeSpan.FromSeconds(1).Ticks,false,false,false,38,11);
   Check(g.Observe(now+TimeSpan.FromMilliseconds(1700).Ticks,false,false,false,38,11)&&g.Progressed&&!g.TargetProgressed&&g.NeedsReposition&&g.Repositions==1,"adjacent receipts cannot erase bounded recovery attempts");
   return checks;
  }
 }
 internal static class GatheringPositionTests
 {internal static void Run(){Console.WriteLine("Production gathering-position checks: "+new RuntimeEngine().ExerciseGatheringPositions());}}
}
