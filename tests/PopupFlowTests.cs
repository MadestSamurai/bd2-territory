using System;
using BD2Territory;
public class UIBase
{
 public bool Active=true,Closable=true;public Action Confirmed;public int ConfirmClicks,DirectCloses;
 public bool CanCloseUI()=>Closable;
 public void CloseUI(){Active=false;DirectCloses++;}
 public virtual void OnClickBackButton(){if(Closable){ConfirmClicks++;CloseUI();Confirmed?.Invoke();}}
}
namespace BD2Territory.Runtime
{
 internal sealed partial class RuntimeEngine
 {
  internal int ExercisePopupFlow()
  {
   int checks=0;void Check(bool ok,string label){checks++;if(!ok)throw new Exception("Popup flow: "+label);}
   long now=DateTime.UtcNow.Ticks;var s=new TerritorySnapshot();bool hud=false;
   var broken=new UIBase{Confirmed=()=>hud=true};broken.CloseUI();Check(!hud&&!broken.Active,"old direct close reproduces hidden HUD");
   var level=new UIBase{Confirmed=()=>hud=true};ConfirmTerritoryPopup(level,s,now);
   Check(hud&&!level.Active&&level.ConfirmClicks==1,"production handler uses native confirmation and restores HUD");
   Check(stopCalls==1&&popupProgress.Pending,"movement stopped before confirming");
   var unlock=new UIBase{Confirmed=()=>hud=true};hud=false;
   level=new UIBase{Confirmed=()=>{unlock.Active=true;hud=false;}};ConfirmTerritoryPopup(level,s,now+TimeSpan.TicksPerSecond);
   Check(unlock.Active&&!hud,"native next-unlock flow is preserved");
   unlock.Closable=false;ConfirmTerritoryPopup(unlock,s,now+TimeSpan.FromMilliseconds(1500).Ticks);
   Check(unlock.ConfirmClicks==0&&!hud,"opening animation cannot be skipped");
   unlock.Closable=true;ConfirmTerritoryPopup(unlock,s,now+2*TimeSpan.TicksPerSecond);Check(hud&&!unlock.Active,"second native confirmation restores HUD");
   Check(popupProgress.WaitForSettle(now+TimeSpan.FromMilliseconds(2700).Ticks),"wait for popup transition before moving");
   Check(!popupProgress.WaitForSettle(now+TimeSpan.FromMilliseconds(2800).Ticks),"resume after quiet transition");
   var repeated=new UIBase();ConfirmTerritoryPopup(repeated,s,now+3*TimeSpan.TicksPerSecond);ConfirmTerritoryPopup(repeated,s,now+TimeSpan.FromMilliseconds(3100).Ticks);
   Check(repeated.ConfirmClicks==1,"stale visible popup cannot be clicked every tick");
   return checks;
  }
 }
 internal static class PopupFlowTests{internal static int Run()=>new RuntimeEngine().ExercisePopupFlow();}
}
