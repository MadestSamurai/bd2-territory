using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using Microsoft.Win32;
namespace BD2Territory.Desktop;
public partial class LayoutWindow:Window
{
 private readonly string root;private readonly TerritoryControlLink link;private readonly DispatcherTimer timer;
 private LayoutWorld? world;private LayoutDocument? document;private LayoutQuote? quote;private string token="",prepared="";private bool refreshing;
 public LayoutWindow(string root,TerritoryControlLink link)
 {
  this.root=root;this.link=link;InitializeComponent();InitializeLanguage();timer=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(500)};timer.Tick+=(_,_)=>{Refresh();};timer.Start();
  Closed+=(_,_)=>{timer.Stop();try{link.Stop();}catch{}};Refresh();
 }
 private void Attempt(Action action){try{action();MessageText.Foreground=(Brush)FindResource("TextBrush");}catch(Exception e){Set(MessageText,e.GetBaseException().Message);MessageText.Foreground=Brushes.Firebrick;}}
 private bool Fresh()=>world!=null&&world.Runtime==TerritoryIdentity.RuntimeName&&world.ProcessId>0&&world.CapturedUtcTicks>=DateTime.UtcNow.AddSeconds(-5).Ticks&&world.CapturedUtcTicks<=DateTime.UtcNow.AddSeconds(2).Ticks;
 private void RequireWorld(){if(!Fresh())throw new InvalidOperationException("请先连接游戏并进入自己的领地，等待实时区域信息。");}
 private void SetDocument(LayoutDocument d)
 {
  LayoutPlanner.Validate(d);document=d;token="";prepared="";quote=null;ApplyButton.IsEnabled=false;CostGrid.ItemsSource=null;Set(TitleText,d.Name+" · "+d.Objects.Length+" 处");
  if(world!=null&&d.Objects.Length>0)ChunkBox.SelectedValue=d.Objects[0].ChunkId;
  PreviewCosts();Draw();Set(MessageText,"布局已载入；预检通过后才会开放购买。");
 }
 private void PreviewCosts(){if(document==null||world==null)return;quote=LayoutPlanner.Quote(world,document);CostGrid.ItemsSource=quote.Costs;Set(TotalText,"合计："+(quote.Costs.Length==0?"无需购买":string.Join("、",quote.Costs.Select(v=>v.Name+" "+v.Count)))+(quote.Costs.Any(v=>v.Missing>0)?"（有缺口）":""));Set(SummaryText,$"本布局 {document.Objects.Length} 处\n复用 {quote.Existing} 处；移动 {quote.Moved} 处；新建 {quote.Purchased} 处\n"+(quote.Costs.Any(v=>v.Missing>0)?"货币或材料尚有缺口。":"费用已核对，尚需检查实际占地。"));}
 private void TemplateClick(object sender,RoutedEventArgs e)=>Attempt(()=>{RequireWorld();if(ChunkBox.SelectedValue is not int id)throw new InvalidOperationException("请选择已解锁区域。");SetDocument(LayoutPlanner.Template(world!,id,TemplateBox.SelectedIndex==0?3:TemplateBox.SelectedIndex==1?2:1));});
 private void LoadClick(object sender,RoutedEventArgs e)=>Attempt(()=>{var dialog=new OpenFileDialog{Title=ui.Language.Text("载入领地布局"),Filter=ui.Language.Text("领地布局 (*.json)|*.json")};if(dialog.ShowDialog(this)!=true)return;if(new FileInfo(dialog.FileName).Length>2000000)throw new InvalidOperationException("布局文件不能超过 2 MB。");var d=JsonSerializer.Deserialize<LayoutDocument>(File.ReadAllText(dialog.FileName))??throw new InvalidOperationException("布局文件为空。");SetDocument(d);});
 private void SaveDocument(LayoutDocument d){var dialog=new SaveFileDialog{Title=ui.Language.Text("保存领地布局"),Filter=ui.Language.Text("领地布局 (*.json)|*.json"),FileName="territory-layout.json"};if(dialog.ShowDialog(this)==true){File.WriteAllText(dialog.FileName,JsonSerializer.Serialize(d,new JsonSerializerOptions{WriteIndented=true}));Set(MessageText,"布局已保存。");}}
 private void SaveClick(object sender,RoutedEventArgs e)=>Attempt(()=>{if(document==null)throw new InvalidOperationException("请先生成或载入布局。");SaveDocument(document);});
 private void ExportCurrentClick(object sender,RoutedEventArgs e)=>Attempt(()=>{RequireWorld();var d=new LayoutDocument{WorldId=world!.WorldId,Name="我的领地布局",Objects=world.Objects.Where(v=>world.Catalog.Any(i=>i.Id==v.ObjectId)).ToArray()};if(d.Objects.Length==0)throw new InvalidOperationException("当前没有可导出的建筑或装饰。");SaveDocument(d);});
 private void Begin(string operation)
 {
  RequireWorld();if(document==null)throw new InvalidOperationException("请先生成或载入布局。");if(link.Enabled)throw new InvalidOperationException("已有操作执行中，请先暂停。");
  quote=LayoutPlanner.Quote(world!,document);if(operation=="apply"&&(prepared.Length==0||quote.Signature!=prepared||quote.Costs.Any(v=>v.Missing>0)))throw new InvalidOperationException("布局或费用已变化，请重新预检。");
  token=Guid.NewGuid().ToString("N");TerritoryJson.Write(System.IO.Path.Combine(root,"layout-request.json"),new LayoutRequest{Token=token,Account=world!.Account,Operation=operation,Document=document,QuoteSignature=prepared});
  link.Start(world.ProcessId,token);ApplyButton.IsEnabled=false;Set(MessageText,operation=="preview"?"正在检查游戏中的实际占地；本步骤不扣费。":"正在按已显示的费用清单购买并导入。");
  if(operation=="apply")prepared="";Refresh();
 }
 private void PreviewClick(object sender,RoutedEventArgs e)=>Attempt(()=>Begin("preview"));
 private void ApplyClick(object sender,RoutedEventArgs e)=>Attempt(()=>Begin("apply"));
 private void PauseClick(object sender,RoutedEventArgs e)=>Attempt(()=>{link.Stop();prepared="";Set(MessageText,"已暂停。已提交的摆放等待确认；再次预检后可接续。");Refresh();});
 private void Refresh()
 {
  var next=TerritoryJson.Read<LayoutWorld>(System.IO.Path.Combine(root,"layout-world.json"));
  if(next!=null)
  {
   bool changed=world?.Account!=next.Account||world?.WorldId!=next.WorldId;world=next;
   string ids=string.Join(",",world.Chunks.Select(v=>v.Id));string old=ChunkBox.ItemsSource is LayoutChunk[] chunks?string.Join(",",chunks.Select(v=>v.Id)):"";
   if(ids!=old||changed){refreshing=true;var id=ChunkBox.SelectedValue;ChunkBox.ItemsSource=world.Chunks;ChunkBox.SelectedValue=id??world.Chunks.FirstOrDefault()?.Id;refreshing=false;Draw();}
   Set(AccountText,$"当前账号 ID：{world.Account}\n领地类型：{world.WorldId} · 已解锁 {world.Chunks.Length} 个区域");
   if(changed)prepared="";
  }
  var s=TerritoryJson.Read<LayoutStatus>(System.IO.Path.Combine(root,"layout-status.json"));
  if(s!=null&&s.Token==token&&token.Length>0)
  {
   Set(MessageText,s.Message);MessageText.Foreground=s.State=="error"?Brushes.Firebrick:(Brush)FindResource("TextBrush");Progress.Maximum=Math.Max(1,s.Total);Progress.Value=s.Done;
   if(s.Quote!=null){quote=s.Quote;CostGrid.ItemsSource=quote.Costs;Set(TotalText,"合计："+(quote.Costs.Length==0?"无需购买":string.Join("、",quote.Costs.Select(v=>v.Name+" "+v.Count)))+(quote.Costs.Any(v=>v.Missing>0)?"（有缺口）":""));Set(SummaryText,$"复用 {quote.Existing} 处；移动 {quote.Moved} 处；新建 {quote.Purchased} 处\n已检查／完成 {s.Done} / {s.Total}");}
   if(s.State is "ready" or "complete" or "error" or "paused")
   {if(link.Enabled)link.Stop();prepared=s.State=="ready"?s.Quote?.Signature??"":"";}
  }
  bool idle=!link.Enabled;PreviewButton.IsEnabled=idle&&Fresh()&&document!=null;ApplyButton.IsEnabled=idle&&Fresh()&&prepared.Length>0&&quote!=null&&!quote.Costs.Any(v=>v.Missing>0);
  PauseButton.IsEnabled=!idle;TemplateButton.IsEnabled=idle&&Fresh();LoadButton.IsEnabled=idle;ChunkBox.IsEnabled=idle;TemplateBox.IsEnabled=idle;ExportCurrentButton.IsEnabled=Fresh()&&idle;
 }
 private void ChunkChanged(object sender,SelectionChangedEventArgs e){if(!refreshing)Draw();}
 private void Draw()
 {
  if(Board==null)return;Board.Children.Clear();int chunk=ChunkBox.SelectedValue is int c?c:-1;
  for(int y=0;y<10;y++)for(int x=0;x<10;x++)
  {
   var target=document?.Objects.Where(v=>v.ChunkId==chunk&&v.X==x&&v.Y==y).ToArray()??Array.Empty<LayoutEntry>();var existing=world?.Objects.Where(v=>v.ChunkId==chunk&&v.X==x&&v.Y==y).ToArray()??Array.Empty<LayoutEntry>();
   string Names(LayoutEntry[] values)=>string.Join("、",values.Select(v=>world?.Catalog.FirstOrDefault(i=>i.Id==v.ObjectId)?.Name??v.ObjectId.ToString()));
   var cell=new Border{Width=29,Height=29,CornerRadius=new CornerRadius(3),Background=new SolidColorBrush((Color)ColorConverter.ConvertFromString(target.Length>0?"#D5E7FF":existing.Length>0?"#DEE2E8":"#FFFFFF")),BorderBrush=new SolidColorBrush((Color)ColorConverter.ConvertFromString(target.Length>0?"#0071E3":"#E3E7ED")),BorderThickness=new Thickness(1),ToolTip=ui.Language.Text($"({x}, {y})\n目标：{Names(target)}\n现有：{Names(existing)}")};
   if(target.Length>0||existing.Length>0)cell.Child=new TextBlock{Text=target.Length>0?"●":"·",HorizontalAlignment=HorizontalAlignment.Center,VerticalAlignment=VerticalAlignment.Center,Foreground=(Brush)FindResource("PrimaryBrush")};
   Canvas.SetLeft(cell,x*32);Canvas.SetTop(cell,(9-y)*32);Board.Children.Add(cell);
  }
 }
 internal void SmokeFlow(Action<bool,string> check)
 {
  Begin("preview");var request=TerritoryJson.Read<LayoutRequest>(System.IO.Path.Combine(root,"layout-request.json"));
  check(link.Enabled&&request?.Account==world!.Account&&request.Operation=="preview","预检指令绑定当前账号和租约");
  var result=new LayoutStatus{Token=token,State="ready",Quote=LayoutPlanner.Quote(world!,document!),Done=document!.Objects.Length,Total=document.Objects.Length,Message="预检通过"};
  TerritoryJson.Write(System.IO.Path.Combine(root,"layout-status.json"),result);Refresh();check(!link.Enabled&&ApplyButton.IsEnabled,"预检完成停止控制且开放执行");
  SetDocument(document!);Refresh();check(!ApplyButton.IsEnabled,"换布局后旧预检回执不能重新开放购买");
  Begin("preview");result.Token=token;TerritoryJson.Write(System.IO.Path.Combine(root,"layout-status.json"),result);Refresh();Begin("apply");
  request=TerritoryJson.Read<LayoutRequest>(System.IO.Path.Combine(root,"layout-request.json"));check(request?.Operation=="apply"&&request.QuoteSignature==result.Quote.Signature&&link.Enabled,"执行指令携带已预检费用身份");
  result.Token=token;result.State="complete";TerritoryJson.Write(System.IO.Path.Combine(root,"layout-status.json"),result);Refresh();check(!link.Enabled&&!ApplyButton.IsEnabled,"导入完成不再次执行付款");
 }
 internal void SmokeSetup(LayoutWorld sample,LayoutDocument d){world=sample;ChunkBox.ItemsSource=world.Chunks;ChunkBox.SelectedValue=world.Chunks[0].Id;SetDocument(d);}
}
