using BD2Territory.Runtime;
Console.WriteLine("Production traversal adapter checks: "+new RuntimeEngine().Test());
namespace BD2Territory.Runtime
{
 using UnityEngine;
 internal sealed partial class RuntimeEngine
 {
  private PlayerController player=new();private PlayerMoveController move=new();
  internal int Test()
  {
   terrainController=new FieldEvent.Life.LifeChunkController();
   int checks=0;void Check(bool ok,string label){checks++;if(!ok)throw new Exception(label);}
   var body=new CharacterController{skinWidth=0};body.transform.parent=player.transform;move.SetBody(body);
   Check(ReferenceEquals(Body,body),"uses native controller field, not avatar component");
   Check(Math.Abs(RootLift)<1e-6,"ground root follows actual controller foot");
   body.center=new(0,.7f,0);Check(Math.Abs(RootLift+.2)<1e-6,"capsule center is respected instead of NavMesh baseOffset");body.center=new(0,.5f,0);
   Check(Math.Abs(StepHeight-.3)<1e-6,"reads native step height");body.stepOffset=0;Check(Math.Abs(StepHeight-.3)<1e-6,"transient native recovery frame does not poison step cache");
   stepBodyId=0;normalStep=0;Check(Math.Abs(StepHeight-.3)<1e-6,"saved native step setting works even when connecting during temporary zero");
   body.stepOffset=.3f;var floor=new Collider{bounds=new Bounds{min=new(-10,-1,-10),max=new(10,0,10)}};
   var decoration=new Collider{bounds=new Bounds{min=new(-1,0,-1),max=new(1,.2f,1)}};
   Check(MovementCollider(decoration),"solid active layer considered");decoration.isTrigger=true;Check(!MovementCollider(decoration),"selection trigger ignored");decoration.isTrigger=false;
   decoration.enabled=false;Check(!MovementCollider(decoration),"disabled body ignored");decoration.enabled=true;
   decoration.gameObject.layer=7;Physics.IgnoredLayers.Add(7);Check(!MovementCollider(decoration),"native ignored layer ignored");Physics.IgnoredLayers.Clear();
   Physics.IgnoredPairs.Add(decoration);Check(!MovementCollider(decoration),"native ignored pair ignored");
   Physics.Ray=p=>new[]{new RaycastHit{collider=decoration,point=new(p.x,.2f,p.z),normal=Vector3.up,distance=.35f},new RaycastHit{collider=floor,point=new(p.x,0,p.z),normal=Vector3.up,distance=.55f}};
   Check(GroundPoint(new(),out var ground)&&Math.Abs(ground.y)<1e-6,"noncollidable decoration cannot become a fictitious floor");
   Physics.IgnoredPairs.Clear();Check(GroundPoint(new(),out ground)&&Math.Abs(ground.y-.2f)<1e-6,"real low decoration is supporting ground");
   Check(!MovementCollider(body),"own collider never obstructs its route");
   Physics.Overlap=(a,b,r)=>new[]{decoration};decoration.enabled=false;Check(StandClear(new()),"disabled NPC collider does not invent occupancy");
   decoration.enabled=true;Physics.IgnoredPairs.Add(decoration);Check(StandClear(new()),"ignored actor body does not invent occupancy");Physics.Reset();
   var hit=new RaycastHit{collider=decoration,point=new(.3f,.1f,0),normal=new(-1,0,0),distance=.3f};Physics.Hits=new[]{hit};
   Physics.Ray=p=>new[]{new RaycastHit{collider=decoration,point=new(p.x,.2f,p.z),normal=Vector3.up,distance=p.y-.2f}};
   Physics.Sweep=(a,b,r,d,n)=>a.y-r<decoration.bounds.max.y-1e-5?new[]{hit}:Array.Empty<RaycastHit>();
   Physics.Overlap=(a,b,r)=>a.y-r<.2f-1e-5?new[]{decoration}:Array.Empty<Collider>();
   Check(Obstacle(new(),new(1,0,0))==null,"horizontal sweep hitting low step is not a wall");
   Check(Math.Abs(Physics.LastRadius-.24)<1e-6,"actual body radius is not reduced by skinWidth");
   decoration.bounds=new Bounds{min=new(-1,0,-1),max=new(1,1.8f,1)};
   Check(Obstacle(new(),new(1,0,0))==decoration,"high wall not predicted as a step");
   decoration.bounds=new Bounds{min=new(-1,0,-1),max=new(1,.2f,1)};
   var ceiling=new Collider{bounds=new Bounds{min=new(-2,1.1f,-2),max=new(2,2,2)}};
   Physics.Overlap=(a,b,r)=>new[]{ceiling};Check(Obstacle(new(),new(1,0,0))==decoration,"step without headroom rejected by prediction");
   Physics.Reset();Physics.Ray=p=>new[]{new RaycastHit{collider=floor,point=new(p.x,0,p.z),normal=new(1,0,0),distance=.55f}};
   Check(!GroundPoint(new(),out ground),"wall side is not walkable floor");
   Physics.Ray=p=>Array.Empty<RaycastHit>();Check(!GroundPoint(new(),out ground),"no supporting ground remains unavailable");
   Physics.Reset();
   Physics.Ray=p=>new[]{new RaycastHit{collider=floor,point=new(p.x,0,p.z),normal=Vector3.up,distance=.55f}};
   // Execute the production follower with a false-positive collision prediction. The
   // fixture deliberately supplies no game movement; only the real 3s timer may fail it.
   decoration.bounds=new Bounds{min=new(-1,0,-1),max=new(1,1.8f,1)};Physics.Hits=new[]{hit};
   long now=TimeSpan.FromDays(1).Ticks;walkTarget=new LifeGatheringObject();localMoving=true;localTarget=walkTarget.GetInstanceID();localBudget.Plan(now);
   SetLocalRoute(new[]{new BD2Territory.RoutePoint(0,0,0),new BD2Territory.RoutePoint(2,0,0)},now);
   var snapshot=new BD2Territory.TerritorySnapshot();var control=new BD2Territory.TerritoryControl();int stops=move.Stops;
   ContinueLocal(snapshot,now,control);
   Check(TerritoryBindings.Inputs==1&&move.Stops==stops&&localBlocked.EdgeAllowed(new(0,0,0),new(1,0,0)),"predicted obstruction issues native movement instead of blocking or restarting");
   ContinueLocal(snapshot,now+TimeSpan.FromMilliseconds(2900).Ticks,control);
   Check(TerritoryBindings.Inputs==2&&move.Stops==stops,"prediction still cannot abort at 2.9s");
   ContinueLocal(snapshot,now+TimeSpan.FromSeconds(3).Ticks,control);
   Check(move.Stops>stops&&!localBlocked.EdgeAllowed(new(0,0,0),new(1,0,0)),"observed 3s stall alone records an exclusion");
   // Simulated real walking with the same false prediction must never hit that branch.
   ResetLocalRecovery();localTarget=walkTarget.GetInstanceID();localBudget.Plan(now);SetLocalRoute(new[]{new BD2Territory.RoutePoint(0,0,0),new BD2Territory.RoutePoint(2,0,0)},now);stops=move.Stops;
   for(int i=0;i<8;i++){player.transform.position=new(i*.08f,0,0);body.transform.position=player.transform.position;ContinueLocal(snapshot,now+i*TimeSpan.TicksPerSecond,control);}
   Check(move.Stops==stops&&localBlocked.EdgeAllowed(new(0,0,0),new(1,0,0)),"game motion overrides repeated speculative collider hits");
   Physics.Reset();player.transform.position=new();body.transform.position=new();
   // Native-shaped stairs: samples advance from the preceding tread, with 1.2m total rise.
   var stairs=new Collider{bounds=new Bounds{min=new(-1,-1,-2),max=new(4,1.2f,2)}};
   float Stair(float x)=>Math.Clamp(MathF.Floor((x+.0001f)/.2f)*.15f,0,1.2f);
   Physics.Ray=p=>{float h=Stair(p.x);return new[]{new RaycastHit{collider=stairs,point=new(p.x,h,p.z),normal=Vector3.up,distance=p.y-h}};};
   Physics.Sweep=(a,b,r,d,n)=>{float top=Stair(Math.Max(a.x,a.x+d.x*n)+r);return a.y-r<top-1e-4?new[]{new RaycastHit{collider=stairs,point=new(a.x+n*.5f,top-.02f,a.z),normal=new(-1,0,0),distance=n*.5f}}:Array.Empty<RaycastHit>();};
   Physics.Overlap=(a,b,r)=>a.y-r<Stair(a.x+r)-1e-4?new[]{stairs}:Array.Empty<Collider>();
   Check(!StandClear(new(.18f,0,0))&&TraversalClear(new(.18f,0,0)),"body contacting the next tread may step while interaction stands remain strict");
   Check(Obstacle(new(.1f,0,0),new(.3f,.15f,0))==null,"tall staircase mesh bounds do not turn a reachable tread into a wall");
   Check(StaticLocalEdge(new(0,0,0),new(1.8,1.2,0),false,false),"ascending a multi-step native staircase follows each tread");
   Check(StaticLocalEdge(new(1.8,1.2,0),new(0,0,0),false,false),"descending the same staircase follows its surface");
   Check(!StaticLocalEdge(new(0,0,0),new(.1,1.2,0),false,true),"same horizontal coordinate on a higher floor is not a teleport connection");
   allowGoals=true;nativeReach=.8f;var upper=new LifeGatheringObject();upper.transform.position=new(3,1.2f,0);upper.Owner=new CircleSectorCollider{distance=.8f};localTrial=false;
   var stands=System.Linq.Enumerable.ToArray(StandCandidates(upper,upper.transform.position,.8f,false));
   Check(stands.Length>0&&System.Linq.Enumerable.All(stands,p=>Math.Abs(p.y-1.2f)<.01),"target stands use target floor instead of player's lower floor");
   localOrigin=new();localBlocked.Clear();var climb=new BD2Territory.AdaptiveLocalRoute(new(0,0,0),new[]{new BD2Territory.RoutePoint(2,1.2,0)},LocalSample,PlanEdge);
   for(int i=0;i<1000&&climb.State==BD2Territory.RouteSearchState.Searching;i++)climb.Step(256,1000);
   Check(climb.State==BD2Territory.RouteSearchState.Found,"production terrain adapter and A* reach upstairs");
   // Reproduce the user's 13:20 snapshot: 45-degree 1m ore box, body radius .24, skin .08.
   Physics.Reset();body.height=.77f;body.center=new(0,.4f,0);body.skinWidth=.08f;body.slopeLimit=55;
   var mine=new LifeGatheringObject();mine.transform.position=new(-3.903f,0,12.254f);mine.Owner=new CircleSectorCollider{distance=.8f};
   var rock=new Collider{Owner=mine,bounds=new Bounds{min=new(-4.61f,-.05f,11.547f),max=new(-3.196f,.45f,12.961f)}};
   float RockDistance(Vector3 p){float x=p.x-mine.transform.position.x,z=p.z-mine.transform.position.z;float u=(x+z)*.70710678f,v=(-x+z)*.70710678f;u=Math.Max(0,Math.Abs(u)-.5f);v=Math.Max(0,Math.Abs(v)-.5f);return MathF.Sqrt(u*u+v*v);}
   Physics.Ray=p=>new[]{new RaycastHit{collider=floor,point=new(p.x,.05f,p.z),normal=Vector3.up,distance=p.y-.05f}};
   Physics.Overlap=(a,b,r)=>RockDistance(a)<r-1e-5&&a.y-r<.45f?new[]{rock}:Array.Empty<Collider>();
   Check(Math.Abs(RootLift-.065)<1e-5,"observed .115m root is reproduced on the .05m floor");
   Check(!StandClear(new(-4.182f,.115f,12.970f))&&!TraversalClear(new(-4.182f,.115f,12.970f)),"recorded unreachable ore goal rejected with real .24m body");
   player.transform.position=new(-4.208f,.116f,12.998f);body.transform.position=player.transform.position;
   localTrial=false;triedDestinations.Clear();stands=System.Linq.Enumerable.ToArray(StandCandidates(mine,mine.transform.position,.8f,false));
   Check(stands.Length>0&&System.Linq.Enumerable.All(stands,p=>RockDistance(p)>=.24f-1e-5&&FlatDistance(p,mine.transform.position)<.8f),"alternative ore face stands both clear the real body and satisfy native range");
   localTrial=true;Check(System.Linq.Enumerable.All(StandCandidates(mine,mine.transform.position,.8f,false),p=>RockDistance(p)>=.24f-1e-5),"fallback must not invent an interaction stand inside ore");
   // A near-arrival failure retires that destination instead of choosing it repeatedly.
   Physics.Reset();Physics.Ray=p=>new[]{new RaycastHit{collider=floor,point=new(p.x,.05f,p.z),normal=Vector3.up,distance=p.y-.05f}};
   ResetLocalRecovery();allowGoals=false;walkTarget=mine;localTarget=mine.GetInstanceID();localBudget.Plan(now);triedDestinations.Clear();
   SetLocalRoute(new[]{Point(player.transform.position),new BD2Territory.RoutePoint(-4.182,.115,12.970)},now);
   ContinueLocal(snapshot,now,control);ContinueLocal(snapshot,now+TimeSpan.FromSeconds(3).Ticks,control);
   Check(triedDestinations.Count==1,"blocked ore stand is retired without waiting for an attack miss");
   Physics.Reset();return checks+TestWater();
  }
 }
}
