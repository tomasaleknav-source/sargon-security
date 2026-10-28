using System;using System.IO;using System.Linq;using System.Text;using System.Security.Cryptography;using System.Collections.Generic;using System.Threading;
namespace SargasPC {
 public sealed class LoadGate {
  bool held;int low;
  public string Evaluate(bool pauseOnBattery,bool? ac,bool pauseWhenBusy,double? cpu){
   if(pauseOnBattery&&ac!=true)return ac==false?"PAUSED_BATTERY":"PAUSED_POWER_UNKNOWN";
   if(!pauseWhenBusy){held=false;low=0;return "";}
   if(!cpu.HasValue||double.IsNaN(cpu.Value)||cpu<0||cpu>100)return "PAUSED_LOAD_UNKNOWN";
   if(cpu>=80){held=true;low=0;return "PAUSED_SYSTEM_BUSY";}
   if(held){if(cpu<=60)low++;else low=0;if(low<2)return "PAUSED_SYSTEM_BUSY";held=false;low=0;}return "";
  }
 }
 public sealed class ReadLeaseSet:IDisposable {public readonly List<FileStream> Streams=new List<FileStream>();public void Dispose(){foreach(var s in Streams)s.Dispose();Streams.Clear();}}
 public sealed class CleanCacheEntry {public string Directory,Generation,Fingerprint;public long VerifiedAt;}
 public sealed class CleanDirectoryCache {
  readonly string file;List<CleanCacheEntry> entries=new List<CleanCacheEntry>();
  public CleanDirectoryCache(string path){file=path;FolderMonitor.NoLinks(Path.GetDirectoryName(path));if(File.Exists(path)){FolderMonitor.NoLinks(path);try{var read=Data.Read<CleanCacheEntry[]>(path,1048576);if(read!=null&&read.Length<=512&&read.All(x=>x!=null&&x.Directory!=null&&x.Directory.Length<=512&&Data.Hash(x.Generation)&&Data.Hash(x.Fingerprint)&&x.VerifiedAt>0))entries=read.ToList();}catch(Exception e) when(e is IOException||e is ArgumentException||e is InvalidOperationException||e is MissingMethodException){/* Bad cache is discarded, never used as a clean verdict. */}}}
  public bool Hit(string directory,string generation,string fingerprint,long now){return fingerprint!=null&&generation!=null&&now>0&&entries.Any(x=>x.Directory==directory&&x.Generation==generation&&x.Fingerprint==fingerprint&&x.VerifiedAt<=now&&now-x.VerifiedAt<TimeSpan.FromDays(7).Ticks);}
  public void Commit(string directory,string generation,string before,string after,bool completeClean,long now){if(!completeClean||before==null||!Data.Hash(before)||before!=after||!Data.Hash(generation)||now<=0)return;var updated=entries.Where(x=>x.Directory!=directory).Concat(new[]{new CleanCacheEntry{Directory=directory,Generation=generation,Fingerprint=after,VerifiedAt=now}}).OrderByDescending(x=>x.VerifiedAt).Take(512).ToList();string json=Data.Json().Serialize(updated);if(Encoding.UTF8.GetByteCount(json)>1048576)return;Data.Atomic(file,json);entries=updated;}
  public static string Fingerprint(string directory,Action check,ICollection<FileStream> leases=null){
   try{FolderMonitor.NoLinks(directory);string[] names=Directory.EnumerateFiles(directory,"*",SearchOption.TopDirectoryOnly).Take(257).OrderBy(x=>x,StringComparer.Ordinal).ToArray();if(names.Length>256)return null;long total=0;var text=new StringBuilder();text.Append(directory.Length).Append(':').Append(directory).Append('\n');
    foreach(string name in names){check();if(name.Any(c=>c<' '))return null;FolderMonitor.NoLinks(name);var info=new FileInfo(name);long size=info.Length,ticks=info.LastWriteTimeUtc.Ticks;if(size>33554432||(total+=size)>67108864)return null;string digest;
     var input=new FileStream(name,FileMode.Open,FileAccess.Read,FileShare.Read);if(leases!=null)leases.Add(input);try{using(var sha=SHA256.Create()){byte[] buffer=new byte[65536];int n;while((n=input.Read(buffer,0,buffer.Length))>0){check();sha.TransformBlock(buffer,0,n,null,0);Thread.Sleep(1);}sha.TransformFinalBlock(new byte[0],0,0);digest=Data.Hex(sha.Hash);}}finally{if(leases==null)input.Dispose();}
     info.Refresh();FolderMonitor.NoLinks(name);if(!info.Exists||info.Length!=size||info.LastWriteTimeUtc.Ticks!=ticks)return null;text.Append(name.Length).Append(':').Append(name).Append('|').Append(size).Append('|').Append(digest).Append('\n');
    }
    FolderMonitor.NoLinks(directory);string[] after=Directory.EnumerateFiles(directory,"*",SearchOption.TopDirectoryOnly).Take(257).OrderBy(x=>x,StringComparer.Ordinal).ToArray();if(!names.SequenceEqual(after,StringComparer.Ordinal))return null;return Data.Digest(Encoding.UTF8.GetBytes(text.ToString()));
   }catch(IOException){return null;}catch(UnauthorizedAccessException){return null;}
  }
  public static string Generation(string db,string engineManifest,Action check){try{EngineSafety.ValidateDatabaseFiles(db);FolderMonitor.NoLinks(engineManifest);var names=Directory.EnumerateFiles(db).Where(x=>x.EndsWith(".cvd",StringComparison.OrdinalIgnoreCase)||x.EndsWith(".cld",StringComparison.OrdinalIgnoreCase)).Take(17).OrderBy(x=>x,StringComparer.Ordinal).Concat(new[]{engineManifest}).ToArray();if(names.Length>17)return null;long total=0;var text=new StringBuilder();foreach(string name in names){check();FolderMonitor.NoLinks(name);long size=new FileInfo(name).Length;if((total+=size)>2147483648L)return null;using(var input=File.OpenRead(name))using(var sha=SHA256.Create()){byte[] buffer=new byte[65536];int n;while((n=input.Read(buffer,0,buffer.Length))>0){check();sha.TransformBlock(buffer,0,n,null,0);Thread.Sleep(1);}sha.TransformFinalBlock(new byte[0],0,0);text.Append(Path.GetFileName(name)).Append('|').Append(Data.Hex(sha.Hash)).Append('\n');}}return Data.Digest(Encoding.UTF8.GetBytes(text.ToString()));}catch(IOException){return null;}catch(UnauthorizedAccessException){return null;}}
 }
}
