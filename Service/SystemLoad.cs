using System;using System.Runtime.InteropServices;
namespace Sargon.Service {
 public sealed class SystemLoad {
  [StructLayout(LayoutKind.Sequential)]struct FileTime{public uint Low,High;public ulong Value{get{return ((ulong)High<<32)|Low;}}}
  [StructLayout(LayoutKind.Sequential)]struct Power{public byte AC,Battery,Percent,Reserved;public uint Life,Full;}
  [DllImport("kernel32.dll")]static extern bool GetSystemTimes(out FileTime idle,out FileTime kernel,out FileTime user);
  [DllImport("kernel32.dll")]static extern bool GetSystemPowerStatus(out Power status);
  ulong? oldIdle,oldTotal;
  public bool? OnAc(){Power p;if(!GetSystemPowerStatus(out p))return null;return p.AC==1?true:p.AC==0?(bool?)false:null;}
  public double? Cpu(){FileTime i,k,u;if(!GetSystemTimes(out i,out k,out u))return null;ulong total=k.Value+u.Value;double? result=null;if(oldTotal.HasValue&&total>oldTotal.Value&&i.Value>=oldIdle.Value){ulong all=total-oldTotal.Value,idle=i.Value-oldIdle.Value;if(idle<=all)result=100.0*(all-idle)/all;}oldIdle=i.Value;oldTotal=total;return result;}
 }
}
