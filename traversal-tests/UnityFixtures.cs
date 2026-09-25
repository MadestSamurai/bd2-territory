using System;
namespace UnityEngine
{
 public struct Vector3
 {
  public float x,y,z;public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}
  public static Vector3 up=>new(0,1,0);public static Vector3 down=>new(0,-1,0);
  public float sqrMagnitude=>x*x+y*y+z*z;public float magnitude=>(float)Math.Sqrt(sqrMagnitude);
  public static float Distance(Vector3 a,Vector3 b)=>(a-b).magnitude;
  public static Vector3 Lerp(Vector3 a,Vector3 b,float t)=>a+(b-a)*t;
  public static Vector3 ClampMagnitude(Vector3 v,float n)=>v.magnitude>n?v.normalized*n:v;
  public string ToString(string format)=>x.ToString(format)+","+y.ToString(format)+","+z.ToString(format);
  public Vector3 normalized=>magnitude>0?this*(1/magnitude):new();
  public static Vector3 operator +(Vector3 a,Vector3 b)=>new(a.x+b.x,a.y+b.y,a.z+b.z);
  public static Vector3 operator -(Vector3 a,Vector3 b)=>new(a.x-b.x,a.y-b.y,a.z-b.z);
  public static Vector3 operator *(Vector3 a,float n)=>new(a.x*n,a.y*n,a.z*n);
 }
 public class Transform
 {
  public Vector3 eulerAngles;public Vector3 position,lossyScale=new(1,1,1);public Transform parent;
  public Vector3 TransformVector(Vector3 p){float a=eulerAngles.y*MathF.PI/180,c=MathF.Cos(a),s=MathF.Sin(a);return new(p.x*lossyScale.x*c+p.z*lossyScale.z*s,p.y*lossyScale.y,-p.x*lossyScale.x*s+p.z*lossyScale.z*c);}
  public Vector3 InverseTransformPoint(Vector3 p){p-=position;float a=eulerAngles.y*MathF.PI/180,c=MathF.Cos(a),s=MathF.Sin(a);return new((p.x*c-p.z*s)/lossyScale.x,p.y/lossyScale.y,(p.x*s+p.z*c)/lossyScale.z);}
  public Vector3 TransformPoint(Vector3 p)=>position+TransformVector(p);
  public bool IsChildOf(Transform t)=>this==t||parent!=null&&parent.IsChildOf(t);
 }
 public class GameObject{public int layer;public bool activeInHierarchy=true;}
 public class Component
 {
  private static int serial;private int id=++serial;public Transform transform=new();public GameObject gameObject=new();public object Owner;
  public int GetInstanceID()=>id;
  public T GetComponent<T>()where T:class=>Owner as T;
  public T GetComponentInParent<T>()where T:class=>Owner as T;
 }
 public struct Bounds {public Vector3 min,max;}
 public class Collider:Component {public bool enabled=true,isTrigger;public Bounds bounds;}
 public class BoxCollider:Collider {public Vector3 center,size;}
 public class CharacterController:Collider {public float stepOffset=.3f,slopeLimit=45,skinWidth=.02f,radius=.24f,height=1;public Vector3 center=new(0,.5f,0);}
 public struct RaycastHit {public Collider collider;public Vector3 point,normal;public float distance;}
 public enum QueryTriggerInteraction {Ignore}
 public static class Physics
 {
  public static Func<Vector3,RaycastHit[]> Ray=p=>Array.Empty<RaycastHit>();
  public static Func<Vector3,Vector3,float,Collider[]> Overlap=(a,b,r)=>Array.Empty<Collider>();
  public static RaycastHit[] Hits=Array.Empty<RaycastHit>();public static float LastRadius;public static Func<Vector3,Vector3,float,Vector3,float,RaycastHit[]> Sweep;
  public static HashSet<int> IgnoredLayers=new();public static HashSet<Collider> IgnoredPairs=new();
  public static bool GetIgnoreLayerCollision(int a,int b)=>IgnoredLayers.Contains(b);
  public static bool GetIgnoreCollision(Collider a,Collider b)=>IgnoredPairs.Contains(b);
  public static RaycastHit[] RaycastAll(Vector3 o,Vector3 d,float n,int mask,QueryTriggerInteraction q)=>System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Where(Ray(o),h=>h.distance>=0&&h.distance<=n));
  public static Collider[] OverlapCapsule(Vector3 a,Vector3 b,float r,int mask,QueryTriggerInteraction q){LastRadius=r;return Overlap(a,b,r);}
  public static RaycastHit[] CapsuleCastAll(Vector3 a,Vector3 b,float r,Vector3 d,float n,int mask,QueryTriggerInteraction q){LastRadius=r;return Sweep==null?Hits:Sweep(a,b,r,d,n);}
  public static void Reset(){Sweep=null;IgnoredLayers.Clear();IgnoredPairs.Clear();Hits=Array.Empty<RaycastHit>();Ray=p=>Array.Empty<RaycastHit>();Overlap=(a,b,r)=>Array.Empty<Collider>();}
 }
}
public class MoveController {protected UnityEngine.CharacterController _characterController;public void SetBody(UnityEngine.CharacterController body)=>_characterController=body;}
public class PlayerMoveController:MoveController {public float configuredStep=.3f;public int Stops;public void ClearMove(){}public void StopMove(){Stops++;}}
public class PlayerController:UnityEngine.Component {public bool IsGetOnVehicle()=>false;public void GetOffVehicle(){}}
public class LifeGatheringObject:UnityEngine.Component {}

public class LifeFarmFieldObject:UnityEngine.Component {public bool IsPlayerInInteractionRange(UnityEngine.Vector3 p)=>true;public UnityEngine.Vector3 GetFieldWorldCenter()=>transform.position;}
public class CircleSectorCollider:UnityEngine.Component {public float distance=1;public bool IsDetectionInCollisionArea(UnityEngine.Vector3 p)=>true;}
public class QuestNavigationManager {}
namespace UnityEngine {public static class Mathf {public static float Clamp(float v,float a,float b)=>Math.Clamp(v,a,b);}}
namespace UnityEngine.AI {public class NavMeshAgent {public bool isActiveAndEnabled,isOnNavMesh;public void ResetPath(){}}}
namespace BD2Territory {public class TerritorySnapshot {public string Scene="test",Reason;}public class TerritoryControl {}}
namespace BD2Territory.Runtime
{
 internal static class TerritoryClient {internal const string NormalStepFieldName="configuredStep";}
 internal static class TerritoryBindings
 {
  public static int Inputs;public static string State="Moving";
  public static object Read(string key,object owner)=>key=="Farm.Db"?(owner as FieldEvent.Life.Chunk.LifePlaceableObject)?.Db:key=="Layout.Temporary"?(owner as FieldEvent.Life.Chunk.LifePlaceableObject)?.Temporary??false:key=="Field.MoveState"?State:key=="Detector.Distance"?(owner as CircleSectorCollider)?.distance??1f:null;
  public static object EnumObject(string a,string b)=>b;
  public static object Singleton(Type t)=>null;
  public static object InvokeOn(string key,object owner,params object[] args){if(key=="Player.StartMove")Inputs++;return null;}
 }
 internal static class LocalStorage {public static void Log(string text){}}
 internal sealed partial class RuntimeEngine
 {
  private readonly BD2Territory.LocalRouteMemory localPaths=new();
  private readonly BD2Territory.RouteMemo<BD2Territory.RouteEdgeKey,bool> localEdges=new(100,TimeSpan.FromSeconds(30).Ticks);
  private readonly System.Collections.Generic.List<UnityEngine.Vector3> triedDestinations=new();
  private bool ownsMove,bypassing,vehiclePending,gatheringReposition;private UnityEngine.Component walkTarget;private string walkScene;
  private UnityEngine.Vector3 finalDestination;private UnityEngine.Collider blockingCollider;private long lastInput;
  private float nativeReach=.71f;private bool allowGoals;private int skipped;private Progress navigation=new(),interaction=new();
  private sealed class Progress {public void CancelAttempt(){}public void Reset(){}public void Select(int id){}public void Arrived(long n){}}
  private void CancelVehicle(){}private UnityEngine.AI.NavMeshAgent NavAgent()=>null;
  private void InvalidateLocalArea(UnityEngine.Vector3 p){}private void CaptureFailure(string reason){}
  private void SkipWalkTarget(UnityEngine.Component t,BD2Territory.TerritorySnapshot s,long now,string reason){skipped++;}
  private bool CanGatherAt(UnityEngine.Component t,UnityEngine.Vector3 p)=>allowGoals&&FlatDistance(p,t.transform.position)<nativeReach;
  private bool GatherStandReached(UnityEngine.Component t,UnityEngine.Vector3 p,bool reached,bool detected)=>false;
  private void StopMove(){move.StopMove();ClearLocalMotion();}
  private static int Kind(LifeGatheringObject o)=>2;
  private static LifeGatheringObject[] Detected(int kind)=>Array.Empty<LifeGatheringObject>();
  private bool TryEscape(BD2Territory.TerritorySnapshot s,long now,BD2Territory.TerritoryControl c,UnityEngine.Collider o)=>false;
  private void UpdateVehicle(long now,BD2Territory.TerritoryControl c,double remaining){}
  private BD2Territory.RoutePoint? CachedGround(BD2Territory.RoutePoint p)=>GroundPoint(Vector(p),out var g,false)?Point(g):null;
 }
}
