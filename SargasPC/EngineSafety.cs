using System;using System.IO;using System.Linq;using System.Text;using System.Security.Cryptography;
namespace SargasPC {
 public static class EngineSafety {
  public static string PrepareWorkspace(string profile){FolderMonitor.NoLinks(profile);string path=Path.Combine(profile,"clamav");Directory.CreateDirectory(path);FolderMonitor.NoLinks(path);return path;}
  public static void ValidateDatabaseFiles(string root){
   foreach(string stem in new[]{"main","daily"}){
    string[] candidates=new[]{Path.Combine(root,stem+".cvd"),Path.Combine(root,stem+".cld")}.Where(File.Exists).ToArray();
    if(candidates.Length==0)throw new IOException("Trūksta ClamAV "+stem+" bazės. Atnaujinkite bazes; patikra nepatvirtinta.");
    foreach(string file in candidates){FolderMonitor.NoLinks(file);if(new FileInfo(file).Length<512)throw new IOException("ClamAV "+stem+" bazės failas per trumpas. Atnaujinkite bazes; patikra nepatvirtinta.");}
   }
  }
  public static bool IsEicarSignature(string x){return x!=null&&(x.StartsWith("Win.Test.EICAR",StringComparison.OrdinalIgnoreCase)||string.Equals(x,"Eicar-Test-Signature",StringComparison.OrdinalIgnoreCase)||string.Equals(x,"Eicar-Signature",StringComparison.OrdinalIgnoreCase));}
  public static bool NeutralReadinessPassed(LocalEngineVerdict v){return v!=null&&!v.Found&&!v.Incomplete&&v.Scanned==1;}
  public static bool EicarOnly(LocalEngineVerdict v){return v!=null&&v.Found&&v.Signatures!=null&&v.Signatures.Length>0&&v.Signatures.All(IsEicarSignature);}
  public static bool DetectionMatches(LocalEngineVerdict neutral,LocalEngineVerdict positive){return neutral!=null&&positive!=null&&!neutral.Found&&!neutral.Incomplete&&neutral.Scanned>0&&positive.Found&&!positive.Incomplete&&positive.Scanned>0&&positive.Signatures!=null&&positive.Signatures.Any(IsEicarSignature);}
  public static string ControlStatus(long at,long now){if(at<=0||at>now)return "Aptikimo EICAR kontrolė: nepatvirtinta. Paleiskite atskirą bandymą";return now-at>TimeSpan.FromHours(24).Ticks?"Aptikimo EICAR kontrolė: senesnė nei 24 h; pakartokite bandymą":"Aptikimo EICAR kontrolė: praėjo per paskutines 24 h; tai ne visų grėsmių aptikimo garantija";}
 }
 public sealed partial class Core {
  public string DetectionControlStatus(){return EngineSafety.ControlStatus(Config.EngineDetectionAt,DateTime.UtcNow.Ticks);}
  static void VerifyControl(string path,byte[] expected){FolderMonitor.NoLinks(path);using(var f=File.OpenRead(path)){if(!Data.Bounded(f,1024,delegate{}).SequenceEqual(expected))throw new IOException("Kontrolinis failas pasikeitė arba jį perėmė kita apsauga. Aptikimas nepatvirtintas.");}}
  public string TestLocalEngineReadiness(){RequireEngine();EngineSafety.ValidateDatabaseFiles(EngineDatabase);return RunLocalEngineReadiness(p=>EngineScan(p,false));}
  internal string RunLocalEngineReadiness(Func<string,CommandOutput> scan){
   if(scan==null)throw new ArgumentNullException("scan");FolderMonitor.NoLinks(Root);string dir=Path.Combine(Root,"engine-readiness-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(dir);string path=Path.Combine(dir,"neutral.txt");byte[] expected=Encoding.ASCII.GetBytes("SARGON neutral ClamAV database runtime readiness control.");bool cleanup=true;string result="";
   try{Check();File.WriteAllBytes(path,expected);VerifyControl(path,expected);LocalEngineVerdict verdict=LocalEngine.Parse(scan(path));VerifyControl(path,expected);Check();if(!EngineSafety.NeutralReadinessPassed(verdict))throw new IOException("ClamAV nepatvirtino aktyvios duomenų bazės neutralaus failo patikros; variklio parengtis nepatvirtinta.");LocalEngineCompleted++;result="ClamAV realiai įkėlė dabar esančias bazes ir perskaitė vieną neutralų kontrolinį failą. Tai patvirtina tik variklio veikimą šiuo metu, ne visų grėsmių aptikimą ar apsaugą realiuoju laiku.";}
   finally{try{if(File.Exists(path))File.Delete(path);}catch(IOException){cleanup=false;}catch(UnauthorizedAccessException){cleanup=false;}try{if(Directory.Exists(dir))Directory.Delete(dir,false);}catch(IOException){cleanup=false;}catch(UnauthorizedAccessException){cleanup=false;}if(!cleanup){NotChecked++;throw new IOException("Laikino ClamAV parengties failo nepavyko saugiai pašalinti; kontrolės rezultatas nepatvirtintas: "+Data.Clean(dir));}}
   return result+"\r\nLaikinas failas pašalintas; ClamAV nuskaitytų failų: 1.";
  }
  public string TestLocalEngineDetection(){RequireEngine();EngineSafety.ValidateDatabaseFiles(EngineDatabase);return RunDetectionControl(p=>EngineScan(p,false));}
  internal string RunDetectionControl(Func<string,CommandOutput> scan){
   Config.EngineDetectionAt=0;SaveSettings();FolderMonitor.NoLinks(Root);
   string dir=Path.Combine(Root,"detection-control-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(dir);
   string negative=Path.Combine(dir,"neutral.txt"),positive=Path.Combine(dir,"eicar-test.txt");bool cleanup=true;string result="";
   try{
    byte[] neutral=Encoding.ASCII.GetBytes("SARGON neutral detection control."),marker=Convert.FromBase64String("WDVPIVAlQEFQWzRcUFpYNTQoUF4pN0NDKTd9JEVJQ0FSLVNUQU5EQVJELUFOVElWSVJVUy1URVNULUZJTEUhJEgrSCo=");
    Check();File.WriteAllBytes(negative,neutral);VerifyControl(negative,neutral);var n=LocalEngine.Parse(scan(negative));VerifyControl(negative,neutral);Check();
    File.WriteAllBytes(positive,marker);VerifyControl(positive,marker);var p=LocalEngine.Parse(scan(positive));VerifyControl(positive,marker);Check();
    if(!EngineSafety.DetectionMatches(n,p))throw new IOException("Aptikimo kontrolė nepraėjo: neutralus failas turi likti be žymų, EICAR žymuo turi būti aptiktas. Apsaugos neišjunkite; atnaujinkite bazes ir patikrinkite parengtį.");
    TestFindings++;Config.EngineDetectionAt=DateTime.UtcNow.Ticks;SaveSettings();
    result="Neutralus ir EICAR bandymai praėjo per vietinio ClamAV atsakymą. Tai bandymo žymuo, ne tikras virusas. EICAR rezultatas nepatvirtina kitų virusų aptikimo ar nuolatinės apsaugos.\r\n"+DetectionControlStatus();
   } finally {foreach(string f in new[]{negative,positive})try{if(File.Exists(f))File.Delete(f);}catch(IOException){cleanup=false;}catch(UnauthorizedAccessException){cleanup=false;}try{Directory.Delete(dir,false);}catch(IOException){cleanup=false;}catch(UnauthorizedAccessException){cleanup=false;}if(!cleanup){NotChecked++;Config.EngineDetectionAt=0;SaveSettings();}}
   return result+(cleanup?"\r\nLaikini bandymo failai pašalinti.":"\r\nDĖMESIO: laikino bandymo aplanko nepavyko pašalinti: "+Data.Clean(dir)+". Kontrolės būsena nepatvirtinta.");
  }
 }
}
