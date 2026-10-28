using System;using System.IO;using System.Windows.Forms;using Microsoft.Win32;
namespace SargasPC {
 public sealed partial class Window {
  private NotifyIcon tray; private bool startupLaunch,configureLaunch,exitRequested;private long lastIssueNotice;
  private const string RunKey="Software\\Microsoft\\Windows\\CurrentVersion\\Run";
  public void ConfigureBackgroundLaunch(bool startup,bool configure){startupLaunch=startup;configureLaunch=configure;}
  public static string StartupCommand(string path){if(string.IsNullOrWhiteSpace(path)||path.IndexOf('"')>=0||path.IndexOf('\r')>=0||path.IndexOf('\n')>=0||!Path.IsPathRooted(path))throw new IOException("Netinkamas programos kelias");return "\""+Path.GetFullPath(path)+"\" --startup";}
  private bool StartupEnabled(){if(Environment.OSVersion.Platform!=PlatformID.Win32NT)return false;using(var key=Registry.CurrentUser.OpenSubKey(RunKey)){return key!=null&&string.Equals(key.GetValue("SARGON Security") as string,StartupCommand(Application.ExecutablePath),StringComparison.OrdinalIgnoreCase);}}
  private void SetStartup(bool value){if(Environment.OSVersion.Platform!=PlatformID.Win32NT)throw new IOException("Paleistis galima tik Windows.");using(var key=Registry.CurrentUser.CreateSubKey(RunKey)){if(value)key.SetValue("SARGON Security",StartupCommand(Application.ExecutablePath),RegistryValueKind.String);else key.DeleteValue("SARGON Security",false);}if(StartupEnabled()!=value)throw new IOException("Windows paleisties įrašo patvirtinti nepavyko.");}
  private void InitializeTray(){
   tray=new NotifyIcon{Icon=Icon??System.Drawing.SystemIcons.Shield,Text="SARGON — atverkite apsaugos būseną",Visible=true};
   var menu=new ContextMenuStrip();menu.Items.Add("Atverti SARGON",null,(s,e)=>ShowFromTray());menu.Items.Add("Išsaugoti perspėjimai",null,(s,e)=>{ShowFromTray();PendingAlertsDialog();});menu.Items.Add("Baigti ir sustabdyti stebėjimą",null,(s,e)=>{if(!Confirm("Baigti SARGON?","Aplanko stebėjimas ir automatiniai atnaujinimai sustos iki kito paleidimo."))return;exitRequested=true;ShowFromTray();Close();if(!IsDisposed)exitRequested=false;});tray.ContextMenuStrip=menu;tray.DoubleClick+=(s,e)=>ShowFromTray();tray.BalloonTipClicked+=(s,e)=>ShowFromTray();
   FormClosing+=(s,e)=>{if(e.CloseReason==CloseReason.UserClosing&&!exitRequested){e.Cancel=true;Hide();Notify("SARGON veikia dėkle","Pasirinkto aplanko stebėjimas tęsiamas tik jei paruoštas ir įjungtas. Atverti — dukart skydo piktogramą; baigti — dėklo meniu.",ToolTipIcon.Info);}};
  }
  private void DisposeTray(){if(tray!=null){tray.Visible=false;if(tray.ContextMenuStrip!=null)tray.ContextMenuStrip.Dispose();tray.Dispose();tray=null;}}
  private void ShowFromTray(){Show();WindowState=FormWindowState.Normal;Activate();}
  private void Notify(string title,string message,ToolTipIcon kind){if(tray!=null){tray.BalloonTipTitle=title;tray.BalloonTipText=message;tray.BalloonTipIcon=kind;tray.ShowBalloonTip(8000);}}
  private void NotifyOutcome(bool complete){
   if(core.EngineFindings>0||core.HashFindings>0){Notify("SARGON — radinys, reikia peržiūros","Neatverkite įtartino failo. Atverkite SARGON ataskaitą ir veiksmų centrą. Karantino rezultatą tikrinkite ataskaitoje; vien radinys nėra pašalinimo patvirtinimas.",ToolTipIcon.Warning);return;}
   if((!complete||core.NotChecked>0)&&DateTime.UtcNow.Ticks-lastIssueNotice>TimeSpan.FromMinutes(30).Ticks){lastIssueNotice=DateTime.UtcNow.Ticks;Notify("SARGON — patikra nepatvirtinta","Dalis failų ar variklio veiksmų liko nepatikrinta. Atverkite SARGON ataskaitą; apsauga nepatvirtinta.",ToolTipIcon.Warning);}
  }
  private void PrepareBackground(){
   string oldDirectory=core.Config.SavedWatchDirectory;bool oldQuarantine=core.Config.SavedAutoQuarantine;
   if(string.IsNullOrEmpty(core.Config.SavedWatchDirectory)){string p=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),"Downloads");if(!Directory.Exists(p))throw new IOException("Downloads aplankas nerastas. Pasirinkite stebimą aplanką nuostatose.");FolderMonitor.NoLinks(p);core.Config.SavedWatchDirectory=p;core.Config.SavedAutoQuarantine=false;}
   bool enabled=core.Config.SavedWatchEnabled,restore=core.Config.RestoreWatch,updates=core.Config.AutoDbUpdate;try{core.Config.SavedWatchEnabled=true;core.Config.RestoreWatch=true;core.Config.AutoDbUpdate=true;core.SaveSettings();}catch{core.Config.SavedWatchDirectory=oldDirectory;core.Config.SavedAutoQuarantine=oldQuarantine;core.Config.SavedWatchEnabled=enabled;core.Config.RestoreWatch=restore;core.Config.AutoDbUpdate=updates;throw;}RestoreSavedMonitor();AutomationTick();
  }
  private void ApplyBackgroundLaunch(){
   if(configureLaunch){try{PrepareBackground();}catch(Exception e) when(e is IOException||e is UnauthorizedAccessException){status.Text="Foninė parengtis nepatvirtinta: "+Data.Clean(e.Message);}}
   if(startupLaunch){BeginInvoke((Action)delegate{Hide();Notify("SARGON paleista prisijungus","Atverkite skydo piktogramą ir patikrinkite variklį bei stebimą aplanką. Vien programos paleidimas nepatvirtina apsaugos.",ToolTipIcon.Info);});}
  }
  private void BackgroundSettings(){using(var d=new Form{Text="Windows paleistis ir foninis režimas",Width=760,Height=550,StartPosition=FormStartPosition.CenterParent}){
   var start=new CheckBox{Left=18,Top=18,Width=700,Height=60,Text="Paleisti SARGON dėkle prisijungus prie šios Windows paskyros"};
   try{start.Checked=StartupEnabled();}catch(Exception e) when(e is IOException||e is UnauthorizedAccessException||e is System.Security.SecurityException){status.Text="Paleisties įrašo perskaityti nepavyko.";}
   var info=new Label{Left=18,Top=90,Width=700,Height=255,Text="Paleistis vyksta prisijungus, ne prieš Windows prisijungimą. Windows gali ją atidėti arba išjungti paleisties nuostatose. Internetas reikalingas bazių atnaujinimams; vietinė patikra veikia ir be jo.\r\n\r\nUždarymo X palieka programą dėkle. Baigti ir sustabdyti — dėklo meniu.\r\n\r\nFoninė parengtis įjungia 24 h bazių atnaujinimus ir pasirinkto aplanko atkūrimą. Jei aplankas nepasirinktas, parenkamas profilio Downloads. Automatinis karantinas naujam aplankui išjungtas. Vietinį ClamAV ir bazes pirmiausia paruoškite variklio meniu. Vienas aplankas be poaplankių; visos sistemos ar failo paleidimo blokavimo nėra."};
   var save=new Button{Left=18,Top=355,Width=700,Height=50,Text="Išsaugoti Windows paleisties pasirinkimą"};save.Click+=(s,e)=>{try{SetStartup(start.Checked);MessageBox.Show(d,"Paleisties įrašas išsaugotas. Windows paleisties nuostatose jis gali būti atskirai išjungtas.");}catch(Exception error) when(error is IOException||error is UnauthorizedAccessException||error is System.Security.SecurityException){MessageBox.Show(d,"Paleisties pakeisti nepavyko: "+Data.Clean(error.Message));}};
   var ready=new Button{Left=18,Top=415,Width=700,Height=50,Text="Įjungti foninę parengtį: aplankas ir atnaujinimai"};ready.Click+=(s,e)=>{try{PrepareBackground();MessageBox.Show(d,"Nuostatos išsaugotos. Patikrinkite vietinio variklio ir aplanko būseną pagrindiniame lange.");}catch(Exception error) when(error is IOException||error is UnauthorizedAccessException){MessageBox.Show(d,"Parengtis nepatvirtinta: "+Data.Clean(error.Message));}};
   d.Controls.AddRange(new Control[]{start,info,save,ready});ThemeDialog(d);d.ShowDialog(this);
  }}
 }
}
