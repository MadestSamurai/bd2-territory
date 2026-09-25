using System;
using System.Linq;
using UnityEngine;
using FieldEvent.Life.Chunk;
namespace BD2Territory.Runtime
{
 internal sealed partial class RuntimeEngine
 {
  private int TestWater()
  {
   int count=0;void Check(bool ok,string name){count++;if(!ok)throw new Exception("Water guard: "+name);}
   player.transform.position=new(-1,0,0);var body=new CharacterController{skinWidth=0};move.SetBody(body);body.transform.position=player.transform.position;body.transform.parent=player.transform;
   terrainController=new();var gm=terrainController.Ground;
   gm.Chunk.Read=(x,z)=>x>=25&&x<=27?CellKind.Water:CellKind.Ground;
   var floor=new Collider{Owner=gm,bounds=new(){min=new(-30,-1,-30),max=new(30,0,30)}};
   Physics.Reset();Physics.Ray=p=>new[]{new RaycastHit{collider=floor,point=new(p.x,0,p.z),normal=Vector3.up,distance=p.y}};
   localTrial=false;Check(!GroundPoint(new(1,0,0),out var ground),"large collision floor underneath water cannot authorize walking");
   localTrial=true;Check(!GroundPoint(new(1,0,0),out ground)&&!StaticLocalEdge(new(-1,0,0),new(3,0,0),false,true),"normal-movement fallback cannot cross water");
   Check(!SurfaceSegmentAllowed(new(-1,0,0),new(3,0,0))&&!SurfacePathAllowed(new[]{new Vector3(-1,0,0),new Vector3(3,0,0)}),"dash and native path checks cannot cross unbridged water");
   var lpo=new LifePlaceableObject();var gate=new LifePlaceableObject_ColliderGate{Owner=lpo};var box=gate.gateCollider;box.center=new(.975f,1,2);box.size=new(3,2,1.3f);terrainGates=new[]{gate};
   Check(SurfaceSegmentAllowed(new(-1,0,2),new(3,0,2))&&GroundPoint(new(1,0,2),out ground),"existing bridge admits its crossing corridor");
   Check(!SurfaceSegmentAllowed(new(-1,0,2.6f),new(3,0,2.6f)),"bridge cells do not authorize water beside its real width");
   lpo.Temporary=true;Check(!SurfaceAllowed(new(1,0,2)),"temporary bridge preview is not a crossing");lpo.Temporary=false;
   lpo.Db=null;Check(!SurfaceAllowed(new(1,0,2)),"unplaced bridge without server object is not a crossing");lpo.Db=new();
   lpo.Water=false;Check(!SurfaceAllowed(new(1,0,2)),"generic dry-land gate is not a bridge");lpo.Water=true;
   gate.isActiveAndEnabled=false;Check(!SurfaceAllowed(new(1,0,2)),"disabled native gate is not a bridge");gate.isActiveAndEnabled=true;
   Physics.IgnoredPairs.Add(floor);Check(GroundPoint(new(1,0,2),out ground),"native bridge gate ignoring the broad floor does not erase bridge support");
   Check(!GroundPoint(new(1,0,0),out ground),"globally ignored floor still cannot authorize water outside bridge");Physics.IgnoredPairs.Clear();
   // The rotated bridge must use its oriented box, never the enclosing world AABB.
   gm.Chunk.Read=(x,z)=>CellKind.Water;box.center=new(0,1,0);box.size=new(3,2,1);box.transform.eulerAngles=new(0,45,0);
   Check(SurfaceAllowed(box.transform.TransformPoint(new(.5f,0,0))),"rotated bridge lane remains valid");
   Check(!SurfaceAllowed(new(.8f,0,.8f)),"rotated bridge AABB corner remains water");
   // Native map rotation and centring, including a tiny diagonal water corner.
   terrainGates=Array.Empty<LifePlaceableObject_ColliderGate>();gm.Chunk.Read=(x,z)=>x==26&&z==26?CellKind.Water:CellKind.Ground;
   gm.transform.position=new(3,0,-4);gm.transform.eulerAngles=new(0,45,0);
   var a=gm.transform.TransformPoint(new(.649f,0,.651f));var b=gm.transform.TransformPoint(new(.655f,0,.649f));
   Check(SurfaceAllowed(a)&&SurfaceAllowed(b)&&!SurfaceSegmentAllowed(a,b),"exact cell boundaries catch water between two dry endpoints on rotated map");
   gm.Ready=false;Check(!SurfaceAllowed(a)&&!SurfaceSegmentAllowed(a,b),"unavailable terrain data fails closed");gm.Ready=true;
   gm.transform.position=new();gm.transform.eulerAngles=new();gm.Chunk.Read=(x,z)=>x>=25&&x<=27?CellKind.Water:CellKind.Ground;
   box.transform.eulerAngles=new();box.center=new(.975f,1,2);box.size=new(3,2,1.3f);terrainGates=new[]{gate};
   localOrigin=player.transform.position;localBlocked.Clear();localEdges.Clear();localTrial=false;
   var search=new BD2Territory.AdaptiveLocalRoute(new(-1,0,0),new[]{new BD2Territory.RoutePoint(3,0,0)},LocalSample,PlanEdge);
   for(int i=0;i<1000&&search.State==BD2Territory.RouteSearchState.Searching;i++)search.Step(256,1000);
   Check(search.State==BD2Territory.RouteSearchState.Found&&search.Path.Any(p=>p.Z>1.5),"A* takes bridge instead of shorter water shortcut");
   Check(search.Path.Zip(search.Path.Skip(1),(x,y)=>SurfaceSegmentAllowed(Vector(x),Vector(y))).All(x=>x),"every planned segment has dry ground or bridge coverage");
   gate.isActiveAndEnabled=false;Check(!SurfaceSegmentAllowed(new(-1,0,2),new(3,0,2)),"removed bridge invalidates a previously valid route immediately");
   long now=TimeSpan.FromDays(3).Ticks;ResetLocalRecovery();walkTarget=new LifeGatheringObject();walkTarget.transform.position=new(3,0,0);localTarget=walkTarget.GetInstanceID();localBudget.Plan(now);allowGoals=false;
   SetLocalRoute(new[]{new BD2Territory.RoutePoint(-1,0,0),new BD2Territory.RoutePoint(3,0,0)},now);int before=TerritoryBindings.Inputs;
   ContinueLocal(new BD2Territory.TerritorySnapshot(),now,new BD2Territory.TerritoryControl());
   Check(TerritoryBindings.Inputs==before,"follower rejects old route through water before issuing a move");
   Physics.Reset();Console.WriteLine("Water and native bridge checks: "+count);return count;
  }
 }
}
