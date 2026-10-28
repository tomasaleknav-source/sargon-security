using System;
using System.IO;
using SargasPC;
class RealtimeMonitorTests {
 static int n;static void C(bool b){if(!b)throw new Exception("realtime monitor check "+n);n++;}
 static void Main(){string root=Path.Combine(Path.GetTempPath(),"sargon-realtime-"+Guid.NewGuid().ToString("N"));string nested=Path.Combine(root,"nested"),excluded=Path.Combine(root,"private"),outside=Path.Combine(Path.GetTempPath(),"sargon-outside-"+Guid.NewGuid().ToString("N")+".dat");Directory.CreateDirectory(nested);Directory.CreateDirectory(excluded);long now=DateTime.UtcNow.Ticks;try{
  using(var m=new RealtimeChangeMonitor(new[]{root},new[]{excluded},false,2,4)){
   string first=Path.Combine(nested,"first.bin"),second=Path.Combine(nested,"second.bin"),third=Path.Combine(nested,"third.bin");
   m.Enqueue(Path.Combine(excluded,"private.bin"),now-TimeSpan.FromSeconds(10).Ticks);m.Enqueue(outside,now-TimeSpan.FromSeconds(10).Ticks);C(m.PendingCount==0&&!m.CoverageLost);
   m.Enqueue(first,now-TimeSpan.FromSeconds(6).Ticks);m.Enqueue(first,now-TimeSpan.FromSeconds(5).Ticks);C(m.PendingCount==1);C(m.Take(now)==first);
   m.Enqueue(second,now-TimeSpan.FromSeconds(6).Ticks);m.Enqueue(third,now-TimeSpan.FromSeconds(6).Ticks);C(m.PendingCount==1&&m.Dropped==1&&m.CoverageLost&&!m.Ready);
   C(m.Take(now)==second);C(m.Take(now)==null);m.MarkScanned();m.MarkUnverified();C(m.Scanned==1&&m.Unverified==1&&m.Summary.Contains("aprėptis nepatvirtinta"));
  }
  using(var m=new RealtimeChangeMonitor(new[]{root},null,false,4,4)){m.Enqueue(Path.Combine(nested,"late.bin"),now-TimeSpan.FromSeconds(10).Ticks);m.Dispose();m.Enqueue(Path.Combine(nested,"after-dispose.bin"),now);C(m.PendingCount==0&&!m.Ready);}
  bool invalidThreshold=false;try{new BulkChangeDetector(1);}catch(ArgumentOutOfRangeException){invalidThreshold=true;}C(invalidThreshold);var detector=new BulkChangeDetector(3,10,20);long t=DateTime.UtcNow.Ticks;C(!detector.Observe("a",t));C(!detector.Observe("a",t+1));C(!detector.Observe("b",t+2));C(detector.Observe("c",t+3));C(detector.IsActive(t+3)&&detector.BurstCount==1&&detector.RecentDistinctPaths(t+3)==3);for(int i=0;i<10000;i++)C(!detector.Observe("during-alert-"+i,t+TimeSpan.FromSeconds(5).Ticks+i));C(detector.RecentDistinctPaths(t+TimeSpan.FromSeconds(5).Ticks+10000)==3&&detector.BurstCount==1);long later=t+TimeSpan.FromSeconds(21).Ticks;C(!detector.IsActive(later));C(!detector.Observe("e",later));C(!detector.Observe("f",later+1));C(detector.Observe("g",later+2));C(detector.BurstCount==2);
  using(var m=new RealtimeChangeMonitor(new[]{root},null,false,8,4,3,10,20)){m.Enqueue(Path.Combine(nested,"bulk-a.bin"),now);m.Enqueue(Path.Combine(nested,"bulk-b.bin"),now+1);m.Enqueue(Path.Combine(nested,"bulk-c.bin"),now+2);C(m.BulkChangeAlertActive&&m.BulkChangeBursts==1&&m.BulkChangeDistinctPaths==3&&m.BulkChangeAlertAt==now+2);}
  var hb=new ServiceHeartbeat{Schema=1,Version=49,RunId=new string('d',32),At=now,State="SCANNING_DIRECTORY",RealtimeBulkChangeAlert=true,RealtimeBulkChangeBursts=1,RealtimeBulkChangeDistinctPaths=100,RealtimeBulkChangeAlertAt=now};C(ServiceHeartbeat.Fresh(hb,now));hb.RealtimeBulkChangeAlertAt=0;C(!ServiceHeartbeat.Fresh(hb,now));hb.RealtimeBulkChangeAlertAt=now+1;C(!ServiceHeartbeat.Fresh(hb,now));hb.RealtimeBulkChangeAlertAt=now;hb.RealtimeBulkChangeDistinctPaths=0;C(!ServiceHeartbeat.Fresh(hb,now));hb.RealtimeBulkChangeDistinctPaths=100;hb.RealtimeBulkChangeBursts=0;C(!ServiceHeartbeat.Fresh(hb,now));hb.RealtimeBulkChangeAlert=false;C(!ServiceHeartbeat.Fresh(hb,now));hb.RealtimeBulkChangeAlertAt=0;C(ServiceHeartbeat.Fresh(hb,now));C(PcScanPolicy.Summary(new ServiceHeartbeat{RealtimeBulkChangeAlert=true,RealtimeBulkChangeDistinctPaths=100}).Contains("DĖMESIO: masinių failų pakeitimų audito signalas"));
  Console.WriteLine("PASS: "+n+" bounded event-queue and advisory bulk-change checks; native FileSystemWatcher coverage is not tested on this host.");
 }finally{Directory.Delete(root,true);}}
}
