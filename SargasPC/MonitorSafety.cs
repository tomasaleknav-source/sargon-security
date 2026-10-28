using System;using System.IO;
namespace SargasPC {
 public static class MonitorSafety {
  static bool Under(string p,string root){p=Path.GetFullPath(p);root=Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar,Path.AltDirectorySeparatorChar);return string.Equals(p,root,StringComparison.OrdinalIgnoreCase)||p.StartsWith(root+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase);}
  public static bool SafeWatchRoot(string watched,string privateRoot,string installRoot){try{return !Under(watched,privateRoot)&&!Under(watched,installRoot);}catch(ArgumentException){return false;}catch(NotSupportedException){return false;}}
  public static bool IsPersonalFolder(string path,string profile){try{if(string.IsNullOrWhiteSpace(profile))return false;foreach(string name in new[]{"Desktop","Documents","Downloads","Pictures","Videos","Music"})if(Under(path,Path.Combine(profile,name)))return true;return false;}catch(ArgumentException){return false;}catch(NotSupportedException){return false;}}
  public static bool RecentControl(long at,long now){return at>0&&at<=now&&now-at<TimeSpan.FromHours(24).Ticks;}
  public static bool CanAutoQuarantine(Witness w,int incomplete,string watched,string profile,long control,long now){if(w==null||w.Kind!="ENGINE"||w.Test||w.Size<0||w.Size>Core.MaxVault||incomplete!=0||!RecentControl(control,now)||!IsPersonalFolder(watched,profile))return false;try{return string.Equals(Path.GetDirectoryName(Path.GetFullPath(w.Path)),Path.GetFullPath(watched).TrimEnd(Path.DirectorySeparatorChar),StringComparison.OrdinalIgnoreCase);}catch(ArgumentException){return false;}catch(NotSupportedException){return false;}}
 }
}
