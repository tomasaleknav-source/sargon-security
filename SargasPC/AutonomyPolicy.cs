using System;using System.IO;
namespace SargasPC {
 public static class AutonomyPolicy {
  public static long RetryDelay(int failures){int n=Math.Max(1,Math.Min(5,failures));return TimeSpan.FromMinutes(Math.Min(360,30*(1<<(n-1)))).Ticks;}
  public static bool ClockValid(Settings s,long now){return now>0&&s.AutoDbAttemptAt<=now&&s.AutoDbSuccessAt<=now;}
  public static bool UpdateDue(Settings s,long now){if(!s.AutoDbUpdate||!ClockValid(s,now))return false;if(s.AutoDbSuccessAt>0&&now-s.AutoDbSuccessAt<TimeSpan.FromHours(24).Ticks)return false;return s.AutoDbAttemptAt<=0||now-s.AutoDbAttemptAt>=RetryDelay(s.AutoDbFailures);}
  public static string Status(Settings s,long now){if(!s.AutoDbUpdate)return "Automatiniai ClamAV bazių atnaujinimai: išjungti";if(!ClockValid(s,now))return "Automatika pristabdyta: išsaugotas laikas ateityje. Patikrinkite kompiuterio laiką";if(UpdateDue(s,now))return "Automatinis atnaujinimas laukia laisvos programos ir paruošto variklio";if(s.AutoDbSuccessAt>0&&s.AutoDbFailures==0)return "Bazių atnaujinimas patvirtintas per 24 h; kitas bandymas vėliau, kol programa veiks";return "Paskutinis atnaujinimo bandymas nepatvirtintas; pakartotinis bandymas atidėtas";}
 }
 public sealed partial class Core {
  public string ScheduledLocalEngineUpdate(){return RunScheduledUpdate(()=>UpdateLocalEngine(),()=>TestLocalEngineDetection());}
  internal string RunScheduledUpdate(Func<string> update,Func<string> control){
   Config.AutoDbAttemptAt=DateTime.UtcNow.Ticks;Config.AutoDbSuccessAt=0;Config.EngineDetectionAt=0;Config.AutoDbFailures=1+Math.Min(11,Math.Max(0,Config.AutoDbFailures));SaveSettings();
   string result;
   try{Check();result=update();Check();Config.AutoDbSuccessAt=DateTime.UtcNow.Ticks;Config.AutoDbFailures=0;SaveSettings();}
   catch(OperationCanceledException){throw;}
   catch(IOException e){NotChecked++;return "AUTOMATINIS ATNAUJINIMAS NEPATVIRTINTAS\r\n"+Data.Clean(e.Message)+"\r\n"+AutonomyPolicy.Status(Config,DateTime.UtcNow.Ticks);}
   catch(UnauthorizedAccessException){NotChecked++;return "Automatinio atnaujinimo nepatvirtino prieigos teisės; bus bandoma vėliau.";}
   if(Config.AutoDetectionControl){try{Check();result+="\r\n\r\nAUTOMATINĖ APTIKIMO KONTROLĖ\r\n"+control();}catch(OperationCanceledException){throw;}catch(IOException e){Config.EngineDetectionAt=0;SaveSettings();NotChecked++;result+="\r\nBazės atnaujintos, bet aptikimo kontrolė NEPATVIRTINTA: "+Data.Clean(e.Message);}catch(UnauthorizedAccessException){Config.EngineDetectionAt=0;SaveSettings();NotChecked++;result+="\r\nAptikimo kontrolės nepatvirtino prieigos teisės.";}}
   return result+"\r\n"+AutonomyPolicy.Status(Config,DateTime.UtcNow.Ticks);
  }
 }
}
