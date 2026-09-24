using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Text.Json;
using UnityEngine;
namespace BD2Territory.Runtime
{
 // Runs the production routing entry points; a native call fails the default-mode test.
 internal sealed partial class RuntimeEngine
 {
  private TerritoryControl control=new TerritoryControl();
  private readonly TravelProgress travel=new TravelProgress();
  private bool localMoving,localSucceeds=true;private Component walkTarget=new Component{Id=42};
  private readonly List<string> routeCalls=new List<string>();
  private void StopMotion(){routeCalls.Add("stop_motion");}
  private void StopEscape(){travel.EndDash();routeCalls.Add("stop_dash");}
  private bool BeginLocalRoute(Component target,TerritorySnapshot s,long now,string reason)
  {if(target!=walkTarget)throw new Exception("Routing lost selected target");routeCalls.Add("astar:"+reason);localMoving=localSucceeds;return localSucceeds;}
  private void SkipWalkTarget(Component target,TerritorySnapshot s,long now,string reason){routeCalls.Add("retry:"+reason);}
  private bool ApproachNavMesh(Component target,Vector3 center,TerritorySnapshot s,long now,float radius){routeCalls.Add("native_start");return false;}
  private void ContinueNavMesh(TerritorySnapshot s,long now,TerritoryControl c){routeCalls.Add("native_tick");}
  private void ResumeNavMesh(TerritorySnapshot s,long now,TerritoryControl c){routeCalls.Add("native_resume");}
  private void ContinueLocal(TerritorySnapshot s,long now,TerritoryControl c){routeCalls.Add("astar_tick");}
  private void ContinueEscape(TerritorySnapshot s,long now,TerritoryControl c){routeCalls.Add("dash_tick");}
  internal int ExerciseRoutes()
  {
   int checks=0;void Check(bool ok,string why){checks++;if(!ok)throw new Exception("Routing: "+why+" / "+string.Join(",",routeCalls));}
   void Calls(params string[] expected){Check(string.Join(",",routeCalls)==string.Join(",",expected),"expected "+string.Join(",",expected));routeCalls.Clear();}
   var s=new TerritorySnapshot();long now=DateTime.UtcNow.Ticks;
   Check(!control.UseNavMesh&&!new TerritorySettings().UseNavMesh,"new installs default to A*");
   var old=JsonSerializer.Deserialize<TerritorySettings>("{\"UseVehicle\":true}");Check(!old.UseNavMesh,"old settings do not opt in");
   using(var f=new MemoryStream(Encoding.UTF8.GetBytes("{\"Enabled\":true}")))Check(!((TerritoryControl)new DataContractJsonSerializer(typeof(TerritoryControl)).ReadObject(f)).UseNavMesh,"hook's legacy control reader does not opt in");
   StartRoute(walkTarget,new Vector3(),s,now,.7f);Calls("stop_motion","astar:astar_default");
   ContinueRoute(s,now,control);Calls("astar_tick");
   ResumeRouteAfterDash(s,now,control);Calls("astar:dash_replan_physical");
   // Failed physical paths stay physical and are queued for later, never upgraded to native.
   localSucceeds=false;StartRoute(walkTarget,new Vector3(),s,now,.7f);Calls("stop_motion","astar:astar_default","retry:astar_unavailable");
   ContinueRoute(s,now,control);Calls("stop_dash","astar:navmesh_disabled","retry:astar_unavailable");
   ResumeRouteAfterDash(s,now,control);Calls("astar:dash_replan_physical","retry:dash_replan_unavailable");localSucceeds=true;
   control.UseNavMesh=true;StartRoute(walkTarget,new Vector3(),s,now,.7f);Calls("native_start");
   localMoving=false;ContinueRoute(s,now,control);Calls("native_tick");
   ResumeRouteAfterDash(s,now,control);Calls("native_resume");
   // Opted-in native obstruction fallback must not jump back to native while following A*.
   localMoving=true;ContinueRoute(s,now,control);Calls("astar_tick");
   ResumeRouteAfterDash(s,now,control);Calls("astar:dash_replan_physical");
   // Disabling in motion keeps the same target and clears the native recovery dash first.
   localMoving=false;travel.Select(42);travel.BeginDash(now,true,true);control.UseNavMesh=false;
   ContinueRoute(s,now,control);Calls("stop_dash","astar:navmesh_disabled");Check(!travel.Dashing&&localMoving,"native dash retired before local movement");
   travel.Select(43);travel.BeginDash(now,true,true);ContinueRoute(s,now,control);Calls("dash_tick");travel.EndDash();
   control.UseNavMesh=true;ContinueRoute(s,now,control);Calls("astar_tick");
   control=null;StartRoute(walkTarget,new Vector3(),s,now,.7f);Calls("stop_motion","astar:astar_default");
   return checks;
  }
 }
 internal static class RoutingAdapterTests
 {
  internal static void Run(){Console.WriteLine("Production routing adapter checks: "+new RuntimeEngine().ExerciseRoutes());}
 }
}
