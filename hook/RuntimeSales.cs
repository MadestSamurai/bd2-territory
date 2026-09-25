using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Proto.Net;
using Proto.Design.common;
using B=BD2Territory.Runtime.TerritoryBindings;
namespace BD2Territory.Runtime
{
 internal sealed partial class RuntimeEngine
 {
  private SalesProgress sales;private string salesPath="";private long lastSaleScan;
  private bool SalesBusy()=>sales!=null&&sales.Pending;
  private ItemDBInfo[] SaleInventory()=>((IEnumerable<ItemDBInfo>)B.Read("Inventory.Items",null)??throw new InvalidOperationException("等待领地库存同步")).Select(x=>x.Clone()).ToArray();
  private long SaleCurrency()=>Convert.ToInt64(B.Invoke("Inventory.Currency",B.EnumObject("CurrencyKind","LocalMileage")));
  private void LoadSales(string key)
  {
   salesPath=Path.Combine(LocalStorage.DataRoot,"sales-"+key+".json");sales=ReadLayoutFile<SalesProgress>(salesPath)??new SalesProgress{Account=key};SurplusSales.Validate(sales,key);
  }
  private void SettleSales()
  {
   if(!SalesBusy())return;var reply=network.LastSaleReply;if(reply==null||reply.Token!=sales.Token)return;
   var user=(UserDBInfo)B.Read("Account.User",null);if(user==null||user.OwnerIndex.ToString()!=sales.Account)throw new InvalidOperationException("售卖确认期间账号发生变化，已保留记录");
   SurplusSales.Confirm(sales,reply,SaleInventory().ToDictionary(x=>x.InvenIndex,x=>x.Count),SaleCurrency());
   LocalStorage.WriteJsonAtomically(salesPath,sales);lastCrops=0;lastCatalog=0;
   LocalStorage.Log("Territory sale "+sales.State+" token="+sales.Token+" items="+sales.Lines.Sum(x=>(long)x.Count)+" currency="+reply.Reward+" threshold="+sales.Threshold);
  }
  private bool SalesTick(TerritorySnapshot s,TerritoryControl c,long now)
  {
   if(sales==null)return false;
   if(sales.Pending){StopMotion();s.Reason="等待领地售卖确认";if(now-sales.SubmittedTicks>TimeSpan.FromSeconds(30).Ticks)throw new InvalidOperationException("售卖结果未知，已保留记录；不会重复卖出，请核对库存和领地币");return true;}
   if(!c.AutoSell||now-lastSaleScan<TimeSpan.FromSeconds(5).Ticks)return false;lastSaleScan=now;
   var rows=(IEnumerable<LifeSellItemTable>)B.Invoke("Sales.Rows",1);
   if(rows==null)throw new InvalidOperationException("等待领地售卖表同步");
   var byItem=rows.Where(x=>x.GroupId==1&&x.ItemType==66&&x.PriceType==64&&x.PriceId==0&&x.ItemCount==1&&x.PriceCount>0).GroupBy(x=>x.ItemId).Where(g=>g.Count()==1).ToDictionary(g=>g.Key,g=>g.Single());
   var stocks=SaleInventory().Select(x=>{
    LifeSellItemTable row;byItem.TryGetValue(x.Id,out row);var item=(LifeItemTable)B.Invoke("Tables.ItemById",x.Id);
    return new SaleStock{Index=x.InvenIndex,Item=x.Id,Count=x.Count,Locked=x.KeepFlag!=0||x.IsDisableSlot,Kind=x.Type,Group=row==null?0:row.GroupId,Row=row==null?0:row.Id,UnitCount=row==null?0:row.ItemCount,Price=row==null?0:row.PriceCount,Eligible=row!=null&&item!=null&&item.Type>=1&&item.Type<=3};
   }).ToArray();
   var lines=SurplusSales.Plan(stocks,c.SellThreshold);if(lines.Length==0)return false;
   // No gathering/cooking/planting is active here. Recheck the lease before any irreversible request.
   var live=control;if(live==null||!live.Valid(DateTime.UtcNow.Ticks,pid)||!live.AutoSell||live.OwnerId!=c.OwnerId||live.SellThreshold!=c.SellThreshold)return false;
   StopMotion();sales=new SalesProgress{Account=account,Token=Guid.NewGuid().ToString("N"),State="pending",Threshold=c.SellThreshold,SubmittedTicks=now,CurrencyBefore=SaleCurrency(),Lines=lines,ConfirmedItems=sales.ConfirmedItems,ConfirmedCurrency=sales.ConfirmedCurrency};
   SurplusSales.Validate(sales,account);LocalStorage.WriteJsonAtomically(salesPath,sales);network.ArmSales(sales);lastInput=now;
   var request=lines.Select(x=>new SellItemInfo{InvenIndex=x.Index,GroupId=x.Group,Id=x.Row,SellCount=x.Count}).ToList();
   LocalStorage.Log("Territory sale submitted token="+sales.Token+" lines="+lines.Length+" items="+lines.Sum(x=>(long)x.Count)+" threshold="+sales.Threshold);
   B.Invoke("Sales.Send",request,null);s.Reason="正在售卖超过保留数量的领地物品";s.Target="";return true;
  }
 }
}
