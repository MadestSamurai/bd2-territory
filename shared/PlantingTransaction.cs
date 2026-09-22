using System;
using System.Linq;
namespace BD2Territory
{
 public sealed class PlantingCell
 {
  public string Key{get;set;}="";public int Seed{get;set;}public int Cost{get;set;}
  public int CurrencyType{get;set;}public int CurrencyId{get;set;}
 }
 public sealed class PlantingReply
 {
  public string Token{get;set;}="";public string[] Keys{get;set;}=new string[0];public int Seed{get;set;}public int Cost{get;set;}
  public bool RequestMatches{get;set;}public bool Accepted{get;set;}public bool Rejected{get;set;}public string Error{get;set;}="";
 }
 public static class PlantingTransaction
 {
  public static bool ValidKeys(string[] keys)=>keys!=null&&keys.Length==RecipeBatchPlanner.BatchSize&&keys.All(k=>!string.IsNullOrEmpty(k))&&keys.Distinct(StringComparer.Ordinal).Count()==RecipeBatchPlanner.BatchSize;
  public static bool SameKeys(string[] left,string[] right)=>ValidKeys(left)&&ValidKeys(right)&&left.OrderBy(k=>k,StringComparer.Ordinal).SequenceEqual(right.OrderBy(k=>k,StringComparer.Ordinal));
  // Persist all 100 distinct field identities and the total charge before the one native confirmation.
  public static RecipeBatchProgress Begin(RecipeBatchProgress state,string[] keys,int unitCost,string owner,long budget,string token)
  {
   if(state==null||state.Schema!=3||state.RecipeId!=3||string.IsNullOrEmpty(state.Account)||state.SeedId<=0||state.Planted!=0||!string.IsNullOrEmpty(state.PendingToken))throw new InvalidOperationException("播种进度尚未就绪");
   if(!ValidKeys(keys))throw new InvalidOperationException("一次批量播种必须包含 100 块不同的空田");
   if(string.IsNullOrEmpty(owner)||string.IsNullOrEmpty(token)||unitCost<=0||unitCost>int.MaxValue/RecipeBatchPlanner.BatchSize||budget<0)throw new InvalidOperationException("播种确认缺少身份或费用");
   int cost=unitCost*RecipeBatchPlanner.BatchSize;
   var next=state.Copy();if(next.BudgetOwner!=owner){next.BudgetOwner=owner;next.Spent=0;}
   if(budget>0&&(next.Spent>budget||cost>budget-next.Spent))throw new InvalidOperationException("剩余预算不足以一次播种 100 个");
   next.PendingKeys=keys.OrderBy(k=>k,StringComparer.Ordinal).ToArray();next.PendingSeed=next.SeedId;next.PendingToken=token;next.PendingCost=cost;next.PendingOwner=owner;return next;
  }
  public static bool MatchesRequest(RecipeBatchProgress intent,PlantingCell[] cells)
  {
   return intent!=null&&ValidKeys(intent.PendingKeys)&&intent.PendingCost>0&&intent.PendingCost%RecipeBatchPlanner.BatchSize==0&&cells!=null&&cells.Length==RecipeBatchPlanner.BatchSize
    &&cells.All(c=>c!=null&&c.Seed==intent.PendingSeed&&c.CurrencyType==64&&c.CurrencyId==0&&c.Cost==intent.PendingCost/RecipeBatchPlanner.BatchSize)
    &&SameKeys(intent.PendingKeys,cells.Select(c=>c.Key).ToArray());
  }
  public static RecipeBatchProgress Settle(RecipeBatchProgress state,PlantingReply reply)
  {
   if(state==null)throw new ArgumentNullException(nameof(state));
   if(reply==null||string.IsNullOrEmpty(reply.Token)||reply.Token!=state.PendingToken)return state;
   if(!reply.RequestMatches||!SameKeys(reply.Keys,state.PendingKeys)||reply.Seed!=state.PendingSeed||reply.Cost!=state.PendingCost)throw new InvalidOperationException("整批播种请求与已记录的 100 格确认不一致");
   if(!reply.Accepted&&!reply.Rejected)throw new InvalidOperationException("整批播种结果未知，保留记录等待世界数据核对");
   var next=state.Copy();
   if(reply.Accepted)
   {
    if(reply.Rejected)throw new InvalidOperationException("播种回执状态冲突");
    RecipeBatchPlanner.Confirm(next,reply.Seed,reply.Keys.Length,reply.Token);
    if(next.BudgetOwner==next.PendingOwner)next.Spent=checked(next.Spent+next.PendingCost);
   }
   next.PendingKeys=new string[0];next.PendingSeed=0;next.PendingToken="";next.PendingCost=0;next.PendingOwner="";return next;
  }
 }
}
