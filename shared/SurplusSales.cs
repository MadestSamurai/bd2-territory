using System;
using System.Collections.Generic;
using System.Linq;
namespace BD2Territory
{
 public sealed class SaleStock
 {
  public long Index{get;set;}public int Item{get;set;}public int Count{get;set;}public bool Locked{get;set;}
  public int Kind{get;set;}public int Group{get;set;}public int Row{get;set;}public int UnitCount{get;set;}public int Price{get;set;}
  public bool Eligible{get;set;}
 }
 public sealed class SaleLine
 {
  public long Index{get;set;}public int Item{get;set;}public int Before{get;set;}public int Count{get;set;}
  public int Group{get;set;}public int Row{get;set;}public int Price{get;set;}
 }
 public sealed class SalesProgress
 {
  public int Schema{get;set;}=1;public string Account{get;set;}="";public string Token{get;set;}="";public string State{get;set;}="idle";
  public int Threshold{get;set;}=9900;public long SubmittedTicks{get;set;}public long CurrencyBefore{get;set;}
  public long ConfirmedItems{get;set;}public long ConfirmedCurrency{get;set;}public SaleLine[] Lines{get;set;}=new SaleLine[0];
  public bool Pending=>State=="pending";
 }
 public sealed class SalesReply {public string Token="";public bool Accepted,Rejected,Matches;public long Reward;public string Error="";}
 public static class SurplusSales
 {
  public const int Minimum=100,Maximum=9900;
  public static bool ValidThreshold(int value)=>value>=Minimum&&value<=Maximum;
  public static SaleLine[] Plan(IEnumerable<SaleStock> inventory,int threshold)
  {
   if(!ValidThreshold(threshold))throw new ArgumentOutOfRangeException(nameof(threshold));
   var stocks=inventory.ToArray();
   if(stocks.Any(s=>s.Index<=0||s.Item<=0||s.Count<0)||stocks.Select(s=>s.Index).Distinct().Count()!=stocks.Length)throw new InvalidOperationException("Invalid territory inventory identity");
   var result=new List<SaleLine>();
   foreach(var group in stocks.OrderBy(s=>s.Item).ThenBy(s=>s.Index).GroupBy(s=>s.Item))
   {
    long remaining=Math.Max(0,group.Sum(s=>(long)s.Count)-threshold);
    foreach(var s in group)
    {
     if(remaining==0||result.Count>=32)break;
     // Only native territory consumables sold for territory currency. No buildings, equipment or general inventory.
     if(!s.Eligible||s.Locked||s.Kind!=66||s.Group!=1||s.Row<=0||s.UnitCount!=1||s.Price<=0)continue;
     int count=(int)Math.Min(remaining,s.Count);if(count==0)continue;
     result.Add(new SaleLine{Index=s.Index,Item=s.Item,Before=s.Count,Count=count,Group=s.Group,Row=s.Row,Price=s.Price});remaining-=count;
    }
   }
   if(result.Sum(s=>(long)s.Count*s.Price)>int.MaxValue)throw new InvalidOperationException("Territory sale value exceeds the native transaction range");
   return result.ToArray();
  }
  public static bool Matches(SaleLine[] expected,SaleLine[] actual)=>expected!=null&&actual!=null&&expected.Length>0&&expected.Length==actual.Length&&expected.All(e=>actual.Count(a=>a.Index==e.Index&&a.Group==e.Group&&a.Row==e.Row&&a.Count==e.Count)==1);
  public static void Validate(SalesProgress p,string account)
  {
   if(p.Schema!=1||p.Account!=account||p.ConfirmedItems<0||p.ConfirmedCurrency<0||!new[]{"idle","pending","confirmed","rejected"}.Contains(p.State))throw new InvalidOperationException("Territory sale journal identity mismatch");
   if(p.Pending&&(!ValidThreshold(p.Threshold)||p.SubmittedTicks<=0||p.CurrencyBefore<0||string.IsNullOrEmpty(p.Token)||p.Lines==null||p.Lines.Length<1||p.Lines.Length>32||p.Lines.Any(x=>x.Index<=0||x.Item<=0||x.Group!=1||x.Row<=0||x.Count<=0||x.Count>x.Before||x.Price<=0)||p.Lines.Select(x=>x.Index).Distinct().Count()!=p.Lines.Length))throw new InvalidOperationException("Invalid pending territory sale; refusing to repeat");
  }
  public static void Confirm(SalesProgress p,SalesReply r,IDictionary<long,int> inventory,long currency)
  {
   if(!p.Pending||r.Token!=p.Token||!r.Matches)throw new InvalidOperationException("Territory sale receipt does not match; no repeat sale will be sent");
   if(r.Rejected&&!r.Accepted){p.State="rejected";return;}
   long expected=p.Lines.Sum(x=>(long)x.Count*x.Price);
   if(!r.Accepted||r.Rejected||r.Reward!=expected||currency!=checked(p.CurrencyBefore+expected)||p.Lines.Any(x=>(inventory.TryGetValue(x.Index,out var n)?n:0)!=x.Before-x.Count))throw new InvalidOperationException("Territory sale result is unknown; inventory and currency must be checked before continuing");
   p.ConfirmedItems=checked(p.ConfirmedItems+p.Lines.Sum(x=>(long)x.Count));p.ConfirmedCurrency=checked(p.ConfirmedCurrency+expected);p.State="confirmed";
  }
 }
}
