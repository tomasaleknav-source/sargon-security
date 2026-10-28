using System;using System.IO;using System.Globalization;
namespace SargasPC {
 public sealed class FileIssue {
  public string Path,Code,Context;public long? Size;public long Limit;public bool Retryable;
  public string Report(){string reason=Code=="TOO_LARGE"?"Failas viršija šio režimo dydžio ribą":Code=="MISSING"?"Failas arba jo aplankas neberastas":Code=="ACCESS_DENIED"?"Nepakanka skaitymo teisių":Code=="LOCKED"?"Failas užrakintas arba naudojamas nesuderinamu režimu":Code=="UNSAFE_LINK"?"Kelias eina per nuorodą / reparse objektą":Code=="NOT_FILE"?"Pasirinktas objektas nėra įprastas failas":Code=="PATH_TOO_LONG"?"Kelias per ilgas šiai vykdymo aplinkai":Code=="INVALID_PATH"?"Netinkamas failo kelias":"Nepavyko nuskaityti failo; priežastis tiksliau nenustatyta";
   return "NEPATIKRINTAS FAILAS\r\nKontekstas: "+Data.Clean(Context)+"\r\nFailas: "+Data.Clean(Path)+"\r\nPriežastis: "+reason+" ["+Code+"]\r\nDydis: "+(Size.HasValue?Size.Value.ToString(CultureInfo.InvariantCulture)+" baitų":"nepavyko nustatyti")+"; šio režimo riba: "+Limit.ToString(CultureInfo.InvariantCulture)+" baitų ("+(Limit/1048576)+" MiB).\r\nTai nėra viruso radinys ar saugumo patvirtinimas.\r\n";
  }
  public static FileIssue From(string path,long limit,string context,Exception error){var special=error as FileScopeException;string code=special!=null?special.Code:error is FileNotFoundException||error is DirectoryNotFoundException?"MISSING":error is UnauthorizedAccessException?"ACCESS_DENIED":error is PathTooLongException?"PATH_TOO_LONG":error is ArgumentException||error is NotSupportedException?"INVALID_PATH":(error.HResult&65535)==32||(error.HResult&65535)==33?"LOCKED":"IO_ERROR";long? size=special==null?null:special.Size;if(!size.HasValue&&code!="UNSAFE_LINK"&&code!="INVALID_PATH"){try{var file=new FileInfo(path);if(file.Exists)size=file.Length;}catch(Exception e) when(e is IOException||e is UnauthorizedAccessException||e is ArgumentException||e is NotSupportedException){}}
   return new FileIssue{Path=path,Limit=limit,Context=context,Code=code,Size=size,Retryable=code=="LOCKED"||code=="ACCESS_DENIED"||code=="IO_ERROR"};}
 }
 public sealed class FileScopeException:IOException {public readonly string Code;public readonly long? Size;public FileScopeException(string code,long? size=null):base(code){Code=code;Size=size;}}
 public static class FileScope {
  public static void Require(string path,long maximum){if(maximum<1)throw new ArgumentOutOfRangeException("maximum");string full=System.IO.Path.GetFullPath(path);var attributes=File.GetAttributes(full);if((attributes&FileAttributes.ReparsePoint)!=0)throw new FileScopeException("UNSAFE_LINK");if((attributes&FileAttributes.Directory)!=0)throw new FileScopeException("NOT_FILE");for(string current=System.IO.Path.GetDirectoryName(full);current!=null;current=System.IO.Path.GetDirectoryName(current))if((File.GetAttributes(current)&FileAttributes.ReparsePoint)!=0)throw new FileScopeException("UNSAFE_LINK");long length=new FileInfo(full).Length;if(length>maximum)throw new FileScopeException("TOO_LARGE",length);}
 }
 public sealed class FileAttempt {public Witness Witness;public FileIssue Issue;}
 public sealed partial class Core {
  public string AttemptPath="";public long AttemptLimit;
  public FileAttempt TryReadFile(string path,long maximum,bool lowImpact,string context){if(maximum<1||maximum>268435456)throw new ArgumentOutOfRangeException("maximum");try{AttemptPath=path;AttemptLimit=maximum;FileScope.Require(path,maximum);return new FileAttempt{Witness=Scan(path,maximum,lowImpact)};}catch(Exception e) when(e is IOException||e is UnauthorizedAccessException||e is ArgumentException||e is NotSupportedException){NotChecked++;return new FileAttempt{Issue=FileIssue.From(path,maximum,context,e)};}}
 }
}
