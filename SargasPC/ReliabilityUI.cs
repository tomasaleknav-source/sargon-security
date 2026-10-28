using System;using System.IO;using System.Linq;using System.Text;using System.Windows.Forms;
namespace SargasPC {
 public sealed partial class Window {
  private AlertStore alerts;private bool pendingAlert,alertReadFailed;private readonly MonitorRecovery monitorRecovery=new MonitorRecovery();private string healthText="";private long lastHealthNotice;
  private FileIssue activeMonitorIssue;private string activeMonitorPath;private int monitorScanFailures;private long monitorScanHold;
  private void InitializeReliability(){alerts=new AlertStore(core.Root);ReloadAlerts();}
  private void ReloadAlerts(){try{pendingAlert=alerts.HasPending();alertReadFailed=false;}catch(Exception e) when(e is IOException||e is UnauthorizedAccessException||e is ArgumentException||e is InvalidOperationException){pendingAlert=true;alertReadFailed=true;healthText="Perspėjimų žurnalas neperskaitomas; automatika pristabdyta.";}}
  private bool BackgroundBlocked(){return pendingAlert||alertReadFailed;}
  private void RememberOutcome(){
   if(core.EngineFindings<=0&&core.HashFindings<=0)return;
   pendingAlert=true;
   try{alerts.Append(lastReport);alertReadFailed=false;}catch(Exception e) when(e is IOException||e is UnauthorizedAccessException||e is ArgumentException||e is InvalidOperationException){alertReadFailed=true;healthText="Radinio perspėjimo išsaugoti nepavyko. Eksportuokite ataskaitą; automatika pristabdyta.";}
   healthText="DĖMESIO: yra neperžiūrėtų radinių. Automatinės patikros pristabdytos; atverkite perspėjimus ir veiksmų centrą.";
   RefreshReadiness();
  }
  private void MonitorWorkFinished(bool complete){
   string p=activeMonitorPath;activeMonitorPath=null;var issue=activeMonitorIssue;activeMonitorIssue=null;if(p==null)return;
   if(issue!=null){if(folderMonitor!=null){if(issue.Retryable)folderMonitor.ScheduleRetry(p,DateTime.UtcNow.Ticks);else folderMonitor.Succeeded(p);}monitorScanFailures=0;monitorScanHold=0;healthText="DĖMESIO: vienas failas nepatikrintas ("+issue.Code+"). Kiti eilės failai tikrinami; išsami priežastis ataskaitoje.";return;}
   if(complete&&core.NotChecked==0&&folderMonitor!=null)folderMonitor.Succeeded(p);
   if(!complete||core.NotChecked>0){monitorScanFailures=Math.Min(5,monitorScanFailures+1);monitorScanHold=DateTime.UtcNow.Ticks+TimeSpan.FromMinutes(Math.Min(30,1<<(monitorScanFailures-1))).Ticks;
    if(folderMonitor!=null&&!folderMonitor.Lost&&core.EngineFindings==0&&core.HashFindings==0&&!core.Cancelled)folderMonitor.Enqueue(p,DateTime.UtcNow.Ticks);
    healthText="Aplanko patikra nepatvirtinta; eilė pristabdyta iki "+new DateTime(monitorScanHold,DateTimeKind.Utc).ToLocalTime().ToString("HH:mm")+". Peržiūrėkite ataskaitą.";
   }else{monitorScanFailures=0;monitorScanHold=0;}
  }
  private void HealthTick(){
   if(IsDisposed||busy)return;long now=DateTime.UtcNow.Ticks;
   if(BackgroundBlocked()){healthText=alertReadFailed?"DĖMESIO: perspėjimų žurnalas nepatvirtintas; automatika pristabdyta.":"DĖMESIO: neperžiūrėti radiniai — automatika pristabdyta. Atverkite išsaugotus perspėjimus.";}
   else if(folderMonitor!=null&&folderMonitor.Lost){StopMonitor();monitorRecovery.Failed(now);healthText="DĖMESIO: stebėtojas prarado įvykius. Yra patikros spraga; atkurti bus bandoma vėliau. Atlikite rankinę aplanko patikrą.";}
   else if(core.Config.RestoreWatch&&core.Config.SavedWatchEnabled&&folderMonitor==null){
    if(monitorRecovery.Due(now)){RestoreSavedMonitor();if(folderMonitor==null){monitorRecovery.Failed(now);healthText="DĖMESIO: stebėjimas neveikia. Patikrinkite variklį, bazes ir aplanką; atkūrimas kartojamas su pauze.";}else{monitorRecovery.Succeeded();healthText="Stebėjimas atkurtas; ankstesnio neveikimo laikotarpio apimtis nepatvirtinta. Rekomenduojama rankinė aplanko patikra.";}}
   }else if(folderMonitor!=null&&folderMonitor.Dropped>0){healthText="DĖMESIO: stebėjimo eilė nepriėmė dalies įvykių ("+folderMonitor.Dropped+"). Tai nėra aptiktų virusų skaičius. Nauji priimti įvykiai tikrinami; ankstesnei spragai reikia rankinės aplanko patikros.";}
   else if(folderMonitor!=null&&now>=monitorScanHold){healthText="Stebimas vienas aplankas. Tai nėra nuolatinė visos sistemos apsauga.";}
   else if(folderMonitor==null&&!core.Config.SavedWatchEnabled){healthText="DĖMESIO: aplanko stebėjimas išjungtas.";}
   if(tray!=null)tray.Text=BackgroundBlocked()?"SARGON — neperžiūrėti perspėjimai":folderMonitor==null?"SARGON — aplanko stebėjimas neveikia":monitorScanHold>now?"SARGON — patikrų eilė pristabdyta":"SARGON — veikia ribotas aplanko stebėjimas";
   if(healthText.StartsWith("DĖMESIO",StringComparison.Ordinal)&&(lastHealthNotice==0||now-lastHealthNotice>=TimeSpan.FromMinutes(30).Ticks)){lastHealthNotice=now;Notify("SARGON — reikia dėmesio",healthText,ToolTipIcon.Warning);}
   RefreshReadiness();
  }
  private void PendingAlertsDialog(){
   AlertEvidence[] snapshot;try{snapshot=alerts.Read();}catch(Exception e) when(e is IOException||e is UnauthorizedAccessException||e is ArgumentException||e is InvalidOperationException){MessageBox.Show(this,"Perspėjimų žurnalo nepavyko perskaityti. Jis neperrašomas. Eksportuokite dabartinę ataskaitą ir sutvarkykite prieigą arba sugadintą žurnalą prieš tęsdami automatiką.");return;}
   using(var d=new Form{Text="Išsaugoti perspėjimai",Width=850,Height=650,StartPosition=FormStartPosition.CenterParent}){
    var box=new TextBox{Left=18,Top=18,Width=790,Height=450,Multiline=true,ReadOnly=true,ScrollBars=ScrollBars.Both,WordWrap=false,Text=snapshot.Length==0?"Neperžiūrėtų įrašų nėra.":string.Join("\r\n\r\n----------------\r\n",snapshot.Select(x=>new DateTime(x.At,DateTimeKind.Utc).ToLocalTime().ToString("g")+"\r\n"+x.Report))};
    var note=new Label{Left=18,Top=480,Width=790,Height=55,Text="Peržiūra nėra neutralizavimas. Veiksmus atlikite grėsmių centre; po paleidimo iš naujo failą patikrinkite dar kartą. Istorinio teksto keliai nenaudojami šalinimui."};
    var ack=new Button{Left=18,Top=540,Width=790,Height=45,Text="Pažymėti šiuos įrašus peržiūrėtais ir tęsti automatiką",Enabled=snapshot.Length>0};
    ack.Click+=(s,e)=>{if(!Confirm("Tęsti automatiką?","Tai tik peržiūros patvirtinimas. Jis nepašalina grėsmės ir nepatvirtina kompiuterio saugumo. Išsamūs ankstesni įrašai lieka bendroje istorijoje."))return;try{alerts.Acknowledge(snapshot.Select(x=>x.Id).ToArray());ReloadAlerts();d.Close();HealthTick();}catch(Exception error) when(error is IOException||error is UnauthorizedAccessException){MessageBox.Show(d,"Peržiūros išsaugoti nepavyko; automatika lieka pristabdyta.");}};
    d.Controls.AddRange(new Control[]{box,note,ack});ThemeDialog(d);d.ShowDialog(this);
   }
  }
 }
}
