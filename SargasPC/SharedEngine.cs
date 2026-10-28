using System;using System.IO;
namespace SargasPC {
 public static class SharedEngine {
  public static string DatabasePath(){string root=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),"SARGONService");string shared=Path.Combine(root,"Databases");return Directory.Exists(shared)?shared:Path.Combine(root,"Private","clamav","database");}
  public static void UseIfAvailable(Core core){if(Environment.OSVersion.Platform!=PlatformID.Win32NT)return;string bin=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),"SARGONService","engine","clamav-1.4.6.win.x64"),db=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),"SARGONService","Databases");try{if(File.Exists(Path.Combine(bin,"clamscan.exe"))&&File.Exists(Path.Combine(bin,"freshclam.exe"))&&Directory.Exists(db)){FolderMonitor.NoLinks(bin);FolderMonitor.NoLinks(db);core.EngineBinaryDirectory=bin;core.EngineDatabaseDirectory=db;core.SharedReadOnlyEngine=true;}}catch(IOException){}catch(UnauthorizedAccessException){}}
 }
}
