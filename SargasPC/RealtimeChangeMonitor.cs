using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SargasPC {
 // Bounded post-write event monitor. It reports event loss instead of implying
 // that FileSystemWatcher blocks execution or observes files while SARGON is down.
 public sealed class RealtimeChangeMonitor : IDisposable {
  const int DefaultCapacity=512;
  readonly object sync=new object();
  readonly string[] roots,exclusions;
  readonly Dictionary<string,long> pending=new Dictionary<string,long>(StringComparer.OrdinalIgnoreCase);
  readonly List<FileSystemWatcher> watchers=new List<FileSystemWatcher>();
  readonly BulkChangeDetector bulk;
  readonly int capacity;
  readonly long settleTicks;
  int dropped,scanned,unverified;
  bool coverageLost,disposed;
  long lastEventAt;
  public RealtimeChangeMonitor(string[] scanRoots,string[] excludedRoots,bool startNative=true,int queueCapacity=DefaultCapacity,int settleSeconds=4,int bulkThreshold=100,int bulkWindowSeconds=15,int bulkAlertSeconds=120) {
   if(scanRoots==null||scanRoots.Length<1||scanRoots.Length>16)throw new IOException("Failų pokyčių stebėjimui reikia nuo 1 iki 16 diskų šaknų.");
   if(queueCapacity<1||queueCapacity>4096||settleSeconds<1||settleSeconds>30)throw new ArgumentOutOfRangeException("queueCapacity");
   roots=scanRoots.Select(Normalize).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
   exclusions=(excludedRoots??new string[0]).Where(x=>!string.IsNullOrWhiteSpace(x)).Select(Normalize).ToArray();
   capacity=queueCapacity;settleTicks=TimeSpan.FromSeconds(settleSeconds).Ticks;bulk=new BulkChangeDetector(bulkThreshold,bulkWindowSeconds,bulkAlertSeconds);
   if(startNative)StartWatchers();
  }
  static string Normalize(string value){if(string.IsNullOrWhiteSpace(value)||!Path.IsPathRooted(value)||value.Length>512||value.Any(c=>c<' '))throw new IOException("Netinkama stebėjimo šaknis.");return Path.GetFullPath(value);}
  static bool Under(string path,string root){if(string.Equals(path,root,StringComparison.OrdinalIgnoreCase))return true;string prefix=root.TrimEnd(Path.DirectorySeparatorChar,Path.AltDirectorySeparatorChar)+Path.DirectorySeparatorChar;return path.StartsWith(prefix,StringComparison.OrdinalIgnoreCase);}
  void StartWatchers(){
   foreach(string root in roots){
    try{
     FolderMonitor.NoLinks(root);if(!Directory.Exists(root)){MarkLost();continue;}
     var watcher=new FileSystemWatcher(root){IncludeSubdirectories=true,NotifyFilter=NotifyFilters.FileName|NotifyFilters.Size|NotifyFilters.LastWrite|NotifyFilters.CreationTime,InternalBufferSize=8192};
     watcher.Created+=OnChanged;watcher.Changed+=OnChanged;watcher.Deleted+=OnChanged;watcher.Renamed+=OnRenamed;watcher.Error+=OnError;
     lock(sync){if(disposed){watcher.Dispose();return;}watchers.Add(watcher);}
     watcher.EnableRaisingEvents=true;
    }catch(Exception e) when(e is IOException||e is UnauthorizedAccessException||e is ArgumentException||e is InvalidOperationException){MarkLost();}
   }
  }
  void OnChanged(object sender,FileSystemEventArgs e){QueueEvent(e.FullPath);}
  void OnRenamed(object sender,RenamedEventArgs e){QueueEvent(e.FullPath);}
  void OnError(object sender,ErrorEventArgs e){MarkLost();}
  void QueueEvent(string path){try{if(Directory.Exists(path))return;Enqueue(path,DateTime.UtcNow.Ticks);}catch(Exception e) when(e is IOException||e is UnauthorizedAccessException||e is ArgumentException||e is NotSupportedException){MarkDropped();}}
  void MarkLost(){lock(sync){coverageLost=true;}}
  void MarkDropped(){lock(sync){dropped=Math.Min(1000000,dropped+1);coverageLost=true;}}
  public void Enqueue(string path,long now){
   try{
    if(now<=0)throw new IOException("Įvykio laikas nepatvirtintas.");string full=Path.GetFullPath(path);if(full.Length>1024||!roots.Any(x=>Under(full,x))||exclusions.Any(x=>Under(full,x)))return;
    lock(sync){if(disposed)return;bulk.Observe(full,now);if(!pending.ContainsKey(full)&&pending.Count>=capacity){dropped=Math.Min(1000000,dropped+1);coverageLost=true;lastEventAt=Math.Max(lastEventAt,now);return;}pending[full]=now;lastEventAt=Math.Max(lastEventAt,now);}
   }catch(Exception e) when(e is IOException||e is UnauthorizedAccessException||e is ArgumentException||e is NotSupportedException){MarkDropped();}
  }
  public string Take(long now){lock(sync){if(disposed||now<=0)return null;var item=pending.Where(x=>x.Value<=now&&now-x.Value>=settleTicks).OrderBy(x=>x.Value).FirstOrDefault();if(string.IsNullOrEmpty(item.Key))return null;pending.Remove(item.Key);return item.Key;}}
  public void MarkScanned(){lock(sync){scanned=Math.Min(1000000,scanned+1);}}
  public void MarkUnverified(){lock(sync){unverified=Math.Min(1000000,unverified+1);}}
  public int ActiveRoots{get{lock(sync)return watchers.Count;}}
  public int PendingCount{get{lock(sync)return pending.Count;}}
  public int Dropped{get{lock(sync)return dropped;}}
  public int Scanned{get{lock(sync)return scanned;}}
  public int Unverified{get{lock(sync)return unverified;}}
  public long LastEventAt{get{lock(sync)return lastEventAt;}}
  public bool BulkChangeAlertActive{get{lock(sync)return bulk.IsActive(DateTime.UtcNow.Ticks);}}
  public int BulkChangeBursts{get{lock(sync)return bulk.BurstCount;}}
  public int BulkChangeDistinctPaths{get{lock(sync)return bulk.RecentDistinctPaths(DateTime.UtcNow.Ticks);}}
  public long BulkChangeAlertAt{get{lock(sync)return bulk.LastBurstAt;}}
  public bool CoverageLost{get{lock(sync)return coverageLost;}}
  public bool Ready{get{lock(sync)return !disposed&&!coverageLost&&watchers.Count==roots.Length;}}
  public string Summary{get{lock(sync)return Ready?"Aktyvūs failų pokyčių įvykiai pasirinktose diskų šaknyse; tai patikra po įrašymo, ne vykdymo blokavimas.":"DĖMESIO: failų pokyčių įvykių aprėptis nepatvirtinta; naudokite periodinę / rankinę patikrą.";}}
  public string[] Roots{get{return (string[])roots.Clone();}}
  public void Dispose(){lock(sync){if(disposed)return;disposed=true;pending.Clear();foreach(var watcher in watchers){try{watcher.EnableRaisingEvents=false;}catch{}watcher.Dispose();}watchers.Clear();}}
 }
}
