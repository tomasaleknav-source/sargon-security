using System;
namespace SargasPC {
 public sealed class UpdateRequestState {public int Accepted,Completed;public bool Pending,Running,AllowBattery;public string Result="NONE";public long FinishedAt;}
 public sealed class ManualUpdateQueue {
  readonly object sync=new object();int accepted,completed;bool pending,running,allow;string result="NONE";long finished;
  public bool Enqueue(int command){if(command!=128&&command!=129)return false;lock(sync){if(pending||running||accepted==int.MaxValue)return false;accepted++;pending=true;allow=command==129;result="QUEUED";return true;}}
  public UpdateRequestState Snapshot(){lock(sync)return new UpdateRequestState{Accepted=accepted,Completed=completed,Pending=pending,Running=running,AllowBattery=allow,Result=result,FinishedAt=finished};}
  public bool Take(){lock(sync){if(!pending||running)return false;pending=false;running=true;result="RUNNING";return true;}}
  public void Complete(bool success,long now){lock(sync){if(!running)throw new InvalidOperationException("No update running");completed=accepted;running=false;allow=false;result=success?"SUCCESS":"INCOMPLETE";finished=now;}}
 }
 public static class UpdateRequestPolicy {
  public static string Hold(UpdateRequestState request,bool? ac){if(request==null||(!request.Pending&&!request.Running))return "NO_REQUEST";if(request.AllowBattery||ac==true)return "";return ac==false?"MANUAL_UPDATE_WAITING_AC":"MANUAL_UPDATE_POWER_UNKNOWN";}
  public static bool Completed(ServiceHeartbeat heartbeat,string run,int previous,long now){return ServiceHeartbeat.Fresh(heartbeat,now)&&heartbeat.RunId==run&&previous>=0&&previous<int.MaxValue&&heartbeat.ManualCompleted==previous+1&&heartbeat.ManualAccepted==heartbeat.ManualCompleted&&heartbeat.ManualFinishedAt>0&&heartbeat.ManualFinishedAt<=now;}
  public static string Explain(string state){if(state=="PAUSED_BATTERY"||state=="MANUAL_UPDATE_WAITING_AC")return "Prijunkite įkroviklį. Nauji darbai ir bazių atsisiuntimas pristabdyti dėl baterijos. Vienkartinį bazių atnaujinimą baterija galite pasirinkti atnaujinimų lange.";if(state=="PAUSED_POWER_UNKNOWN"||state=="MANUAL_UPDATE_POWER_UNKNOWN")return "Maitinimo būsena nepatvirtinta. Prijunkite įkroviklį arba aiškiai pasirinkite vienkartinį bazių atnaujinimą nepriklausomai nuo maitinimo būsenos.";if(state=="MANUAL_UPDATING_DATABASE")return "Vyksta jūsų užsakytas bazių atnaujinimas. Palaukite jo rezultato.";return "";}
 }
}
