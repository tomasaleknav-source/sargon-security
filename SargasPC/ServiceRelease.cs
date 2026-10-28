using System;using System.IO;using System.Linq;using System.Text;using System.Security.Cryptography;using System.Security.Cryptography.X509Certificates;
namespace SargasPC {
 public sealed class ReleaseFile{public string Name,Hash;}
 public sealed class ServiceRelease{public int Version;public ReleaseFile[] Files;public string Signature;
  public static string Canonical(ServiceRelease m){if(m==null||m.Version<=0||m.Files==null||m.Files.Length==0||m.Files.Length>64)throw new IOException("Netinkamas leidimo manifestas.");var names=new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase);foreach(var f in m.Files){if(f==null||string.IsNullOrEmpty(f.Name)||f.Name.Length>100||!f.Name.All(c=>(c>='A'&&c<='Z')||(c>='a'&&c<='z')||(c>='0'&&c<='9')||c=='.'||c=='_'||c=='-')||f.Name=="."||f.Name==".."||!names.Add(f.Name)||!Data.Hash(f.Hash))throw new IOException("Netinkamas leidimo failas.");}return "SARGON|"+m.Version+"\n"+string.Concat(m.Files.OrderBy(x=>x.Name,StringComparer.Ordinal).Select(x=>x.Name+"|"+x.Hash.ToLowerInvariant()+"\n"));}
  public static void Verify(string directory,string certificateSha256,int minimumVersion){
   FolderMonitor.NoLinks(directory);string manifest=Path.Combine(directory,"release.json");FolderMonitor.NoLinks(manifest);var m=Data.Read<ServiceRelease>(manifest,65536);string canonical=Canonical(m);if(m.Version<minimumVersion)throw new IOException("Senesnis leidimas atmestas.");
   if(!Data.Hash(certificateSha256))throw new IOException("Nėra patikimo leidėjo rakto atspaudo.");string certFile=Path.Combine(directory,"publisher.cer");FolderMonitor.NoLinks(certFile);byte[] bytes;using(var input=File.OpenRead(certFile))bytes=Data.Bounded(input,16384,null);if(!string.Equals(Data.Digest(bytes),certificateSha256,StringComparison.OrdinalIgnoreCase))throw new IOException("Leidėjo raktas neatitinka patikimo atspaudo.");
   using(var cert=new X509Certificate2(bytes))using(RSA rsa=cert.GetRSAPublicKey()){if(rsa==null||rsa.KeySize<2048||!rsa.VerifyData(Encoding.UTF8.GetBytes(canonical),Convert.FromBase64String(m.Signature??""),HashAlgorithmName.SHA256,RSASignaturePadding.Pkcs1))throw new IOException("Leidimo parašas nepatvirtintas.");}
   VerifyFiles(directory,m);
  }
  public static void VerifyFiles(string directory,ServiceRelease m){Canonical(m);foreach(var file in m.Files){string p=Path.Combine(directory,file.Name);FolderMonitor.NoLinks(p);using(var stream=File.OpenRead(p))using(var sha=SHA256.Create()){if(!string.Equals(Data.Hex(sha.ComputeHash(stream)),file.Hash,StringComparison.OrdinalIgnoreCase))throw new IOException("Leidimo failo vientisumas nepatvirtintas: "+file.Name);}}}
 }
}
