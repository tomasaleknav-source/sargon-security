using System;
using System.Collections.Generic;
using System.Linq;

namespace SargasPC {
 // Advisory-only signal for a sudden burst of distinct file changes.
 // It cannot attribute events to a process or identify ransomware.
 public sealed class BulkChangeDetector {
  readonly Dictionary<string,long> recent=new Dictionary<string,long>(StringComparer.OrdinalIgnoreCase);
  readonly int threshold;
  readonly long windowTicks,alertTicks;
  long lastObserved,alertUntil,lastBurstAt;
  int bursts,alertedDistinct;
  public BulkChangeDetector(int threshold=100,int windowSeconds=15,int alertSeconds=120){
   if(threshold<2||threshold>4096||windowSeconds<1||windowSeconds>300||alertSeconds<1||alertSeconds>1800)throw new ArgumentOutOfRangeException("threshold");
   this.threshold=threshold;windowTicks=TimeSpan.FromSeconds(windowSeconds).Ticks;alertTicks=TimeSpan.FromSeconds(alertSeconds).Ticks;
  }
  void Prune(long now){foreach(string path in recent.Where(x=>x.Value<=now&&now-x.Value>windowTicks).Select(x=>x.Key).ToArray())recent.Remove(path);}
  public bool Observe(string path,long now){
   if(string.IsNullOrWhiteSpace(path)||now<=0)throw new ArgumentException("File event is invalid.");
   if(lastObserved>0&&now<lastObserved){recent.Clear();alertUntil=0;alertedDistinct=0;}
   lastObserved=now;Prune(now);
   // Once the threshold is reached, stop retaining attacker-controlled path
   // strings until the alert window expires. The count is only "at least N".
   if(now<alertUntil)return false;
   if(!recent.ContainsKey(path)&&recent.Count>=threshold)return false;
   recent[path]=now;
   if(recent.Count<threshold||now<alertUntil)return false;
   bursts=Math.Min(1000000,bursts+1);lastBurstAt=now;alertedDistinct=recent.Count;alertUntil=now>long.MaxValue-alertTicks?long.MaxValue:now+alertTicks;recent.Clear();return true;
  }
  public bool IsActive(long now){return now>0&&alertUntil>now;}
  public int RecentDistinctPaths(long now){if(IsActive(now))return alertedDistinct;if(now>0)Prune(now);return recent.Count;}
  public int BurstCount{get{return bursts;}}
  public long LastBurstAt{get{return lastBurstAt;}}
 }
}
