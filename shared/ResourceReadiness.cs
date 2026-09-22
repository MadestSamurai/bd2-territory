namespace BD2Territory
{
 public static class ResourceReadiness
 {
  public static bool Mature(int status,int finalStage)=>finalStage>0&&status>=finalStage;
  // Strict baseline; reconciliation below handles stale client world caches using native field state.
  public static bool EmptyField(bool known,bool serverCrop,bool occupied,bool liveCrop)=>known&&!serverCrop&&!occupied&&!liveCrop;
 }
 public sealed class FarmEmptyProgress
 {
  private long emptySince;
  public bool Observe(long now,bool known,bool cachedCrop,bool occupied,bool liveCrop,bool nativePreviewAllowed,bool requestPending,bool harvestConfirmed)
  {
   if(!known||occupied||liveCrop||!nativePreviewAllowed||requestPending){emptySince=0;return false;}
   if(emptySince==0)emptySince=now;
   // The normal game preview is authoritative for availability. A cache mismatch additionally needs
   // an accepted harvest or stable agreement from occupancy, live crop and native preview checks.
   return !cachedCrop||harvestConfirmed||now-emptySince>=System.TimeSpan.FromSeconds(2).Ticks;
  }
 }
}
