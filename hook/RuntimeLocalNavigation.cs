using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using B=BD2Territory.Runtime.TerritoryBindings;
namespace BD2Territory.Runtime
{
 internal sealed partial class RuntimeEngine
 {
  private LocalRouteSearch localSearch;private Vector3[] localRoute=new Vector3[0];private int localIndex,localTarget;
  private bool localMoving;private long localPlanAt,localProgressAt;private double localBest;
  private readonly LocalRecoveryBudget localBudget=new LocalRecoveryBudget();private readonly List<Vector3> localBlocked=new List<Vector3>();
  private static RoutePoint Point(Vector3 p)=>new RoutePoint(p.x,p.y,p.z);
  private static Vector3 Vector(RoutePoint p)=>new Vector3((float)p.X,(float)p.Y,(float)p.Z);
  private static float FlatDistance(Vector3 a,Vector3 b){a.y=b.y=0;return Vector3.Distance(a,b);}
  private float RootLift
  {
   get{var agent=NavAgent();return agent==null?.18f:Mathf.Clamp(agent.baseOffset,0,.5f);}
  }
  private void BodyCapsule(Vector3 root,out Vector3 low,out Vector3 high,out float radius)
  {
   var body=player.GetComponent<CharacterController>();radius=BodyRadius;
   var center=body==null?root+Vector3.up*.4f:root+(body.transform.TransformPoint(body.center)-player.transform.position);
   float half=body==null?.4f:Math.Max(radius,body.height*Math.Abs(body.transform.lossyScale.y)*.5f);
   low=center-Vector3.up*(half-radius);high=center+Vector3.up*(half-radius);
  }
  private bool GroundPoint(Vector3 desired,out Vector3 point,bool clearance=true,bool includeNpcs=true)
  {
   point=desired;float floor=desired.y-RootLift;
   foreach(var hit in Physics.RaycastAll(new Vector3(desired.x,floor+.55f,desired.z),Vector3.down,1.3f,~0,QueryTriggerInteraction.Ignore).OrderBy(h=>h.distance))
   {
    var collider=hit.collider;if(collider==null||collider.transform.IsChildOf(player.transform)||hit.normal.y<.72f||Math.Abs(hit.point.y-floor)>.45f)continue;
    if(collider is CharacterController||collider.GetComponentInParent<LifeGatheringObject>()!=null)continue;
    point=hit.point+Vector3.up*RootLift;return !clearance||StandClear(point,includeNpcs);
   }
   return false;
  }
  private RoutePoint? LocalSample(RoutePoint p)
  {
   Vector3 point;if(!GroundPoint(Vector(p),out point)||localBlocked.Any(b=>FlatDistance(b,point)<.26f))return null;return Point(point);
  }
  private bool LocalEdge(RoutePoint a,RoutePoint b)
  {
   var from=Vector(a);var to=Vector(b);if(!NpcPathClear(from,to)||Obstacle(from,to)!=null||Math.Abs(a.Y-b.Y)>.35)return false;
   int steps=Math.Max(1,(int)Math.Ceiling(FlatDistance(from,to)/.2f));var previous=from;
   for(int i=1;i<=steps;i++)
   {var wanted=Vector3.Lerp(from,to,(float)i/steps);Vector3 ground;if(!GroundPoint(wanted,out ground,true,false)||Math.Abs(ground.y-previous.y)>.25f||localBlocked.Any(p=>FlatDistance(p,ground)<.26f))return false;previous=ground;}
   return true;
  }
  private bool InInteractionRange(Component target,Vector3 point)
  {
   if(target is LifeFarmFieldObject farm)return farm.IsPlayerInInteractionRange(point);
   var sector=target.GetComponent<CircleSectorCollider>();return sector!=null&&sector.IsDetectionInCollisionArea(point);
  }
  private IEnumerable<Vector3> StandCandidates(Component target,Vector3 center,float radius,bool retry)
  {
   var sector=target is LifeGatheringObject?target.GetComponent<CircleSectorCollider>():null;
   float reach=sector==null?Math.Max(.7f,radius):Convert.ToSingle(B.Read("Detector.Distance",sector));
   // Sample inside the interaction sector; actual capsule/shape tests decide clearance.
   var from=player.transform.position;
   foreach(var candidate in LocalStandPoints.Create(Point(center),Point(from),reach))
   {
    Vector3 p;if(!GroundPoint(Vector(candidate),out p))continue;
    if(!InInteractionRange(target,p))continue;
    if(retry&&(triedDestinations.Any(t=>FlatDistance(t,p)<.12f)||(gatheringReposition&&FlatDistance(from,p)<.25f)))continue;
    yield return p;
   }
  }
  private void ResetLocalRecovery(){localBudget.Reset();localBlocked.Clear();localTarget=0;}
  private void ClearLocalMotion(){localSearch=null;localMoving=false;localRoute=new Vector3[0];localIndex=0;}
  private bool BeginLocalRoute(Component target,TerritorySnapshot s,long now,string reason)
  {
   if(target==null||player==null||move==null)return false;
   if(localTarget!=target.GetInstanceID()){ResetLocalRecovery();localTarget=target.GetInstanceID();}
   if(!localBudget.Plan(now))return false;
   CancelVehicle();if(player.IsGetOnVehicle())player.GetOffVehicle();
   move.ClearMove();move.StopMove();var agent=NavAgent();if(agent!=null&&agent.isActiveAndEnabled&&agent.isOnNavMesh)agent.ResetPath();
   B.InvokeOn("Player.ChangeMoveType",move,B.EnumObject("MoveKind","CharController"));
   var quest=B.Singleton(typeof(QuestNavigationManager));if(quest!=null)B.InvokeOn("Quest.Pause",quest);
   navigation.CancelAttempt();ownsMove=true;walkTarget=target;walkScene=s.Scene;bypassing=false;
   var center=target is LifeFarmFieldObject farm?farm.GetFieldWorldCenter():target.transform.position;
   var goals=StandCandidates(target,center,.7f,gatheringReposition).Select(Point).ToArray();
   if(goals.Length==0){LocalStorage.Log("局部规划无站位 target="+target.GetInstanceID()+" reason="+reason);return false;}
   localSearch=new LocalRouteSearch(Point(player.transform.position),goals,LocalSample,LocalEdge,.4,localBudget.Plans==1?8:14,18000);
   localRoute=new Vector3[0];localIndex=0;localMoving=true;localPlanAt=now;
   LocalStorage.Log("实体局部规划 target="+target.GetInstanceID()+" reason="+reason+" plan="+localBudget.Plans+" goals="+goals.Length+" from="+player.transform.position);
   s.Reason="检测周围障碍，规划普通移动绕行";return true;
  }
  private void RetryLocal(TerritorySnapshot s,long now,string reason,Vector3 blocked)
  {
   move.ClearMove();move.StopMove();localBlocked.Add(blocked);CaptureFailure(reason);
   if(!BeginLocalRoute(walkTarget,s,now,reason))SkipWalkTarget(walkTarget,s,now,reason);
  }
  private void ContinueLocal(TerritorySnapshot s,long now,TerritoryControl c)
  {
   s.Reason="沿实体通路绕行，保持原目标";
   if(vehiclePending){move.StopMove();localProgressAt=now;s.Reason="等待载具加载结束后绕行";return;}
   var field=B.Read("Field.Instance",null);string state=B.Read("Field.MoveState",field)?.ToString();
   if(state=="DontMove"||state=="Anchored"){move.ClearMove();s.Reason="等待游戏恢复角色移动";return;}
   if(localBudget.Expired(now)){SkipWalkTarget(walkTarget,s,now,"local_recovery_budget");return;}
   if(localSearch!=null)
   {
    if(now-localPlanAt>TimeSpan.FromSeconds(15).Ticks){SkipWalkTarget(walkTarget,s,now,"local_planning_budget");return;}
    var status=localSearch.Step(96,6);s.Reason="扫描地面和碰撞体 · "+localSearch.Expanded+" 个路点";
    if(status==RouteSearchState.Searching)return;
    if(status==RouteSearchState.Exhausted)
    {int count=localSearch.Expanded;LocalStorage.Log("实体局部规划未找到通路 target="+walkTarget.GetInstanceID()+" expanded="+count);if(localBudget.Plans<2&&BeginLocalRoute(walkTarget,s,now,"expand_local_area"))return;SkipWalkTarget(walkTarget,s,now,"local_route_exhausted");return;}
    localRoute=localSearch.Path.Select(Vector).ToArray();finalDestination=localRoute.Last();localIndex=1;localSearch=null;localBest=double.PositiveInfinity;localProgressAt=now;
    LocalStorage.Log("实体局部路线就绪 target="+walkTarget.GetInstanceID()+" points="+localRoute.Length+" goal="+localRoute.Last());
   }
   var from=player.transform.position;
   if(!NpcStandClear(finalDestination)){RetryLocal(s,now,"npc_destination_occupied",finalDestination);return;}
   bool detected=walkTarget is LifeGatheringObject resource&&Detected(Kind(resource)).Contains(resource)&&InInteractionRange(walkTarget,from)&&StandClear(from);
   if(detected||(localIndex>=localRoute.Length&&InInteractionRange(walkTarget,from)&&StandClear(from)))
   {int id=walkTarget.GetInstanceID();StopMove();ResetLocalRecovery();navigation.Reset();interaction.Select(id);interaction.Arrived(now);gatheringReposition=false;s.Reason="绕行已到位，准备采集";return;}
   while(localIndex<localRoute.Length&&FlatDistance(from,localRoute[localIndex])<.10f&&(localIndex<localRoute.Length-1||InInteractionRange(walkTarget,from)))localIndex++;
   if(localIndex>=localRoute.Length){if(!NpcStandClear(from))RetryLocal(s,now,"npc_arrival_occupied",from);else if(!InInteractionRange(walkTarget,from))RetryLocal(s,now,"local_arrival_outside_range",from);else move.StopMove();return;}
   // Follow a visible corridor, not every grid corner. Recheck against live colliders before steering.
   for(int j=Math.Min(localRoute.Length-1,localIndex+4);j>localIndex;j--)if(FlatDistance(from,localRoute[j])<=1.4f&&LocalEdge(Point(from),Point(localRoute[j]))){localIndex=j;break;}
   var dest=localRoute[localIndex];var delta=dest-from;delta.y=0;
   var ahead=from+Vector3.ClampMagnitude(delta,.7f);
   if(!NpcPathClear(from,ahead)){RetryLocal(s,now,"npc_corridor_occupied",ahead);return;}
   var obstacle=Obstacle(from,ahead);
   if(obstacle!=null){blockingCollider=obstacle;RetryLocal(s,now,"local_new_obstacle",from+delta.normalized*.35f);return;}
   double remaining=FlatDistance(from,dest);for(int i=localIndex+1;i<localRoute.Length;i++)remaining+=FlatDistance(localRoute[i-1],localRoute[i]);
   if(localBest-remaining>.12){localBest=remaining;localProgressAt=now;}
   if(now-localProgressAt>TimeSpan.FromSeconds(2).Ticks){if(TryEscape(s,now,c,blockingCollider))return;RetryLocal(s,now,"local_motion_stalled",from+delta.normalized*.35f);return;}
   if(!LocalEdge(Point(from),Point(dest))){RetryLocal(s,now,"local_terrain_changed",dest);return;}
   // Mounting changes speed, not the movement controller. A* keeps steering via CharController.
   UpdateVehicle(now,c,remaining);
   if(vehiclePending){move.StopMove();localProgressAt=now;return;}
   if(player.IsGetOnVehicle())s.Reason="使用载具沿实体通路行进";
   B.InvokeOn("Player.Face",player,delta.normalized*Mathf.Clamp(delta.magnitude/.65f,.1f,1f));B.InvokeOn("Player.StartMove",player);lastInput=now;
  }
 }
}
