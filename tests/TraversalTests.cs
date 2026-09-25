using BD2Territory;
internal static class TraversalTests
{
 public static int Run()
 {
  int checks=0;void Check(bool ok,string label){checks++;if(!ok)throw new Exception("Traversal: "+label);}
  Check(TraversalRules.CanStep(.3,0,.3,.02),"native step height accepted");
  Check(!TraversalRules.CanStep(.6,0,.3,.02),"tall wall is not a step");
  Check(!TraversalRules.CanStep(.1,0,0,.02),"no invented step ability");
  Check(TraversalRules.WalkableNormal(Math.Cos(40*Math.PI/180),45),"native slope accepted");
  Check(!TraversalRules.WalkableNormal(.5,45),"steep surface not a floor");
  Check(!TraversalRules.WalkableNormal(0,89),"vertical wall not ground");
  Check(TraversalRules.SurfaceChange(.25,.2,.3,45,.02),"single step accepted");
  Check(!TraversalRules.SurfaceChange(.8,.2,.3,45,.02),"large rise rejected");
  Check(!TraversalRules.SurfaceChange(-2,.2,.3,45,.02),"no walk across a drop");
  long t=TimeSpan.FromDays(1).Ticks;long S(double seconds)=>t+(long)(seconds*TimeSpan.TicksPerSecond);
  var motion=new LocalMotionProgress();motion.Begin(t,new(0,0,0));
  Check(!motion.Stalled(t,new(0,0,0),10),"first input gets a motion window");
  Check(!motion.Stalled(S(2.99),new(.003,0,0),9.997),"prediction does not cause immediate obstruction");
  Check(motion.Stalled(S(3),new(.003,0,0),9.997),"three seconds of real no-progress ends trial");
  motion.Begin(t,new(0,0,0));motion.Stalled(t,new(0,0,0),10);
  Check(!motion.Stalled(S(2.9),new(.08,0,0),9.92)&&!motion.Stalled(S(5.8),new(.16,0,0),9.84),"slow but sustained walking keeps route");
  motion.Begin(t,new(0,0,0));motion.Stalled(t,new(0,0,0),1);
  Check(!motion.Stalled(S(2.8),new(.03,.1,0),.97)&&!motion.Stalled(S(5),new(.03,.1,0),.97),"native upward step counts as progress");
  motion.Begin(t,new(0,0,0));motion.Stalled(t,new(0,0,0),1);
  Check(motion.Stalled(S(3),new(0,.1,0),1),"vertical-only stuck jitter cannot renew indefinitely");
  motion.Begin(S(10),new(0,0,0));Check(!motion.Stalled(S(10),new(0,0,0),1),"loading wait restarts input clock");
  var blocked=new LocalObstructionMemory();var origin=new RoutePoint(0,0,0);blocked.Record(origin);
  Check(blocked.SampleAllowed(origin,new(.1,0,0))&&blocked.EdgeAllowed(origin,new(.1,0,0)),"observed-block disk cannot imprison its own origin");
  Check(!blocked.EdgeAllowed(new(1,0,0),new(-1,0,0)),"cannot enter observed obstruction from outside");
  Check(!blocked.EdgeAllowed(new(.1,0,0),new(-.2,0,0)),"escape cannot cut through center");
  Check(blocked.EdgeAllowed(new(.1,0,0),new(.4,0,0)),"outward escape remains available");
  var plan=new AdaptiveLocalRoute(origin,new[]{new RoutePoint(1,0,0)},p=>blocked.SampleAllowed(origin,p)?p:null,blocked.EdgeAllowed,true);
  while(plan.State==RouteSearchState.Searching)plan.Step(100,1000);
  Check(plan.State==RouteSearchState.Found,"actual A* escapes its old near-origin exclusion");
  blocked.Clear();Check(blocked.EdgeAllowed(new(1,0,0),new(-1,0,0)),"attempt reset clears temporary evidence");
  Console.WriteLine("Traversal and observed-motion checks: "+checks);return checks;
 }
}
