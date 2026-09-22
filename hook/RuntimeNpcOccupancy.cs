using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using B=BD2Territory.Runtime.TerritoryBindings;
namespace BD2Territory.Runtime
{
 internal sealed partial class RuntimeEngine
 {
  private NpcFootprint[] npcFootprints=new NpcFootprint[0];
  private readonly HashSet<Collider> npcBodies=new HashSet<Collider>();
  private bool RefreshNpcOccupancy()
  {
   var field=B.Read("Field.Instance",null);var context=field==null?null:B.Read("Field.Context",field);
   var manager=context==null?null:context.GetType().GetMethods().Single(m=>m.Name=="GetTickBase"&&m.IsGenericMethod&&m.GetParameters().Length==0).MakeGenericMethod(B.Type("NpcManager")).Invoke(context,null);
   if(manager==null)return false;
   var footprints=new List<NpcFootprint>();npcBodies.Clear();var seen=new HashSet<int>();
   foreach(var role in new[]{"Npc.Citizens","Npc.Workers"})
   {
    var dictionary=B.InvokeOn(role,manager) as IDictionary;if(dictionary==null)throw new InvalidOperationException("领地 NPC 列表尚未可读");
    foreach(Component npc in dictionary.Values)
    {
     if(!B.Active(npc)||!seen.Add(npc.GetInstanceID()))continue;
     var body=npc.GetComponentInChildren<CharacterController>(true);Vector3 center;float radius,height;
     if(body!=null)
     {
      npcBodies.Add(body);var scale=body.transform.lossyScale;center=body.transform.TransformPoint(body.center);
      radius=body.radius*Math.Max(Math.Abs(scale.x),Math.Abs(scale.z));height=Math.Max(radius*2,body.height*Math.Abs(scale.y));
     }
     else{center=npc.transform.position+Vector3.up*.4f;radius=.24f;height=.77f;}
     footprints.Add(new NpcFootprint{Id=npc.GetInstanceID(),Center=Point(center),Radius=radius,Bottom=center.y-height*.5,Top=center.y+height*.5});
    }
   }
   npcFootprints=footprints.ToArray();return true;
  }
  private bool NpcStandClear(Vector3 root)
  {
   Vector3 low,high;float radius;BodyCapsule(root,out low,out high,out radius);
   var center=(low+high)*.5f;return NpcOccupancy.StandClear(Point(center),radius,low.y-radius,high.y+radius,npcFootprints);
  }
  private bool NpcPathClear(Vector3 from,Vector3 to)
  {
   Vector3 low,high;float radius;BodyCapsule(from,out low,out high,out radius);
   var center=(low+high)*.5f;return NpcOccupancy.PathClear(Point(center),Point(center+to-from),radius,low.y-radius+Math.Min(0,to.y-from.y),high.y+radius+Math.Max(0,to.y-from.y),npcFootprints);
  }
 }
}
