using System;using System.IO;using System.Linq;using System.Text;
namespace SargasPC {
 // Evidence only. Never deserialize an historical file path into an action target.
 public sealed class AlertEvidence {public string Id;public long At;public string Report;}
 public sealed class AlertStore {
  public readonly string PathName;
  public AlertStore(string root){PathName=Path.Combine(root,"pending-alerts.json");}
  public AlertEvidence[] Read(){
   FolderMonitor.NoLinks(Path.GetDirectoryName(PathName));if(!File.Exists(PathName))return new AlertEvidence[0];FolderMonitor.NoLinks(PathName);
   AlertEvidence[] items;try{items=Data.Read<AlertEvidence[]>(PathName,400000);}catch(Exception e) when(e is ArgumentException||e is InvalidOperationException||e is MissingMethodException||e is InvalidCastException||e is FormatException||e is OverflowException){throw new IOException("Perspėjimų žurnalo formatas nepatvirtintas.",e);}
   if(items==null||items.Length>20||items.Any(x=>x==null||string.IsNullOrEmpty(x.Id)||x.Id.Length>40||x.At<=0||x.At>DateTime.MaxValue.Ticks||x.Report==null||x.Report.Length>4000))throw new IOException("Perspėjimų žurnalas nepatvirtintas; automatinės patikros pristabdytos.");
   return items;
  }
  public bool HasPending(){return Read().Length>0;}
  public void Append(string report){var old=Read();if(old.Length>=20)throw new IOException("Perspėjimų žurnalas pilnas; ankstesni įrašai išsaugoti.");var item=new AlertEvidence{Id=Guid.NewGuid().ToString("N"),At=DateTime.UtcNow.Ticks,Report=ShortReport(report)};string json=Data.Json().Serialize(old.Concat(new[]{item}).ToArray());if(Encoding.UTF8.GetByteCount(json)>400000)throw new IOException("Perspėjimų apimties riba; ankstesni įrašai išsaugoti.");Data.Atomic(PathName,json);}
  private static string ShortReport(string text){string clean=Data.CleanLog(text);return clean.Length<=4000?clean:clean.Substring(0,3920)+"\r\n[Suvestinė sutrumpinta; visa ataskaita bendroje istorijoje.]";}
  public void Acknowledge(string[] ids){var old=Read();if(ids==null||ids.Length==0)return;Data.Atomic(PathName,Data.Json().Serialize(old.Where(x=>!ids.Contains(x.Id)).ToArray()));}
 }
 public sealed class MonitorRecovery {
  public long RetryAt {get;private set;}public int Failures {get;private set;}
  public bool Due(long now){return now>0&&(RetryAt==0||now>=RetryAt);}
  public void Failed(long now){Failures=Math.Min(5,Failures+1);RetryAt=now+TimeSpan.FromMinutes(Math.Min(30,1<<(Failures-1))).Ticks;}
  public void Succeeded(){RetryAt=0;Failures=0;}
 }
}
