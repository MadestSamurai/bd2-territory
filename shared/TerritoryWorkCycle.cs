using System;
using System.Collections.Generic;
namespace BD2Territory
{
 public enum TerritoryWorkPhase { Idle, Gathering, Planting, Processing }
 // Finite worksets prevent one newly empty field from interrupting a harvest,
 // and prevent continuous resource respawns from starving the planting pass.
 public sealed class TerritoryWorkCycle
 {
  private readonly HashSet<string> gather=new HashSet<string>(StringComparer.Ordinal);
  private readonly HashSet<string> fields=new HashSet<string>(StringComparer.Ordinal);
  public TerritoryWorkPhase Phase{get;private set;}=TerritoryWorkPhase.Idle;
  public int Remaining=>Phase==TerritoryWorkPhase.Gathering?gather.Count:Phase==TerritoryWorkPhase.Planting?fields.Count:0;
  public bool AcceptsResource(string key)=>Phase==TerritoryWorkPhase.Gathering&&gather.Contains(key);
  public bool AcceptsField(string key)=>Phase==TerritoryWorkPhase.Planting&&fields.Contains(key);
  public TerritoryWorkPhase Advance(IEnumerable<string> collectible,IEnumerable<string> emptyFields,bool actionPending=false)
  {
   // The caller waits for tool/preview/network completion before even enumerating
   // a new schedule. Keep this guard for adapters and future controller reuse.
   if(actionPending)return Phase;
   var resources=new HashSet<string>(collectible,StringComparer.Ordinal);
   var availableFields=new HashSet<string>(emptyFields,StringComparer.Ordinal);
   if(Phase==TerritoryWorkPhase.Idle){gather.UnionWith(resources);Phase=TerritoryWorkPhase.Gathering;}
   if(Phase==TerritoryWorkPhase.Gathering){
    // No longer mature, disabled and cooling-down targets leave this pass only.
    // The runtime retains failure memory and offers them in a later pass.
    gather.IntersectWith(resources);
    if(gather.Count==0){fields.UnionWith(availableFields);Phase=TerritoryWorkPhase.Planting;}
   }
   if(Phase==TerritoryWorkPhase.Planting){fields.IntersectWith(availableFields);if(fields.Count==0)Phase=TerritoryWorkPhase.Processing;}
   return Phase;
  }
  public void Gathered(string key){gather.Remove(key);}
  public void Planted(IEnumerable<string> keys){foreach(var key in keys)fields.Remove(key);}
  public void FinishProcessing(){if(Phase!=TerritoryWorkPhase.Processing)throw new InvalidOperationException("Work pass has not reached processing");Reset();}
  public void Reset(){gather.Clear();fields.Clear();Phase=TerritoryWorkPhase.Idle;}
 }
}
