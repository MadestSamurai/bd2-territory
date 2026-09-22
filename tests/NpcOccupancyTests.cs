using System;
using System.Linq;
using BD2Territory;
internal static class NpcOccupancyTests
{
 public static int Run()
 {
  int checks=0;void Check(bool ok,string label){checks++;if(!ok)throw new Exception("NPC occupancy: "+label);}
  NpcFootprint Actor(int id,double x,double z)=>new(){Id=id,Center=new(x,.597,z),Radius=.24,Bottom=.212,Top=.982};
  var npcs=new[]{Actor(1,1.349,.853),Actor(2,.896,1.659)};
  bool Stand(RoutePoint p)=>NpcOccupancy.StandClear(p,.24,.2,.97,npcs);
  bool Edge(RoutePoint a,RoutePoint b)=>NpcOccupancy.PathClear(a,b,.24,.2,.97,npcs);
  var badGoal=new RoutePoint(1.712,.230,.900);
  Check(RoutePoint.Distance(badGoal,npcs[0].Center)<.48,"saved Runtime10 goal overlaps actual worker");
  Check(!Stand(badGoal),"saved bad goal rejected independently of physics layer filtering");
  Check(!Stand(new(1.34,.23,.96)),"next proposed goal also rejected");
  var from=new RoutePoint(1.774,.115,1.070);
  Check(!Stand(from),"saved player already overlaps worker margin");
  Check(!Edge(from,badGoal),"do not steer deeper into worker");
  Check(Edge(from,new(2.4,.115,1.070)),"allow outward recovery after NPC approaches player");
  var goals=LocalStandPoints.Create(new(1.379,0,0),from,1).Where(Stand).ToArray();
  Check(goals.Length>0&&goals.Length<128,"choose free side of the same resource, not skip resource");
  var search=new LocalRouteSearch(from,goals,p=>Stand(p)?p:null,Edge);
  while(search.State==RouteSearchState.Searching)search.Step(96,1000);
  Check(search.State==RouteSearchState.Found,"real recorded worker arrangement admits a detour");
  Check(Stand(search.Path.Last()),"recovered endpoint clear of both NPCs");
  for(int i=1;i<search.Path.Length;i++)Check(Edge(search.Path[i-1],search.Path[i]),"whole route avoids NPC or monotonically exits initial overlap");
  npcs=new[]{Actor(3,0,0)};
  Check(!Edge(new(-1,0,0),new(1,0,0)),"free endpoints do not permit crossing through NPC");
  Check(!Edge(new(-1,0,.53),new(1,0,.53)),"clearance catches near-edge squeeze");
  Check(Edge(new(-1,0,.57),new(1,0,.57)),"clear corridor not over-blocked");
  Check(!Stand(new(.55,0,0))&&Stand(new(.57,0,0)),"80mm NPC-only clearance, not expanding mineral collision");
  Check(!Edge(new(.1,0,0),new(-1,0,0)),"initial overlap cannot escape through actor center");
  Check(!Edge(new(.1,0,0),new(.1,0,0)),"zero move is not an escape");
  Check(Edge(new(.1,0,0),new(.2,0,0)),"incremental outward step allowed");
  var distant=Actor(3,0,0);distant.Bottom=3;distant.Top=4;npcs=new[]{distant};
  Check(Stand(new(0,0,0))&&Edge(new(-1,0,0),new(1,0,0)),"NPC on different floor does not block ground");
  npcs=Array.Empty<NpcFootprint>();Check(Stand(new(0,0,0)),"empty current NPC list clears previous occupancy");
  npcs=new[]{Actor(4,0,0)};Check(!Stand(new(0,0,0)),"new NPC at endpoint immediately invalidates it");
  npcs=new[]{Actor(4,2,0)};Check(Stand(new(0,0,0)),"movement away makes old endpoint eligible again");
  checks+=BD2Territory.Runtime.NpcAdapterTests.Run();Console.WriteLine("NPC occupancy checks: "+checks);return checks;
 }
}
