using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
namespace BD2Territory.Desktop;
public partial class TerritoryWindow : Window
{
 private readonly string root;
 private readonly TerritoryControlLink link;
 private readonly DispatcherTimer timer;
 private TerritorySnapshot? snapshot;
 private bool initialized,connecting,closing,smoke;
 private DateTime lastDiagnostic;private int selectedRecipe=3;private bool refreshingRecipes;private string recipeCatalogKey="";
 public TerritoryWindow(string? dataRoot=null)
 {
  root=dataRoot??TerritoryIdentity.DataRoot;link=new(root);InitializeComponent();InitializeLanguage();
  Set(VersionText,"0.3.2 beta");
  var settings=TerritoryJson.Read<TerritorySettings>(Path.Combine(root,"settings.json"))??new();
  if(!settings.ValidSettings())settings=new();
  selectedRecipe=settings.RecipeId;RecipeBox.ItemsSource=new[]{new RecipeOption{Id=selectedRecipe,Name=selectedRecipe==3?"活力面疙瘩":"配方 "+selectedRecipe,Available=true}};RecipeBox.SelectedValue=selectedRecipe;
  CookingBox.IsChecked=settings.Cooking;CookingBatchBox.Text=settings.CookingBatch.ToString();LoggingBox.IsChecked=settings.Logging;MiningBox.IsChecked=settings.Mining;FarmingBox.IsChecked=settings.Farming;
  NavMeshBox.IsChecked=settings.UseNavMesh;VehicleBox.IsChecked=settings.UseVehicle;DashBox.IsChecked=settings.DashRecovery;
  Set(IntervalBox,settings.IntervalMs.ToString());Set(BudgetBox,settings.PlantingBudget.ToString());Set(DataPathBox,root);
  link.Configure(settings);initialized=true;timer=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(500)};timer.Tick+=(_,_)=>{Refresh();};timer.Start();
  Closed+=(_,_)=>{closing=true;timer.Stop();link.Dispose();};
 }
 private TerritorySettings Settings()
 {
  if(!int.TryParse(CookingBatchBox.Text,out int cookingBatch)||!int.TryParse(IntervalBox.Text,out int interval)||!long.TryParse(BudgetBox.Text,out long budget))throw new InvalidOperationException("间隔和预算请输入整数。");
  var s=new TerritorySettings{Cooking=CookingBox.IsChecked==true,CookingBatch=cookingBatch,RecipeId=selectedRecipe,Logging=LoggingBox.IsChecked==true,Mining=MiningBox.IsChecked==true,Farming=FarmingBox.IsChecked==true,UseNavMesh=NavMeshBox.IsChecked==true,UseVehicle=VehicleBox.IsChecked==true,DashRecovery=DashBox.IsChecked==true,IntervalMs=interval,PlantingBudget=budget};
  if(!s.ValidSettings())throw new InvalidOperationException("间隔范围 100–60000 毫秒，预算范围 0–10000000；料理每批 1–1000 份。");return s;
 }
 private void SettingsChanged(object sender,RoutedEventArgs e)
 {
  if(!initialized||closing)return;
  try{link.Configure(Settings());Set(SettingsHint,"设置已保存。预算 0 表示不限；暂停后重新开始会重置本次预算。");SettingsHint.Foreground=(Brush)FindResource("MutedBrush");}
  catch(Exception ex){Set(SettingsHint,ex.GetBaseException().Message);SettingsHint.Foreground=Brushes.Firebrick;}
 }
 private void RecipeChanged(object sender,System.Windows.Controls.SelectionChangedEventArgs e)
 {if(!initialized||refreshingRecipes)return;if(RecipeBox.SelectedItem is RecipeOption r){selectedRecipe=r.Id;SettingsChanged(sender,e);Set(RecipeHint,r.Available?r.Ratio+"。每批同种 100 个；正在播种时，本批回执确认后切换。":r.Reason);}}
 private void LayoutClick(object sender,RoutedEventArgs e)
 {if(link.Enabled){ShowError("请先暂停采集和种植，再打开布局工具。");return;}new LayoutWindow(root,link){Owner=this}.ShowDialog();Refresh();}
 private void ShowError(string text){Set(ErrorText,text);ErrorText.Visibility=string.IsNullOrEmpty(text)?Visibility.Collapsed:Visibility.Visible;}
 private async void ConnectClick(object sender,RoutedEventArgs e)
 {
  if(connecting||link.Enabled)return;connecting=true;ConnectButton.IsEnabled=false;ShowError("");Set(StatusText,"正在连接领地组件");
  try
  {
   var message=await Task.Run(()=>new TerritoryConnection(root).Connect(text=>Dispatcher.InvokeAsync(()=>{if(!closing)Set(ReasonText,text);})));
   if(!closing){Set(ReasonText,message);Refresh();}
  }
  catch(Exception ex){if(!closing){Set(StatusText,"连接未完成");ShowError(ex.GetBaseException().Message);}}
  finally{connecting=false;if(!closing)ConnectButton.IsEnabled=!link.Enabled;}
 }
 private void StartClick(object sender,RoutedEventArgs e)
 {
  try
  {
   var settings=Settings();if((settings.Farming||settings.Cooking)&&snapshot?.Recipes?.Length>0&&snapshot.Recipes.FirstOrDefault(r=>r.Id==settings.RecipeId)?.Available!=true)throw new InvalidOperationException("当前配方尚不可自动种植，请选择已解锁配方或取消种植项目。");if(!settings.Logging&&!settings.Mining&&!settings.Farming&&!settings.Cooking)throw new InvalidOperationException("请至少选择一项执行项目。");
   if(!TerritoryControlLink.Fresh(snapshot,DateTime.UtcNow)||!snapshot!.Ready)throw new InvalidOperationException("请先连接游戏并进入可走动的领地，等待实时状态。");
   if(!smoke)
   {
    using var game=TerritoryConnection.FindGame();var saved=TerritoryJson.Read<TerritoryConnectionState>(Path.Combine(root,"connection.json"));
    if(game.Id!=snapshot.ProcessId||saved==null||!TerritoryConnection.SameProcess(saved,game.Id,game.StartTime.ToUniversalTime().Ticks))throw new InvalidOperationException("游戏进程已变化，请重新连接。");
   }
   link.Configure(settings);link.Start(snapshot.ProcessId);ShowError("");Refresh();
  }
  catch(Exception ex){ShowError(ex.GetBaseException().Message);}
 }
 private void StopClick(object sender,RoutedEventArgs e)
 {
  try{link.Stop();ShowError("");Set(StatusText,"自动化已暂停");Set(ReasonText,"批次进度会保留；已提交的播种等待服务器确认。");}
  catch(Exception ex){ShowError(ex.GetBaseException().Message+"；若停止文件未写入，控制租约会在 10 秒内失效。");}
  StartButton.IsEnabled=TerritoryControlLink.Fresh(snapshot,DateTime.UtcNow)&&snapshot!.Ready;StopButton.IsEnabled=false;ConnectButton.IsEnabled=!connecting;
 }
 private void Refresh()
 {
  if(closing)return;snapshot=TerritoryJson.Read<TerritorySnapshot>(Path.Combine(root,"latest.json"));
  bool fresh=TerritoryControlLink.Fresh(snapshot,DateTime.UtcNow);StartButton.IsEnabled=fresh&&snapshot!.Ready&&!link.Enabled&&!connecting;StopButton.IsEnabled=link.Enabled;ConnectButton.IsEnabled=!connecting&&!link.Enabled;
  if(!fresh)
  {
   if(!connecting){Set(StatusText,link.Enabled?"等待游戏恢复实时状态":"尚未连接领地实时状态");Set(ReasonText,link.Enabled?"请检查游戏是否正在加载。当前设置保持，恢复领地状态后继续。":"连接游戏并进入可走动的领地后，即可开始。");}
   UpdateDiagnostics();return;
  }
  var s=snapshot!;
  if(s.Recipes?.Length>0){var key=string.Join("|",s.Recipes.Select(v=>v.Id+v.Label+v.Ratio));if(!RecipeBox.IsDropDownOpen&&key!=recipeCatalogKey){refreshingRecipes=true;RecipeBox.ItemsSource=s.Recipes.Select(v=>new RecipeOption{Id=v.Id,Name=v.Name,Ratio=v.Ratio,Reason=ui.Language.Text(v.Reason),Available=v.Available}).ToArray();RecipeBox.SelectedValue=selectedRecipe;recipeCatalogKey=key;refreshingRecipes=false;}var r=s.Recipes.FirstOrDefault(v=>v.Id==selectedRecipe);Set(RecipeHint,r==null?"当前客户端已无此配方，请重新选择。":r.Available?r.Ratio+"。每批同种 100 个；切换时先完成当前播种回执。":r.Reason);}
  if(link.Enabled&&s.OwnerId==link.OwnerId&&!string.IsNullOrEmpty(s.Error))
  {try{link.Stop();}catch{}ShowError(s.Error);}
  bool ours=s.OwnerId==link.OwnerId;
  Set(StatusText,link.Enabled?"自动化运行中":ErrorText.Visibility==Visibility.Visible?"自动化已暂停":s.Ready?"领地已识别":"等待进入领地");
  Set(ReasonText,link.Enabled?(ours?s.Reason:"等待游戏接收开始指令"):s.Ready?"设置完成后点击「开始自动化」。":"请进入可走动的 Fantasia Territory 领地。");
  if(!string.IsNullOrEmpty(link.Error))ShowError(link.Error);
  Set(TargetText,link.Enabled&&ours?s.Target:"");
  Set(BatchText,s.BatchSeedId>0?$"当前作物：{s.Crops?.FirstOrDefault(c=>c.SeedId==s.BatchSeedId)?.Name??s.BatchSeedId.ToString()}　已确认 {s.BatchPlanted} / 100　完成 {s.CompletedBatches} 批":"开启后显示当前批次");
  Set(CookingText,$"已确认制作 {s.Cooked} 份"+(s.CookingState=="pending"?" · 等待服务器确认":""));
  BatchBar.Value=Math.Clamp(s.BatchPlanted,0,100);
  CropsGrid.ItemsSource=(s.Crops??Array.Empty<CropStock>()).Select(c=>new CropRow(c.Name,c.Required,c.Inventory,c.Growing,ui.Language.Text(c.GrowthSeconds>=60?$"{c.GrowthSeconds/60} 分钟":$"{c.GrowthSeconds} 秒"))).ToArray();
  Set(StatsText,$"耕地 {s.Fields}，空地 {s.EmptyFields}，可收作物 {s.Mature} · 成熟树 {s.Trees} · 成熟矿点 {s.Ores}\n已确认采集 {s.GatherReplies} 次，物品 {s.ReceivedItems} 个 · 本次播种已用 {s.Spent}");
  StartButton.IsEnabled=s.Ready&&!link.Enabled&&!connecting;StopButton.IsEnabled=link.Enabled;ConnectButton.IsEnabled=!connecting&&!link.Enabled;UpdateDiagnostics();
 }
 private void DiagnosticsExpanded(object sender,RoutedEventArgs e){if(initialized){lastDiagnostic=DateTime.MinValue;UpdateDiagnostics();}}
 private void UpdateDiagnostics()
 {
  if(!DiagnosticsPanel.IsExpanded||DateTime.UtcNow-lastDiagnostic<TimeSpan.FromSeconds(1))return;lastDiagnostic=DateTime.UtcNow;
  var runtime=TerritoryJson.Read<TerritoryRuntimeStatus>(Path.Combine(root,"runtime.json"));
  string status=$"组件：{runtime?.Runtime??TerritoryIdentity.RuntimeName}\n连接：{runtime?.State??"未连接"}　PID：{runtime?.ProcessId??0}\n最近心跳：{(snapshot==null?"无":new DateTime(Math.Clamp(snapshot.CapturedUtcTicks,0,DateTime.MaxValue.Ticks),DateTimeKind.Utc).ToLocalTime().ToString("HH:mm:ss"))}\n场景：{snapshot?.Scene}\n农田：{snapshot?.FarmState}\n网络：{snapshot?.Network}\n{runtime?.Error}\n{link.Error}\n";
  try
  {
   var path=Path.Combine(root,"runtime.log");if(File.Exists(path)){using var stream=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.ReadWrite|FileShare.Delete);stream.Seek(Math.Max(0,stream.Length-16000),SeekOrigin.Begin);using var reader=new StreamReader(stream);status+="\n最近日志\n"+reader.ReadToEnd();}
  }
  catch(IOException){status+="\n日志正在更新，请稍候。";}catch(UnauthorizedAccessException){status+="\n无法读取日志，请检查目录权限。";}
  Set(DiagnosticsBox,status);
 }
 private sealed record CropRow(string Name,int Required,long Inventory,int Growing,string Growth);
 internal async Task SmokeAsync(string output)
 {
  smoke=true;LanguageBox.SelectedIndex=0;ui.Language.Select("zh-CN");ui.Apply();ShowActivated=false;ShowInTaskbar=false;Show();await Dispatcher.InvokeAsync(()=>{},DispatcherPriority.ApplicationIdle);
  var checks=new List<string>();void Check(bool ok,string label){if(!ok)throw new InvalidOperationException(label);checks.Add(label);}
  Check(!link.Enabled&&!StartButton.IsEnabled&&TerritoryJson.Read<TerritoryControl>(Path.Combine(root,"control.json"))?.Enabled==false,"启动不自动操作游戏");
  Check(LoggingBox.IsChecked==true&&MiningBox.IsChecked==true&&FarmingBox.IsChecked==true,"默认三个项目可见并选中");
  Check(IntervalBox.Text=="500"&&BudgetBox.Text=="1400","默认间隔与预算");
  Check(VehicleBox.IsChecked==true&&DashBox.IsChecked==true,"默认开启载具赶路与冲刺脱困");
  Check(NavMeshBox.IsChecked==false,"默认不启用 NavMesh，使用 A* 寻路");
  StartClick(this,new());Check(!link.Enabled&&ErrorText.Visibility==Visibility.Visible,"无心跳不能开始");ShowError("");
  var s=new TerritorySnapshot{Ready=true,ProcessId=424242,CapturedUtcTicks=DateTime.UtcNow.Ticks,Scene="Fantasia Territory",Fields=100,EmptyFields=38,Trees=8,Ores=5,Mature=12,BatchSeedId=2,BatchPlanted=100,CompletedBatches=3,Spent=100,GatherReplies=27,ReceivedItems=64,Crops=new[]{new CropStock{SeedId=2,Name="弯弯土豆",Required=5,Inventory=300,Growing=62,GrowthSeconds=60},new CropStock{SeedId=1,Name="黏糯小麦",Required=3,Inventory=200,Growing=0,GrowthSeconds=300},new CropStock{SeedId=4,Name="活力蘑菇",Required=2,Inventory=100,Growing=0,GrowthSeconds=900}}};
  void Feed(){s.CapturedUtcTicks=DateTime.UtcNow.Ticks;TerritoryJson.Write(Path.Combine(root,"latest.json"),s);Refresh();}
  Feed();Check(StartButton.IsEnabled&&CropsGrid.Items.Count==3,"识别领地后展示配方并允许开始");
  IntervalBox.Text="50";StartClick(this,new());Check(!link.Enabled,"非法间隔不启动");IntervalBox.Text="500";
  LoggingBox.IsChecked=false;MiningBox.IsChecked=false;FarmingBox.IsChecked=false;StartClick(this,new());Check(!link.Enabled,"不能启动空项目");
  LoggingBox.IsChecked=true;MiningBox.IsChecked=true;FarmingBox.IsChecked=true;StartClick(this,new());
  var command=TerritoryJson.Read<TerritoryControl>(Path.Combine(root,"control.json"));Check(link.Enabled&&command!=null&&command.Valid(DateTime.UtcNow.Ticks,424242),"开始写入有效短租约");
  s.OwnerId=link.OwnerId;s.Enabled=true;s.Reason="使用当前工具正常采集";s.Target="收获作物";Feed();
  Check(StopButton.IsEnabled&&!StartButton.IsEnabled&&!ConnectButton.IsEnabled,"运行中可暂停且禁止重复连接");
  Check(BatchBar.Value==100&&BatchText.Text.Contains("100 / 100"),"批次显示确认进度");
  MiningBox.IsChecked=false;command=TerritoryJson.Read<TerritoryControl>(Path.Combine(root,"control.json"));Check(command?.Mining==false,"运行开关同步保存");MiningBox.IsChecked=true;
  VehicleBox.IsChecked=false;DashBox.IsChecked=false;command=TerritoryJson.Read<TerritoryControl>(Path.Combine(root,"control.json"));Check(command!=null&&!command.UseVehicle&&!command.DashRecovery,"运行时关闭移动辅助同步到组件");
  var saved=TerritoryJson.Read<TerritorySettings>(Path.Combine(root,"settings.json"));Check(saved!=null&&!saved.UseVehicle&&!saved.DashRecovery,"移动辅助偏好持久保存");VehicleBox.IsChecked=true;DashBox.IsChecked=true;
  command=TerritoryJson.Read<TerritoryControl>(Path.Combine(root,"control.json"));Check(command!=null&&command.UseVehicle&&command.DashRecovery,"运行时重新开启移动辅助");
  NavMeshBox.IsChecked=true;command=TerritoryJson.Read<TerritoryControl>(Path.Combine(root,"control.json"));saved=TerritoryJson.Read<TerritorySettings>(Path.Combine(root,"settings.json"));Check(command?.UseNavMesh==true&&saved?.UseNavMesh==true,"显式启用 NavMesh 同步并持久保存");
  NavMeshBox.IsChecked=false;command=TerritoryJson.Read<TerritoryControl>(Path.Combine(root,"control.json"));saved=TerritoryJson.Read<TerritorySettings>(Path.Combine(root,"settings.json"));Check(command?.UseNavMesh==false&&saved?.UseNavMesh==false&&link.Enabled,"运行中关闭 NavMesh 不停止自动化");
  s.OwnerId="previous-run";s.Error="old error";Feed();Check(link.Enabled,"旧会话错误不停止当前任务");s.OwnerId=link.OwnerId;s.Error="播种货币不足";Feed();Check(!link.Enabled&&ErrorText.Text==s.Error,"当前会话错误明确暂停");
  s.Error="";Feed();ShowError("");StartClick(this,new());StopClick(this,new());command=TerritoryJson.Read<TerritoryControl>(Path.Combine(root,"control.json"));Check(command?.Enabled==false&&command.UntilUtcTicks==0,"暂停立即撤销租约");
  s.CapturedUtcTicks=DateTime.UtcNow.AddSeconds(-20).Ticks;TerritoryJson.Write(Path.Combine(root,"latest.json"),s);Refresh();Check(!StartButton.IsEnabled,"过期心跳不能开始");
  Feed();DiagnosticsPanel.IsExpanded=true;UpdateDiagnostics();Check(DataPathBox.Text==root&&DiagnosticsBox.Text.Contains("组件"),"诊断在界面内展示");DiagnosticsPanel.IsExpanded=false;
  s.Recipes=new[]{new RecipeOption{Id=3,Name="活力面疙瘩",Available=true,Ratio="土豆 5 : 小麦 3 : 蘑菇 2"},new RecipeOption{Id=7,Name="双原料料理",Available=true,Ratio="土豆 40 : 胡萝卜 20"}};Feed();
  RecipeBox.SelectedValue=7;command=TerritoryJson.Read<TerritoryControl>(Path.Combine(root,"control.json"));Check(command?.RecipeId==7&&!command.Enabled,"暂停时也保存并同步所选配方");Check(RecipeHint.Text.Contains("40"),"配方显示动态比例");RecipeBox.SelectedValue=3;
  var layoutWorld=new LayoutWorld{Account="test",WorldId=3,ProcessId=424242,CapturedUtcTicks=DateTime.UtcNow.Ticks,Chunks=new[]{new LayoutChunk{Id=1}},Catalog=new[]{new LayoutItem{Id=30008,Name="农田",Unlocked=true,Function=3,MaxCount=100,Layer=3,Costs=new[]{new LayoutCost{Id=1011,Type=66,Name="木材",Count=5,Owned=700}}}}};
  TerritoryJson.Write(Path.Combine(root,"layout-world.json"),layoutWorld);
  using(var layoutLink=new TerritoryControlLink(root))
  {
   var layout=new LayoutWindow(root,layoutLink){ShowActivated=false,ShowInTaskbar=false};layout.SmokeSetup(layoutWorld,LayoutPlanner.Template(layoutWorld,1,3));layout.Show();await Dispatcher.InvokeAsync(()=>{},DispatcherPriority.ApplicationIdle);layout.UpdateLayout();
   Check(layout.Board.Children.Count==100,"布局区域以百格棋盘展示");Check(layout.CostGrid.Items.Count==1&&!layout.ApplyButton.IsEnabled,"费用可见，未预检不可购买");Check(layout.SummaryText.Text.Contains("100"),"布局显示新增数量");
   var lb=new RenderTargetBitmap((int)layout.ActualWidth,(int)layout.ActualHeight,96,96,PixelFormats.Pbgra32);lb.Render(layout);var le=new PngBitmapEncoder();le.Frames.Add(BitmapFrame.Create(lb));using(var f=File.Create(Path.Combine(output,"layout.png")))le.Save(f);
   layout.SmokeFlow(Check);layout.Width=780;layout.Height=640;layout.UpdateLayout();Check(layout.PreviewButton.IsVisible&&layout.PauseButton.IsVisible,"布局小窗口保留操作按钮");layout.Close();Check(!layoutLink.Enabled,"关闭布局窗口只停止布局控制");
  }
  await Dispatcher.InvokeAsync(()=>{},DispatcherPriority.ApplicationIdle);UpdateLayout();
  var bitmap=new RenderTargetBitmap((int)ActualWidth,(int)ActualHeight,96,96,PixelFormats.Pbgra32);bitmap.Render(this);var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bitmap));using(var f=File.Create(Path.Combine(output,"territory.png")))encoder.Save(f);
  var settingsBefore=File.ReadAllText(Path.Combine(root,"settings.json"));
  LanguageBox.SelectedIndex=1;await Dispatcher.InvokeAsync(()=>{},DispatcherPriority.ApplicationIdle);UpdateLayout();
  Check(!link.Enabled&&File.ReadAllText(Path.Combine(root,"settings.json"))==settingsBefore,"切换英语不会改变运行设置或启动自动化");
  Check((string)Resources["Ui62"]=="Start automation"&&RecipeBox.SelectedValue is int,"英语主操作与配方选择保留");
  Check(!CookingBox.IsChecked.GetValueOrDefault()&&CookingBatchBox.Text=="100","料理默认关闭且批次限制可见");
  Check(UiLabels.All.Values.All(v=>ui.Language.Text(v)!=v||!System.Text.RegularExpressions.Regex.IsMatch(v,"[\u4e00-\u9fff]")),"全部静态界面文本覆盖英文");
  var enBitmap=new RenderTargetBitmap((int)ActualWidth,(int)ActualHeight,96,96,PixelFormats.Pbgra32);enBitmap.Render(this);var enEncoder=new PngBitmapEncoder();enEncoder.Frames.Add(BitmapFrame.Create(enBitmap));using(var f=File.Create(Path.Combine(output,"territory-en.png")))enEncoder.Save(f);
  LanguageBox.SelectedIndex=0;
  Width=720;Height=610;UpdateLayout();Check(StartButton.IsVisible&&StopButton.IsVisible&&ConnectButton.IsVisible,"最小窗口保留操作栏");
  StartClick(this,new());Close();command=TerritoryJson.Read<TerritoryControl>(Path.Combine(root,"control.json"));Check(command?.Enabled==false,"关闭窗口停止控制租约");
  var restored=new TerritoryWindow(root);Check(restored.NavMeshBox.IsChecked==false,"重新打开仍默认 A*");restored.NavMeshBox.IsChecked=true;restored.Close();
  restored=new TerritoryWindow(root);Check(restored.NavMeshBox.IsChecked==true,"重新打开保留主动选择的 NavMesh");restored.NavMeshBox.IsChecked=false;restored.Close();
  File.WriteAllText(Path.Combine(output,"results.json"),JsonSerializer.Serialize(new{status="pass",assertions=checks,isolatedRoot=root}));
 }
}
