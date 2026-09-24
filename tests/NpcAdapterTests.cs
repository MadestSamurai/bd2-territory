using System;
using System.Collections;
using System.Collections.Generic;
using BD2Territory;
// Minimal Unity/binding fixture. Compiles the production RuntimeNpcOccupancy adapter unchanged.
namespace UnityEngine
{
 public struct Vector3
 {
  public float x,y,z;public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}
  public static Vector3 up=>new(0,1,0);
  public static Vector3 operator +(Vector3 a,Vector3 b)=>new(a.x+b.x,a.y+b.y,a.z+b.z);
  public static Vector3 operator -(Vector3 a,Vector3 b)=>new(a.x-b.x,a.y-b.y,a.z-b.z);
  public static Vector3 operator *(Vector3 a,float n)=>new(a.x*n,a.y*n,a.z*n);
 }
 public class Transform
 {
  public Vector3 position;public Vector3 lossyScale=new(1,1,1);
  public Vector3 TransformPoint(Vector3 p)=>position+new Vector3(p.x*lossyScale.x,p.y*lossyScale.y,p.z*lossyScale.z);
 }
 public class Component
 {
  public int Id;public bool Active=true;public Transform transform=new();public CharacterController Body;public Collider HitBox;
  public T GetComponent<T>() where T:class=>(HitBox as T)??(Body as T);
  public int GetInstanceID()=>Id;
  public T GetComponentInChildren<T>(bool includeInactive) where T:class=>Body as T;
 }
 public class Collider:Component {public bool enabled=true;public Func<Vector3,Vector3> Closest=p=>p;public Vector3 ClosestPoint(Vector3 p)=>Closest(p);}
 public class CharacterController:Collider {public float radius=.24f,height=.77f;public Vector3 center=new(0,.4f,0);}
}
namespace BD2Territory.Runtime
{
 using UnityEngine;
 internal static class TerritoryBindings
 {
  internal static object Manager=new();internal static readonly Dictionary<string,IDictionary> Lists=new();
  internal static Type Type(string role)=>typeof(object);
  public sealed class Context {public object GetTickBase<T>()=>Manager;}
  internal static object Read(string role,object owner)=>new Context();
  internal static object InvokeOn(string role,object owner){if(role=="Ui.Back"){((UIBase)owner).OnClickBackButton();return null;}return Lists[role];}
  internal static bool Active(Component c)=>c!=null&&c.Active;
 }
 internal sealed partial class RuntimeEngine
 {
  private long lastInput;private int stopCalls;private void StopMove(){stopCalls++;}
  private static RoutePoint Point(Vector3 p)=>new(p.x,p.y,p.z);
  private static void BodyCapsule(Vector3 p,out Vector3 low,out Vector3 high,out float radius){radius=.24f;low=p+Vector3.up*.255f;high=p+Vector3.up*.545f;}
  internal int ExerciseNpcAdapter()
  {
   int checks=0;void Check(bool ok,string label){checks++;if(!ok)throw new Exception("NPC adapter: "+label);}
   var worker=new Component{Id=101};worker.transform.position=new(1.349f,.197f,.853f);worker.Body=new CharacterController{transform=worker.transform};
   var citizen=new Component{Id=102};citizen.transform.position=new(.896f,.197f,1.659f);citizen.Body=new CharacterController{transform=citizen.transform};
   var workers=new Hashtable{{1,worker}};var citizens=new Hashtable{{2,citizen}};
   TerritoryBindings.Lists["Npc.Workers"]=workers;TerritoryBindings.Lists["Npc.Citizens"]=citizens;
   Check(RefreshNpcOccupancy()&&npcFootprints.Length==2,"reads both authoritative dictionaries");
   Check(!NpcStandClear(new(1.712f,.23f,.9f)),"production adapter rejects actual recorded landing");
   Check(!NpcPathClear(new(1.774f,.115f,1.070f),new(1.712f,.23f,.9f)),"production adapter blocks entry toward worker");
   Check(NpcPathClear(new(1.774f,.115f,1.070f),new(2.4f,.115f,1.070f)),"production adapter allows outward escape");
   worker.Body.enabled=false;RefreshNpcOccupancy();Check(!NpcStandClear(new(1.712f,.23f,.9f)),"disabled physical collider does not erase live worker occupancy");
   workers[5]=worker;RefreshNpcOccupancy();Check(npcFootprints.Length==2,"deduplicates NPC identity");
   worker.transform.position=new(9,.197f,9);RefreshNpcOccupancy();Check(NpcStandClear(new(1.712f,.23f,.9f)),"moving worker read afresh next update");
   worker.Body=null;RefreshNpcOccupancy();Check(!NpcStandClear(new(9,.2f,9)),"missing body uses conservative actor footprint");
   worker.Active=false;RefreshNpcOccupancy();Check(npcFootprints.Length==1,"hidden inactive NPC is excluded");
   citizens.Clear();workers.Clear();RefreshNpcOccupancy();Check(npcFootprints.Length==0&&npcBodies.Count==0,"despawn clears live occupancy and collider ownership");
   worker.Active=true;worker.Body=new CharacterController{transform=worker.transform};worker.transform.lossyScale=new(2,2,2);workers[1]=worker;RefreshNpcOccupancy();
   Check(Math.Abs(npcFootprints[0].Radius-.48)<.00001&&Math.Abs(npcFootprints[0].Top-npcFootprints[0].Bottom-1.54)<.00001,"scaled actor uses real radius and height");
   TerritoryBindings.Manager=null;Check(!RefreshNpcOccupancy(),"missing manager is pending, not evidence of no NPCs");TerritoryBindings.Manager=new();
   TerritoryBindings.Lists["Npc.Workers"]=null;bool rejected=false;try{RefreshNpcOccupancy();}catch(InvalidOperationException){rejected=true;}Check(rejected,"invalid registry cannot silently report a clear map");
   return checks;
  }
 }
 internal static class NpcAdapterTests {internal static int Run()=>new RuntimeEngine().ExerciseNpcAdapter();}
}
