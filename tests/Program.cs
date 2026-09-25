using BD2Territory;
using System.Text.Json;
CookingCases.Run();
BD2Territory.Runtime.RoutingAdapterTests.Run();
BD2Territory.Runtime.GatheringPositionTests.Run();
int checks=0;
void Check(bool v,string why){checks++;if(!v)throw new Exception(why);}
void Reject(Action a,string why){bool rejected=false;try{a();}catch(InvalidOperationException){rejected=true;}Check(rejected,why);}
CropStock[] Stocks()=>new[]{new CropStock{SeedId=2,Required=5,Yield=1},new CropStock{SeedId=1,Required=3,Yield=1},new CropStock{SeedId=4,Required=2,Yield=1}};
string[] Fields()=>Enumerable.Range(0,100).Select(i=>"1:30008:"+(i%10)+":"+(i/10)).ToArray();
PlantingCell[] Cells(string[] keys,int seed=2,int cost=1)=>keys.Select(k=>new PlantingCell{Key=k,Seed=seed,Cost=cost,CurrencyType=64,CurrencyId=0}).ToArray();
PlantingReply Reply(RecipeBatchProgress intent)=>new(){Token=intent.PendingToken,Keys=(string[])intent.PendingKeys.Clone(),Seed=intent.PendingSeed,Cost=intent.PendingCost,RequestMatches=true,Accepted=true};
var crops=Stocks();var p=new RecipeBatchProgress{Account="account-a"};var totals=new Dictionary<int,int>{{2,0},{1,0},{4,0}};
for(int batch=0;batch<100;batch++)
{
 int seed=RecipeBatchPlanner.Choose(p,crops);var c=crops.Single(x=>x.SeedId==seed);
 var staged=PlantingTransaction.Begin(p,Fields(),seed==4?3:1,"run",0,"batch:"+batch);
 Check(staged.Planted==0,"准备预览不可记为已播种");
 Check(PlantingTransaction.MatchesRequest(staged,Cells(Fields(),seed,seed==4?3:1)),"整批原生请求必须匹配");
 p=PlantingTransaction.Settle(staged,Reply(staged));
 c.Growing+=100;totals[seed]+=100;
 Check(p.Planted==100&&p.Batches==batch+1,"一次回执直接完成 100 格及一个批次");
 var state=JsonSerializer.Deserialize<RecipeBatchProgress>(JsonSerializer.Serialize(p));int next=RecipeBatchPlanner.Choose(state,crops);
 foreach(var crop in crops){crop.Inventory+=crop.Growing;crop.Growing=0;}
 var state2=JsonSerializer.Deserialize<RecipeBatchProgress>(JsonSerializer.Serialize(p));Check(RecipeBatchPlanner.Choose(state2,crops)==next,"收获前后配比应相同");
 if((batch+1)%10==0){int dishes=(int)crops.Min(x=>x.Inventory/x.Required);foreach(var crop in crops)crop.Inventory-=dishes*crop.Required;}
}
Check(totals[2]==5000&&totals[1]==3000&&totals[4]==2000,"100 次整批事务产出应为 5:3:2");
Check(p.Spent==14000,"100 批总费用应为 14000，不重复乘 100");
var stock=Stocks();stock[0].Inventory=500;stock[1].Growing=300;Check(RecipeBatchPlanner.Choose(new(),stock)==4,"库存及地里作物共同决定缺口");
Reject(()=>RecipeBatchPlanner.Confirm(new(){SeedId=2},2,1,"one"),"不能退回逐格播种");
Reject(()=>RecipeBatchPlanner.Confirm(new(){SeedId=2},2,99,"partial"),"部分成功不能算为整批");
Reject(()=>RecipeBatchPlanner.Confirm(new(){SeedId=2},2,int.MaxValue,"overflow"),"异常数量不能绕过批次上限");
Reject(()=>RecipeBatchPlanner.Choose(new(){SeedId=2,Planted=37},Stocks()),"拒绝半批记录");
var now=DateTime.UtcNow.Ticks;var cmd=new TerritoryControl{Enabled=true,OwnerId="x",ProcessId=123,UntilUtcTicks=now+TimeSpan.FromSeconds(10).Ticks};Check(cmd.Valid(now,123),"正常租约");Check(!cmd.Valid(now,124),"其他进程不能复用租约");Check(!cmd.Valid(now+TimeSpan.FromSeconds(11).Ticks,123),"界面失联应停止");cmd.UntilUtcTicks=now+TimeSpan.FromDays(1).Ticks;Check(!cmd.Valid(now,123),"拒绝长期伪租约");
var ledger=new RecipeBatchProgress{Account="account-a",SeedId=2,BudgetOwner="run-a",Spent=0};
var fields=Fields();var intent=PlantingTransaction.Begin(ledger,fields,1,"run-a",100,"operation-1");
Check(ledger.PendingToken==""&&intent.PendingToken=="operation-1","记录写入成功前不得改变运行态");
fields[0]="changed";Check(!intent.PendingKeys.Contains("changed"),"调用者变更数组不能更改已记录意图");
var copied=intent.Copy();copied.PendingKeys[0]="changed";Check(!intent.PendingKeys.Contains("changed"),"事务副本不能共享地块数组");
Reject(()=>PlantingTransaction.Begin(intent,Fields(),1,"run-a",100,"operation-2"),"未确认时不得追加付款");
Reject(()=>RecipeBatchPlanner.Choose(intent,Stocks()),"未知回执不得换批");
foreach(int count in new[]{0,1,99,101})Reject(()=>PlantingTransaction.Begin(ledger,Enumerable.Range(0,count).Select(i=>"key"+i).ToArray(),1,"run-a",0,"wrong-size"),"必须恰好 100 格");
var duplicates=Fields();duplicates[1]=duplicates[0];Reject(()=>PlantingTransaction.Begin(ledger,duplicates,1,"run-a",0,"duplicate"),"不能重复计数同一格");
Check(!PlantingTransaction.SameKeys(Fields(),duplicates),"回执不得包含重复格子");
var cells=Cells(Fields());Check(PlantingTransaction.MatchesRequest(intent,cells.Reverse().ToArray()),"网络顺序改变不影响整批身份");
Check(!PlantingTransaction.MatchesRequest(intent,cells.Take(99).ToArray()),"99 格请求不能匹配");
foreach(var change in new Action<PlantingCell>[]{c=>c.Seed=4,c=>c.Cost=2,c=>c.Cost=100,c=>c.CurrencyType=2,c=>c.CurrencyId=1,c=>c.Key="wrong"})
 {var wrong=Cells(Fields());change(wrong[42]);Check(!PlantingTransaction.MatchesRequest(intent,wrong),"单格费用、种子、币种或位置改变应拒绝");}
var disk=JsonSerializer.Deserialize<RecipeBatchProgress>(JsonSerializer.Serialize(intent));var receipt=Reply(intent);
var settled=PlantingTransaction.Settle(disk,receipt);
Check(settled.Planted==100&&settled.Batches==1&&settled.Spent==100&&settled.PendingToken=="","暂停后的整批成功回执只记账一次");
Check(disk.PendingToken=="operation-1"&&disk.Planted==0,"持久化失败不能留下半更新内存");
Check(ReferenceEquals(settled,PlantingTransaction.Settle(settled,receipt)),"旧回执重放不增加批次");
Check(ReferenceEquals(intent,PlantingTransaction.Settle(intent,new PlantingReply{Token="foreign"})),"其他请求不推进批次");
foreach(var change in new Action<PlantingReply>[]{r=>r.Keys[12]="wrong",r=>r.Seed=4,r=>r.Cost=1,r=>r.Keys=r.Keys.Take(99).ToArray(),r=>r.RequestMatches=false,r=>r.Rejected=true})
 {var bad=Reply(intent);change(bad);Reject(()=>PlantingTransaction.Settle(intent,bad),"错误地块、作物、费用、部分回执或矛盾结果不能记账");}
var unknown=Reply(intent);unknown.Accepted=false;
Reject(()=>PlantingTransaction.Settle(intent,unknown),"未知结果必须保留整批意图");
Check(intent.PendingKeys.Length==100&&intent.Planted==0,"未知结果保留所有地块且不计数");
unknown.Rejected=true;var denied=PlantingTransaction.Settle(intent,unknown);
Check(denied.Planted==0&&denied.Spent==0&&denied.PendingToken=="","明确拒绝不推进、不扣预算");
Reject(()=>PlantingTransaction.Begin(ledger,Fields(),1,"run-a",99,"over-budget"),"整批费用超过预算不得播种一部分");
Reject(()=>PlantingTransaction.Begin(ledger,Fields(),int.MaxValue,"run-a",0,"overflow"),"费用溢出应拒绝");
var mushroom=new RecipeBatchProgress{Account="a",SeedId=4};
var mushroomIntent=PlantingTransaction.Begin(mushroom,Fields(),3,"run-a",300,"mushroom");
Check(mushroomIntent.PendingCost==300&&PlantingTransaction.MatchesRequest(mushroomIntent,Cells(Fields(),4,3)),"蘑菇 100 格共收费 300");
Check(PlantingTransaction.Settle(mushroomIntent,Reply(mushroomIntent)).Spent==300,"蘑菇整批只记 300");
var secondBatch=settled.Copy();RecipeBatchPlanner.Choose(secondBatch,Stocks());
Reject(()=>PlantingTransaction.Begin(secondBatch,Fields(),1,"run-a",199,"over-total"),"本次启动多批不能突破预算");
var newIntent=PlantingTransaction.Begin(secondBatch,Fields(),1,"run-b",100,"operation-2");
Check(newIntent.Spent==0&&newIntent.BudgetOwner=="run-b","新的开始单独计本次预算");
Check(ReferenceEquals(newIntent,PlantingTransaction.Settle(newIntent,receipt)),"更早回执不能推进新批次同名作物");
// The desktop and Mono runtime use different serializers; verify the actual wire format, including inherited settings.
var wire=System.Text.Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new TerritoryControl{OwnerId="wire",ProcessId=456,Enabled=true,UntilUtcTicks=now+TimeSpan.FromSeconds(10).Ticks,Logging=false,Mining=true,Farming=false,IntervalMs=750,PlantingBudget=321}));
using(var stream=new MemoryStream(wire))
{
 var decoded=(TerritoryControl)new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(TerritoryControl)).ReadObject(stream);
 Check(decoded.Valid(now,456)&&!decoded.Logging&&decoded.Mining&&!decoded.Farming&&decoded.IntervalMs==750&&decoded.PlantingBudget==321,"桌面控制 JSON 与 Mono 反序列化一致");
}
using(var stream=new MemoryStream())
{
 new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(RecipeBatchProgress)).WriteObject(stream,intent);
 var decoded=JsonSerializer.Deserialize<RecipeBatchProgress>(stream.ToArray());var completed=PlantingTransaction.Settle(decoded,receipt);
 Check(completed.Planted==100&&completed.Batches==1&&completed.Spent==100,"跨进程序列化接续确认结果");
}
// Match the live 100 ms polling cadence: native tool animations retain control until fully idle.
int gatheringStart=checks;
long At(int ms)=>now+TimeSpan.FromMilliseconds(ms).Ticks;
var gather=new GatheringProgress();gather.Select(123);gather.Issued(now,100,10);
Check(!gather.Observe(At(100),true,false,false,100,10),"第一帧 busy 不能结束动作");
for(int ms=200;ms<=2000;ms+=100)
 Check(!gather.Observe(At(ms),true,false,false,100,10)&&gather.Pending&&gather.Misses==0,"动画轮询只观察，不能重发、走位或记空挥");
Reject(()=>gather.Issued(At(2000),100,10),"动作中不能重复发起");
Reject(()=>gather.Reposition(),"动作中不能重新走位");
Reject(()=>gather.Select(456),"动作中不能因最近目标变化而换目标");
Check(!gather.Observe(At(2100),false,false,false,100,10),"短暂 idle 不是整段动作结束");
Check(!gather.Observe(At(2500),true,false,false,100,10),"原生下一段动作可以继续");
Check(!gather.Observe(At(3000),false,false,true,100,10),"动画结束仍须等待服务器");
Check(!gather.Observe(At(6000),false,false,false,100,11)&&gather.Progressed,"目标血量没变但邻格收获回执也算有效采集");
Check(!gather.Observe(At(6500),false,false,false,100,11),"回执后留出连续动作收尾间隔");
Check(gather.Observe(At(6600),false,false,false,100,11)&&!gather.Pending&&gather.Misses==1&&!gather.TargetProgressed,"邻格回执不能掩盖所选目标未命中，收尾后走位");
Check(gather.Observe(At(6700),false,false,false,100,11)&&gather.Misses==1,"重复刷新不会重复结算");
gather.Reset();gather.Select(123);
// Real empty swings, not high frequency status updates, trigger bounded recovery.
for(int attempt=0;attempt<2;attempt++)
{
 int t=7000+attempt*2000;gather.Issued(At(t),100,11);
 gather.Observe(At(t+100),true,false,false,100,11);
 gather.Observe(At(t+1000),false,false,false,100,11);
 Check(gather.Observe(At(t+1600),false,false,false,100,11)&&gather.Misses==attempt+1,"完整空挥只记一次");
 Check(gather.NeedsReposition,"一次完整空挥后就换站位，不能等原地连砍");
}
Check(gather.NeedsReposition,"完整无进展动作需要走位");
gather.Reposition();Check(gather.Repositions==1&&!gather.NeedsReposition,"走位后保留尝试次数并清除空挥");
gather.Issued(At(11000),100,11);gather.Observe(At(11100),true,false,false,80,11);
Check(gather.Progressed&&gather.Repositions==0,"实际命中清除恢复次数");
gather.Observe(At(12000),false,false,false,80,11);
Check(gather.Observe(At(12600),false,false,false,80,11)&&gather.Misses==0,"掉血但尚未产出奖励也算进展");
// Loading can exceed the old five-second timeout. Do not issue a second async tool load.
gather.Issued(At(13000),80,11);
Check(!gather.Observe(At(23000),false,true,false,80,11)&&gather.Misses==0,"加载十秒不能误判为空挥");
Check(!gather.Observe(At(23100),true,false,false,80,11),"加载结束进入动画仍保持当前动作");
gather.Observe(At(24000),false,false,false,0,12);
Check(gather.Observe(At(24600),false,false,false,0,12)&&gather.Progressed,"目标采完销毁前的血量或回执可结算");
gather.Issued(At(25000),1,12);
Check(!gather.Observe(At(25100),false,false,false,1,12),"没有 busy 也不能立刻再次触发");
Check(!gather.Observe(At(29900),false,false,false,1,12),"工具没有启动时保留五秒启动窗口");
Check(gather.Observe(At(30000),false,false,false,1,12)&&gather.Misses==1,"没有加载或动作且超过启动窗口才记未生效");
// Native continuous harvesting can run longer than the watchdog while receipts keep arriving.
gather.Select(456);gather.Issued(At(31000),1,12);
for(int i=1;i<=60;i++)
 Check(!gather.Observe(At(31000+i*2000),true,false,i%2==0,1,12+i)&&!gather.Stalled(At(31000+i*2000)),"持续两分钟有收获回执不受总运行时间限制");
Check(!gather.Stalled(At(195000)),"最近进展后四十四秒仍可等待");
Check(gather.Stalled(At(197000)),"确实四十五秒无进展才报告卡住");
gather.Reset();Check(!gather.Pending&&gather.Target==0,"用户暂停或退出场景清除本地动作状态");
Check(!gather.Observe(At(198000),true,false,false,1,72),"用户手动动作中也不能抢控制");
Check(!gather.Observe(At(198000),false,true,false,1,72),"用户手动加载工具时不能抢控制");
Check(!gather.Observe(At(198000),false,false,true,1,72),"非采集请求也须先完成");
Check(gather.Observe(At(198000),false,false,false,1,72),"完全空闲后可以开始下个操作");
int gatheringChecks=checks-gatheringStart;
int navigationStart=checks;
var nav=new NavigationProgress();nav.Select(11);nav.Begin(now,6);
for(int ms=100;ms<4000;ms+=100)
 Check(nav.Observe(At(ms),ms%200==0?6.1:6.5,true,false)==NavigationDecision.Moving,"栅栏前来回移动不算路径进展，但保留短暂恢复时间");
Check(nav.Observe(At(4000),6.1,true,false)==NavigationDecision.Retry,"绕圈四秒没有更短路径就停下改道");
nav.Begin(At(4100),7);
Check(nav.Observe(At(8100),7,true,false)==NavigationDecision.Skip&&nav.Attempts==2,"第二条路径也卡住就跳过，不无限改道");
Reject(()=>nav.Begin(At(8200),7),"同目标不能启动第三次寻路");
nav.Select(12);nav.Begin(At(9000),20);
Reject(()=>nav.Select(13),"寻路期间最近目标变化不能重置计时");
for(int i=1;i<=5;i++)
 Check(nav.Observe(At(9000+i*3000),20-i*2,true,false)==NavigationDecision.Moving,"沿 U 形绕行时剩余可走路径缩短即可继续");
Check(nav.Observe(At(25000),0,true,true)==NavigationDecision.Arrived,"到达实际落脚点结束寻路");
nav.Reset();nav.Select(13);nav.Begin(At(26000),8);
Check(nav.Observe(At(26300),double.PositiveInfinity,false,false)==NavigationDecision.Moving,"短暂重算没有有效路径时不立即判失败");
Check(nav.Observe(At(28300),double.NaN,false,false)==NavigationDecision.Retry,"持续两秒无有效路径需要改道");
nav.Reset();nav.Select(14);nav.Begin(At(30000),100);
for(int i=1;i<15;i++)
 Check(nav.Observe(At(30000+i*3000),100-i,true,false)==NavigationDecision.Moving,"实际进展可以续行但不能延长总时限");
Check(nav.Observe(At(75000),84,true,false)==NavigationDecision.Retry,"持续改道也受四十五秒单路径上限约束");
nav.Reset();Check(!nav.Active&&nav.Attempts==0,"暂停清除路径状态");
nav.Select(15);nav.Begin(At(80000),3);
nav.Observe(At(81000),2.6,true,false);
Check(nav.Observe(At(84500),2.6,true,false)==NavigationDecision.Moving,"真实进展后按新进度计时");
Check(nav.Observe(At(85000),2.6,true,false)==NavigationDecision.Retry,"相同最短距离反复出现不算新进展");
nav.Reset();nav.Select(16);nav.Begin(At(90000),5);nav.CancelAttempt();
Check(!nav.Active&&nav.Attempts==1,"弹窗打断移动时释放活动路径，保留尝试上限");
nav.Begin(At(91000),4);Check(nav.Active&&nav.Attempts==2,"关闭弹窗后可以接续下一次寻路，不因旧 Active 标记暂停");
int navigationChecks=checks-navigationStart;
int readinessStart=checks;
Check(!ResourceReadiness.Mature(1,3)&&!ResourceReadiness.Mature(2,3),"可砍的幼树与中期树也必须等待成熟");
Check(ResourceReadiness.Mature(3,3)&&ResourceReadiness.Mature(4,3),"达到最终阶段允许正常采集");
Check(!ResourceReadiness.Mature(0,3)&&!ResourceReadiness.Mature(1,0),"未生长或未知最终阶段不采集");
Check(ResourceReadiness.Mature(1,1),"本身只有一阶段的资源仍可采集");
Check(!ResourceReadiness.EmptyField(false,false,false,false),"服务器世界数据未载入不能冒充空田");
Check(!ResourceReadiness.EmptyField(true,true,false,false),"服务器仍有作物不能覆盖");
Check(!ResourceReadiness.EmptyField(true,false,true,false),"已预览或动画尚未释放的田格不能二次预览");
Check(!ResourceReadiness.EmptyField(true,false,false,true),"仍有存活作物时等待原生状态同步");
// Regression: all crops harvested, but 26 cached seed selections still hold the previous seed id.
var harvested=Enumerable.Range(0,100).Select(i=>new{Seed=i<74?0:1,Known=true,ServerCrop=false,Occupied=false,Live=false}).ToArray();
Check(harvested.Count(f=>ResourceReadiness.EmptyField(f.Known,f.ServerCrop,f.Occupied,f.Live))==100,"74 零缓存加 26 旧种子缓存仍是 100 块已收空田");
var replant=new RecipeBatchProgress{Account="a",SeedId=4};
var replantIntent=PlantingTransaction.Begin(replant,Fields(),3,"replant",300,"replant-100");
Check(PlantingTransaction.MatchesRequest(replantIntent,Cells(Fields(),4,3)),"收完小麦后仍通过原生一次 100 格蘑菇事务核验");
Check(PlantingTransaction.Settle(replantIntent,Reply(replantIntent)).Batches==1,"补种回执按整批记账，不逐格发送");
// Recorded Runtime6 failure: cache=16, occupancy=0, live=0 after 100 confirmed mushrooms.
var empty84=Enumerable.Range(0,100).Select(_=>new FarmEmptyProgress()).ToArray();
Check(empty84.Select((x,i)=>x.Observe(now,true,i>=84,false,false,true,false,false)).Count(x=>x)==84,"旧缓存初次观察不能立刻推断全部空田");
Check(empty84.Select((x,i)=>x.Observe(now+TimeSpan.FromSeconds(3).Ticks,true,i>=84,false,false,true,false,false)).All(x=>x),"原生空田一致且稳定，16 个旧缓存不再阻止补种");
Check(empty84.All(x=>!x.Observe(now+TimeSpan.FromSeconds(4).Ticks,true,true,true,true,false,false,false)),"新播种重新占用后全部立刻排除");
for(int mask=0;mask<16;mask++)
{
 var field=new FarmEmptyProgress();bool occupied=(mask&1)!=0,live=(mask&2)!=0,preview=(mask&4)==0,pending=(mask&8)!=0;
 Check(field.Observe(now,true,true,occupied,live,preview,pending,true)==(mask==0),"成功采集也不能绕过当前原生状态 "+mask);
}
var unknownField=new FarmEmptyProgress();Check(!unknownField.Observe(now,false,false,false,false,true,false,true),"缺失身份仍拒绝补种");
var interruptedField=new FarmEmptyProgress();interruptedField.Observe(now,true,true,false,false,true,false,false);interruptedField.Observe(now+TimeSpan.FromSeconds(1).Ticks,true,true,false,false,true,true,false);
Check(!interruptedField.Observe(now+TimeSpan.FromSeconds(3).Ticks,true,true,false,false,true,false,false),"网络在途打断稳定空田观察");
int interactionStart=checks;
// Actual failure: native nearest may be another immature ore while the selected mature ore is detected.
var interaction=new InteractionProgress();interaction.Select(17);
Check(interaction.Decide(now,false,true)==InteractionDecision.Approach,"远处尚未到位需要寻路");
interaction.Arrived(now);
Check(interaction.Decide(now+TimeSpan.FromMilliseconds(100).Ticks,false,true)==InteractionDecision.WaitForDetection,"旧版到位 100ms 后不能再耗一次寻路");
Check(interaction.Decide(now+TimeSpan.FromMilliseconds(600).Ticks,false,true)==InteractionDecision.WaitForDetection,"给 200ms 探测刷新多个周期");
Check(interaction.Decide(now+TimeSpan.FromMilliseconds(250).Ticks,true,true)==InteractionDecision.UseTool,"选中目标在原生探测集合即能操作，不要求最近身份一致");
Check(interaction.Decide(now+TimeSpan.FromMilliseconds(750).Ticks,false,true)==InteractionDecision.Reposition,"探测确实未进入才改站位");
interaction.Select(18);Check(interaction.Decide(now+TimeSpan.FromSeconds(2).Ticks,false,true)==InteractionDecision.Approach,"换目标不继承旧到位状态");
Check(interaction.Decide(now,true,false)!=InteractionDecision.UseTool,"未成熟目标不发工具");
var memory=new TargetFailureMemory();memory.Failed(now);Check(!memory.Available(now+TimeSpan.FromSeconds(14).Ticks),"首次失败冷却");
memory.Failed(now);Check(!memory.Available(now+TimeSpan.FromSeconds(29).Ticks),"重复失败只短暂退避，不立即顶墙重试");
memory.Failed(now);Check(memory.Until==now+TimeSpan.FromSeconds(60).Ticks,"多次失败最长一分钟，不能丢掉矿点");memory.Succeeded();Check(memory.Available(now)&&memory.Failures==0,"实得进展清除失败记录");
for(int mask=0;mask<64;mask++)Check(RuntimeHandoffRules.CanStop((mask&1)!=0,(mask&2)!=0,(mask&4)!=0,(mask&8)!=0,(mask&16)!=0,(mask&32)!=0)==(mask==0),"更新只允许全部空闲组合 "+mask);
var detour=new NavigationProgress();detour.Select(42);detour.Begin(now,10);detour.Replan(now+TimeSpan.FromSeconds(3).Ticks,15);
Check(detour.Observe(now+TimeSpan.FromSeconds(4).Ticks,14,true,false)==NavigationDecision.Moving,"绕开实体时允许一次变长路径");
Reject(()=>detour.Replan(now+TimeSpan.FromSeconds(5).Ticks,18),"不能无限次绕行续时");
Check(detour.Observe(now+TimeSpan.FromSeconds(29).Ticks,1,true,false)==NavigationDecision.Retry,"绕行不延长原总时间上限");
int travelStart=checks;
var travel=new TravelProgress();
Check(!travel.BeginDash(now,true,true),"没有目标不冲刺");travel.Select(42);
Check(!travel.BeginDash(now,false,true)&&!travel.BeginDash(now,true,false)&&!travel.DashUsed,"关闭或原生次数不足不消耗脱困机会");
Check(travel.BeginDash(now,true,true),"允许一次脱困");
Check(!travel.FinishDash(now+TimeSpan.FromMilliseconds(100).Ticks,.5,true,true),"有效短冲刺不中断");
Check(travel.FinishDash(now+TimeSpan.FromMilliseconds(450).Ticks,0,true,true),"原地受阻也在 450ms 结束，不能一直顶墙");
Check(travel.FinishDash(now+TimeSpan.FromMilliseconds(100).Ticks,1.2,true,true),"足够距离即重算，不耗尽冲刺时长");
Check(travel.FinishDash(now,0,false,true),"原生拒绝或自然结束也进入收尾");
Check(travel.FinishDash(now,0,true,false),"运行中关闭冲刺立即收尾");travel.EndDash();travel.Select(42);
Check(!travel.BeginDash(now+TimeSpan.FromSeconds(5).Ticks,true,true),"同目标重新寻路不能连续消耗冲刺");
travel.Select(43);Check(travel.BeginDash(now,true,true),"新目标可尝试脱困");travel.EndDash();
for(int mask=0;mask<16;mask++)Check(TravelProgress.ShouldMount((mask&1)!=0,(mask&2)!=0,(mask&4)!=0,(mask&8)!=0,8)==(mask==1),"上车条件组合 "+mask);
Check(!TravelProgress.ShouldMount(true,false,false,false,5.9),"短途不反复上车");Check(TravelProgress.ShouldMount(true,false,false,false,6),"长途上车阈值");
Check(!TravelProgress.ShouldMount(true,false,false,false,double.PositiveInfinity),"坏路径不调用载具");
Check(TravelProgress.ShouldDismount(true,true,false,2),"采集点附近下车");Check(!TravelProgress.ShouldDismount(true,true,false,3),"中间距离维持载具，避免阈值抖动");
Check(TravelProgress.ShouldDismount(false,true,false,20)&&TravelProgress.ShouldDismount(true,false,false,20)&&TravelProgress.ShouldDismount(true,true,true,20),"关闭、停步与绕行都下车");
var vehicle=new VehicleRequest();vehicle.Begin(now);Reject(()=>vehicle.Begin(now),"异步加载禁止重复调用上车");
vehicle.Cancel();Check(!vehicle.Complete(true)&&!vehicle.Pending,"暂停后迟到回调不能留下骑乘状态");
vehicle.Begin(now);Check(!vehicle.Complete(false),"目标或场景变化的回调不能留下骑乘状态");
vehicle.Begin(now);Check(vehicle.Complete(true),"同目标仍在赶路才保留载具");
vehicle.Begin(now);Check(!vehicle.TimedOut(now+TimeSpan.FromSeconds(7).Ticks)&&vehicle.TimedOut(now+TimeSpan.FromSeconds(8).Ticks),"挂起加载有明确超时");vehicle.Cancel();Check(vehicle.Pending,"取消不能伪造加载已完成，交接须等待真实回调");vehicle.Complete(false);
var recovery=new NavigationProgress();recovery.Select(42);recovery.Begin(now,2);
Check(recovery.Observe(now+TimeSpan.FromSeconds(4).Ticks,2,true,false)==NavigationDecision.Retry,"复现实际停滞后待重试");
Check(recovery.Recover(now+TimeSpan.FromMilliseconds(4500).Ticks,4)&&recovery.Attempts==1,"冲刺后允许从实际位置重算且保留原尝试计数");
Check(!recovery.Recover(now+TimeSpan.FromSeconds(5).Ticks,6),"冲刺不能无限重置进展");
Check(recovery.Observe(now+TimeSpan.FromSeconds(12).Ticks,.5,true,false)==NavigationDecision.Retry,"脱困不能延长原始总期限");
Check(!recovery.Recover(now+TimeSpan.FromSeconds(13).Ticks,1),"已到硬时限不能借冲刺恢复");
recovery.Begin(now+TimeSpan.FromSeconds(14).Ticks,2);recovery.CancelAttempt();Check(!recovery.Recover(now+TimeSpan.FromSeconds(15).Ticks,1),"显式暂停不能被恢复路径复活");
var options=JsonSerializer.Deserialize<TerritorySettings>("{\"Mining\":true,\"IntervalMs\":500}");Check(options.UseVehicle&&options.DashRecovery,"已有设置缺省开启两个新开关");
checks+=PlantingPreviewTests.Run();checks+=FixedCropTests.Run();checks+=AdaptiveNavigationTests.Run();
int localStart=checks;checks+=LocalNavigationTests.Run();int npcStart=checks;checks+=NpcOccupancyTests.Run();int popupStart=checks;checks+=BD2Territory.Runtime.PopupFlowTests.Run();
int popupFlowChecks=checks-popupStart;int layoutRecipeChecks=LayoutRecipeTests.Run();checks+=layoutRecipeChecks;
Console.WriteLine(JsonSerializer.Serialize(new{layoutRecipeChecks,status="passed",assertions=checks,gatheringChecks,navigationChecks,readinessChecks=interactionStart-readinessStart,interactionAndHandoffChecks=travelStart-interactionStart,travelChecks=localStart-travelStart,localNavigationChecks=npcStart-localStart,npcOccupancyChecks=popupStart-npcStart,popupFlowChecks,simulatedBatches=100,totalPotato=totals[2],totalWheat=totals[1],totalMushroom=totals[4]}));
