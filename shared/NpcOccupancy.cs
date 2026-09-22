using System;
namespace BD2Territory
{
 // NPC occupancy is independent of physics query masks and navigation-agent avoidance.
 public struct NpcFootprint
 {
  public int Id;public RoutePoint Center;public double Radius,Bottom,Top;
  public override string ToString()=>"id="+Id+" x="+Center.X.ToString("F3")+" z="+Center.Z.ToString("F3")+" radius="+Radius.ToString("F3")+" bottom="+Bottom.ToString("F3")+" top="+Top.ToString("F3");
 }
 public static class NpcOccupancy
 {
  public const double Clearance=.08;
  private static bool SameHeight(NpcFootprint npc,double bottom,double top)=>npc.Top>bottom+.015&&npc.Bottom<top-.015;
  public static bool StandClear(RoutePoint p,double radius,double bottom,double top,NpcFootprint[] npcs)
  {
   foreach(var npc in npcs)if(SameHeight(npc,bottom,top)&&RoutePoint.Distance(p,npc.Center)<radius+npc.Radius+Clearance)return false;
   return true;
  }
  public static bool PathClear(RoutePoint a,RoutePoint b,double radius,double bottom,double top,NpcFootprint[] npcs)
  {
   double dx=b.X-a.X,dz=b.Z-a.Z,len=dx*dx+dz*dz;
   foreach(var npc in npcs)
   {
    if(!SameHeight(npc,bottom,top))continue;
    double limit=radius+npc.Radius+Clearance,start=RoutePoint.Distance(a,npc.Center),end=RoutePoint.Distance(b,npc.Center);
    double t=len<1e-12?0:Math.Max(0,Math.Min(1,((npc.Center.X-a.X)*dx+(npc.Center.Z-a.Z)*dz)/len));
    double nearest=RoutePoint.Distance(new RoutePoint(a.X+t*dx,a.Y,a.Z+t*dz),npc.Center);
    if(start<limit)
    {
     // An NPC can walk into us. Permit monotonic escape, never crossing through it.
     if(end<=start+1e-5||nearest<start-1e-5)return false;
    }
    else if(nearest<limit)return false;
   }
   return true;
  }
 }
}
