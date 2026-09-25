using System;
using System.Collections;
using System.Collections.Generic;
using BD2Territory;
// Shared Unity/binding fixtures for routing, gathering and popup adapters.
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
 }
}
