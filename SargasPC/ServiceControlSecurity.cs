using System;using System.IO;using System.Runtime.InteropServices;using System.Security.AccessControl;using System.Security.Principal;
namespace SargasPC {public static class ServiceControlSecurity {
 public const int UserControl=256,ReadOnlyRights=0x2008D;
 public const string Descriptor="D:P(A;;CCDCLCSWRPWPDTLOCRSDRCWDWO;;;SY)(A;;CCDCLCSWRPWPDTLOCRSDRCWDWO;;;BA)(A;;CCLCSWLORC;;;AU)";
 [DllImport("advapi32.dll",CharSet=CharSet.Unicode,SetLastError=true)]static extern IntPtr OpenSCManager(string machine,string database,uint rights);
 [DllImport("advapi32.dll",CharSet=CharSet.Unicode,SetLastError=true)]static extern IntPtr OpenService(IntPtr manager,string name,uint rights);
 [DllImport("advapi32.dll",SetLastError=true)]static extern bool SetServiceObjectSecurity(IntPtr service,uint info,byte[] descriptor);
 [DllImport("advapi32.dll",SetLastError=true)]static extern bool QueryServiceObjectSecurity(IntPtr service,uint info,byte[] descriptor,uint size,out uint required);
 [DllImport("advapi32.dll")]static extern bool CloseServiceHandle(IntPtr handle);
 static byte[] Bytes(){var raw=new RawSecurityDescriptor(Descriptor);byte[] bytes=new byte[raw.BinaryLength];raw.GetBinaryForm(bytes,0);return bytes;}
 public static bool IsRestricted(byte[] bytes){var raw=new RawSecurityDescriptor(bytes,0);if(raw.DiscretionaryAcl==null)return false;bool admin=false,system=false;foreach(GenericAce ace in raw.DiscretionaryAcl){var rule=ace as CommonAce;if(rule==null)return false;if(rule.AceQualifier!=AceQualifier.AccessAllowed)continue;if((rule.AccessMask&(UserControl|0x10000000|0x20000000|0x40000000))==0)continue;string sid=rule.SecurityIdentifier.Value;if(sid=="S-1-5-32-544")admin=true;else if(sid=="S-1-5-18")system=true;else return false;}return admin&&system;}
 static void Access(bool write,Action<IntPtr> action){if(Environment.OSVersion.Platform!=PlatformID.Win32NT)throw new IOException("Reikia Windows SCM.");IntPtr manager=OpenSCManager(null,null,1);if(manager==IntPtr.Zero)throw new IOException("SCM prieiga nepatvirtinta.");try{IntPtr service=OpenService(manager,"SARGONProtection",write?0x40000u:0x20000u);if(service==IntPtr.Zero)throw new IOException("Paslaugos teisių prieiga nepatvirtinta.");try{action(service);}finally{CloseServiceHandle(service);}}finally{CloseServiceHandle(manager);}}
 public static void Apply(){Access(true,service=>{if(!SetServiceObjectSecurity(service,4,Bytes()))throw new IOException("Paslaugos komandų teisių nustatyti nepavyko.");});}
 public static bool Verify(){try{bool valid=false;Access(false,service=>{uint size;QueryServiceObjectSecurity(service,4,null,0,out size);if(size<20||size>65536)return;byte[] bytes=new byte[size];if(QueryServiceObjectSecurity(service,4,bytes,size,out size))valid=IsRestricted(bytes);});return valid;}catch(Exception e) when(e is IOException||e is ArgumentException||e is UnauthorizedAccessException){return false;}}
}}
