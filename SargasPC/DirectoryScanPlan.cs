using System;using System.IO;using System.Linq;using System.Collections.Generic;
namespace SargasPC {
 public sealed class ScanGap {public string Path,Reason;}
 public sealed class DirectoryPlanState {public List<ScanGap> GapDetails=new List<ScanGap>();public int DetailsOmitted;
  public string Id;public string[] Roots;public List<string> Gaps=new List<string>();public List<string> Pending=new List<string>();public int Completed,Findings,Unverified;public long Started,Finished;
 }
 public sealed class DirectoryScanPlan {
  public readonly string FileName;public DirectoryPlanState State{get;private set;}private readonly string[] exclusions;
  public DirectoryScanPlan(string file,string[] roots,string[] exclude){
   FileName=file;exclusions=(exclude??new string[0]).Select(Path.GetFullPath).ToArray();
   if(roots==null||roots.Length==0||roots.Length>16)throw new IOException("Reikia nuo 1 iki 16 vietinių šaknų.");
   string[] normalized=roots.Select(Normalize).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
   FolderMonitor.NoLinks(Path.GetDirectoryName(file));
   if(File.Exists(file)){FolderMonitor.NoLinks(file);try{State=Data.Read<DirectoryPlanState>(file,8388608);}catch(Exception e) when(e is ArgumentException||e is InvalidOperationException||e is MissingMethodException){throw new IOException("Patikrų planas sugadintas; jis neperrašytas.",e);}
    if(State==null||State.DetailsOmitted<0||(State.GapDetails!=null&&(State.GapDetails.Count>256||State.GapDetails.Any(g=>g==null||!Allowed(g.Path,normalized)||g.Reason==null||g.Reason.Length>2048)))||State.Id==null||State.Id.Length!=32||State.Roots==null||!State.Roots.SequenceEqual(normalized,StringComparer.OrdinalIgnoreCase)||State.Pending==null||State.Pending.Count>8192||State.Gaps==null||State.Gaps.Count>1024||State.Gaps.Any(x=>!Allowed(x,normalized))||State.Completed<0||State.Unverified<0||State.Findings<0||State.Started<=0||State.Finished<0||State.Pending.Any(x=>!Allowed(x,normalized)))throw new IOException("Patikrų plano apimtis nepatvirtinta; jis neperrašytas.");
   }else{State=new DirectoryPlanState{Id=Guid.NewGuid().ToString("N"),Roots=normalized,Pending=normalized.ToList(),Started=DateTime.UtcNow.Ticks};Save();}
  }
  static string Normalize(string p){if(string.IsNullOrWhiteSpace(p)||!Path.IsPathRooted(p)||p.StartsWith("\\\\",StringComparison.Ordinal)||p.Length>512||p.Any(c=>c<' '))throw new IOException("Netinkama vietinė plano šaknis.");return Path.GetFullPath(p);}
  static bool Under(string p,string root){return string.Equals(p,root,StringComparison.OrdinalIgnoreCase)||p.StartsWith(root.TrimEnd(Path.DirectorySeparatorChar,Path.AltDirectorySeparatorChar)+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase);}
  static bool Allowed(string p,string[] roots){try{string n=Normalize(p);return string.Equals(n,p,StringComparison.Ordinal)&&roots.Any(x=>Under(n,x));}catch(IOException){return false;}}
  public bool Step(Func<string,bool> scan,Action check,Func<bool> incomplete=null,Func<string> incompleteDetail=null){
   check();if(State.Pending.Count==0)return false;string current=State.Pending[0];
   // Mutate only after scan/cancellation checks. A killed process retries its current directory.
   int localDetailsOmitted=0;var details=new List<ScanGap>();Action<string,string> gap=(path,reason)=>{if(details.Count<256)details.Add(new ScanGap{Path=Allowed(path,State.Roots)?path:current,Reason=reason.Substring(0,Math.Min(2048,reason.Length))});else localDetailsOmitted++;};
   var next=State.Pending.Skip(1).ToList();int unverified=0,found=0;bool excluded=exclusions.Any(x=>Under(current,x));
   if(excluded){unverified++;gap(current,"EXCLUDED: SARGON apsaugotas aplankas");}
   else try{
    FolderMonitor.NoLinks(current);if(!Directory.Exists(current))throw new IOException("Aplankas nepasiekiamas.");
    check();try{found=scan(current)?1:0;if(incomplete!=null&&incomplete()){unverified++;gap(current,"ENGINE_LIMIT: "+(incompleteDetail==null?"Variklis nepatvirtino visos šio aplanko apimties.":incompleteDetail()));}}catch(OperationCanceledException){throw;}catch(CommandFailure e){if(e.Problem==CommandProblem.Cancelled)throw;unverified++;gap(current,"SCAN_FAILED: "+e.Problem);}catch(IOException){unverified++;gap(current,"IO_ERROR: aplanko nuskaitymas arba išvardijimas nepavyko.");}catch(UnauthorizedAccessException){unverified++;gap(current,"ACCESS_DENIED: trūksta prieigos teisių.");}check();
    int seen=0;foreach(string child in Directory.EnumerateDirectories(current,"*",SearchOption.TopDirectoryOnly)){
     check();if(++seen>8192){unverified++;gap(current,"ENUMERATION_LIMIT: daugiau nei 8192 tiesioginiai poaplankiai.");break;}string full=Path.GetFullPath(child);
     if(next.Count>=8192||!Allowed(full,State.Roots)||exclusions.Any(x=>Under(full,x))){unverified++;gap(full,next.Count>=8192?"QUEUE_LIMIT: 8192 laukiančių aplankų riba.":"EXCLUDED_OR_PATH_LIMIT: neįtrauktas kelias.");continue;}
     try{FolderMonitor.NoLinks(full);if(!next.Contains(full,StringComparer.OrdinalIgnoreCase))next.Insert(0,full);}catch(IOException){unverified++;gap(full,"UNSAFE_OR_UNAVAILABLE: nuoroda arba nepasiekiamas poaplankis.");}catch(UnauthorizedAccessException){unverified++;gap(full,"ACCESS_DENIED: poaplankis nepasiekiamas.");}
    }
   }catch(OperationCanceledException){throw;}catch(CommandFailure e){if(e.Problem==CommandProblem.Cancelled)throw;unverified++;gap(current,"SCAN_FAILED: "+e.Problem);}catch(IOException){unverified++;gap(current,"IO_ERROR: aplanko nuskaitymas arba išvardijimas nepavyko.");}catch(UnauthorizedAccessException){unverified++;gap(current,"ACCESS_DENIED: trūksta prieigos teisių.");}
   check();var previous=State;State=new DirectoryPlanState{Id=previous.Id,Roots=previous.Roots,Pending=next,Completed=(int)Math.Min(int.MaxValue,(long)previous.Completed+1),Findings=(int)Math.Min(int.MaxValue,(long)previous.Findings+found),Unverified=(int)Math.Min(int.MaxValue,(long)previous.Unverified+unverified),Started=previous.Started,Finished=next.Count==0?DateTime.UtcNow.Ticks:0};
   var allDetails=(previous.GapDetails??new List<ScanGap>()).Concat(details).GroupBy(x=>x.Path+"\0"+x.Reason,StringComparer.OrdinalIgnoreCase).Select(g=>g.First()).ToList();State.GapDetails=allDetails.Take(256).ToList();State.DetailsOmitted=(int)Math.Min(int.MaxValue,(long)previous.DetailsOmitted+localDetailsOmitted+Math.Max(0,allDetails.Count-256));
   State.Gaps=previous.Gaps.ToList();if(unverified>0&&State.Gaps.Count<1024&&!State.Gaps.Contains(current,StringComparer.OrdinalIgnoreCase))State.Gaps.Add(current);
   try{Save();}catch{State=previous;throw;}return true;
  }
  void Save(){string json=Data.Json().Serialize(State);if(System.Text.Encoding.UTF8.GetByteCount(json)>8388608)throw new IOException("Plano apimties riba.");Data.Atomic(FileName,json);}
 }
 public sealed partial class Core {
  public string ScanDirectoryLevel(string directory){Check();FolderMonitor.NoLinks(directory);if(!Directory.EnumerateFiles(directory,"*",SearchOption.TopDirectoryOnly).Any()){Check();return "TUŠČIAS APLANKO LYGIS — failų šiame lygyje nerasta. ClamAV nevykdytas; poaplankiai tikrinami atskirais eilės žingsniais. Tai momentinė apimtis, ne viso aplanko saugumo verdiktas.\r\n"+Data.Clean(directory);}var result=EngineScan(directory,false);var verdict=LocalEngine.Parse(result);if(verdict.Incomplete)NotChecked++;else LocalEngineCompleted++;
   if(verdict.Found){if(EngineSafety.EicarOnly(verdict))TestFindings++;else EngineFindings++;}
   return "CLAMAV APLANKO LYGIS (be poaplankių)\r\n"+Data.Clean(directory)+"\r\nFailų: "+verdict.Scanned+"; radinys: "+verdict.Found+"; ribota apimtis: "+verdict.Incomplete+"\r\n"+LocalEngine.ReportOutput(Data.CleanLog(result.Output),verdict)+"\r\n"+Data.CleanLog(result.Error);
  }
  public string IsolateAndConfirm(Witness witness){string result=Isolate(witness,true);if(VisualEvent=="REMOVAL_CONFIRMED"&&File.Exists(witness.Path)){VisualEvent="UNCONFIRMED";NotChecked++;return result+"\r\nKelias tebėra pasiekiamas arba atsirado naujas failas. Pakartokite jo patikrą; neutralizavimas nepatvirtintas.";}return result+"\r\nPatvirtinimas taikomas tik šiam originalui ir kopijos veiksmui, ne viso kompiuterio saugumui.";}
 }
}
