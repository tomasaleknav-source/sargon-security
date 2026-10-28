using System;
using System.Collections;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace SargasPC
{
	public sealed class ReleaseUpdate
	{
		public UpdateOffer Offer;
		public string ReleasePage;
		public string InstallerUrl;
	}

	public static class ReleaseUpdateClient
	{
		private const string Api = "https://api.github.com/repos/tomasaleknav-source/sargon-security/releases/latest";
		private const string AssetPrefix = "https://github.com/tomasaleknav-source/sargon-security/releases/download/";
		private const string InstallerName = "SARGON_Security_PC_Setup.exe";
		private const long MaxManifest = 4096;
		private const long MaxInstaller = 536870912;

		private static HttpWebResponse Get(Uri uri, Action check)
		{
			if (uri == null || uri.Scheme != Uri.UriSchemeHttps || (uri.Host != "api.github.com" && uri.Host != "github.com"))
				throw new IOException("Atnaujinimo HTTPS adresas nepatvirtintas");
			check();
			HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
			request.Method = "GET";
			request.Accept = "application/vnd.github+json, application/octet-stream";
			request.UserAgent = "SARGON-Security-Updater/1.0";
			request.Timeout = 20000;
			request.ReadWriteTimeout = 20000;
			request.AllowAutoRedirect = true;
			try
			{
				HttpWebResponse response = (HttpWebResponse)request.GetResponse();
				string host = response.ResponseUri.Host;
				if (response.ResponseUri.Scheme != Uri.UriSchemeHttps ||
					(host != "api.github.com" && host != "github.com" && host != "release-assets.githubusercontent.com" && host != "objects.githubusercontent.com"))
				{
					response.Dispose();
					throw new IOException("Atnaujinimo atsisiuntimas peradresuotas į nepatvirtintą HTTPS serverį");
				}
				return response;
			}
			catch (WebException e)
			{
				if (e.Response != null) e.Response.Dispose();
				throw new IOException("Nepavyko pasiekti viešo SARGON leidimo. Atnaujinimas neatliktas.", e);
			}
		}

		private static byte[] ReadBounded(Uri uri, int limit, Action check)
		{
			using (HttpWebResponse response = Get(uri, check))
			using (Stream input = response.GetResponseStream())
			using (MemoryStream output = new MemoryStream())
			{
				byte[] buffer = new byte[8192];
				int read;
				while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
				{
					check();
					if (output.Length + read > limit) throw new IOException("Atnaujinimo atsakymas viršijo dydžio ribą");
					output.Write(buffer, 0, read);
				}
				return output.ToArray();
			}
		}

		private static IDictionary JsonObject(byte[] bytes)
		{
			if (bytes == null || bytes.Length < 2 || bytes.Length > 262144) throw new IOException("Netinkamas GitHub leidimo atsakymo dydis");
			try
			{
				object parsed = Data.Json().DeserializeObject(Encoding.UTF8.GetString(bytes));
				IDictionary result = parsed as IDictionary;
				if (result == null) throw new IOException("Netinkamas GitHub leidimo atsakymas");
				return result;
			}
			catch (IOException) { throw; }
			catch (Exception e) { throw new IOException("GitHub leidimo atsakymo nepavyko perskaityti", e); }
		}

		private static string Text(IDictionary value, string key)
		{
			object raw = value[key];
			string text = raw as string;
			if (string.IsNullOrEmpty(text) || text.Length > 2048) throw new IOException("Trūksta GitHub leidimo lauko: " + key);
			return text;
		}

		private static Uri AssetUrl(IDictionary asset, string expectedName)
		{
			if (Text(asset, "name") != expectedName) throw new IOException("Netinkamas SARGON leidimo failo vardas");
			Uri uri;
			if (!Uri.TryCreate(Text(asset, "browser_download_url"), UriKind.Absolute, out uri) ||
				uri.Scheme != Uri.UriSchemeHttps || uri.Host != "github.com" || uri.Query.Length != 0 || uri.Fragment.Length != 0 ||
				!uri.AbsolutePath.StartsWith("/tomasaleknav-source/sargon-security/releases/download/", StringComparison.Ordinal) ||
				!uri.AbsolutePath.EndsWith("/" + expectedName, StringComparison.Ordinal))
				throw new IOException("GitHub leidimo failo adresas neatitinka šios saugyklos");
			return uri;
		}

		public static ReleaseUpdate CheckLatest(byte[] publisher, long installedVersion, long now, Action check)
		{
			if (publisher == null || publisher.Length == 0) throw new IOException("Pasirašymo viešasis raktas nesukonfigūruotas");
			IDictionary release = JsonObject(ReadBounded(new Uri(Api), 262144, check));
			if (!(release["draft"] is bool) || (bool)release["draft"] || !(release["prerelease"] is bool) || (bool)release["prerelease"])
				throw new IOException("Naujausias GitHub įrašas nėra stabilus viešas leidimas");
			string page = Text(release, "html_url");
			if (!page.StartsWith("https://github.com/tomasaleknav-source/sargon-security/releases/tag/", StringComparison.Ordinal))
				throw new IOException("GitHub leidimo puslapis neatitinka šios saugyklos");
			string tag = Text(release, "tag_name");
			Match tagVersion = Regex.Match(tag, "^v([0-9]{1,10})$");
			long publishedVersion;
			if (!tagVersion.Success || !long.TryParse(tagVersion.Groups[1].Value, out publishedVersion) || publishedVersion > int.MaxValue)
				throw new IOException("GitHub leidimo žyma netinkama");
			if (publishedVersion <= installedVersion) return null;
			IList assets = release["assets"] as IList;
			if (assets == null || assets.Count > 100) throw new IOException("GitHub leidimo failų sąrašas netinkamas");
			IDictionary manifestAsset = null, installerAsset = null;
			foreach (object item in assets)
			{
				IDictionary asset = item as IDictionary;
				if (asset == null) continue;
				string name = asset["name"] as string;
				if (name == "sargon-update.manifest") manifestAsset = asset;
				if (name == InstallerName) installerAsset = asset;
			}
			if (manifestAsset == null || installerAsset == null) throw new IOException("Viešame leidime nėra manifesto arba diegiklio");
			Uri manifestUrl = AssetUrl(manifestAsset, "sargon-update.manifest");
			Uri installerUrl = AssetUrl(installerAsset, InstallerName);
			byte[] envelope = ReadBounded(manifestUrl, (int)MaxManifest, check);
			UpdateOffer offer = UpdateGate.Manifest(envelope, publisher, "windows", installedVersion, now);
			if (offer.Version != publishedVersion) throw new IOException("Pasirašyta versija nesutampa su GitHub leidimo žyma");
			return new ReleaseUpdate { Offer = offer, ReleasePage = page, InstallerUrl = installerUrl.AbsoluteUri };
		}

		public static string DownloadVerified(ReleaseUpdate update, byte[] publisher, string root, Action check)
		{
			if (update == null || update.Offer == null || update.Offer.Bytes < 1 || update.Offer.Bytes > MaxInstaller) throw new IOException("Naujinimo pasiūlymas netinkamas");
			Uri uri;
			if (!Uri.TryCreate(update.InstallerUrl, UriKind.Absolute, out uri)) throw new IOException("Diegiklio adresas netinkamas");
			Uri expected = AssetUrl(new System.Collections.Hashtable { { "name", InstallerName }, { "browser_download_url", update.InstallerUrl } }, InstallerName);
			Directory.CreateDirectory(root);
			if ((File.GetAttributes(root) & FileAttributes.ReparsePoint) != 0) throw new IOException("Atnaujinimo aplankas neturi būti nuoroda");
			string temp = Path.Combine(root, "sargon-update-" + Guid.NewGuid().ToString("N") + ".download");
			try
			{
				using (HttpWebResponse response = Get(expected, check))
				using (Stream input = response.GetResponseStream())
				using (FileStream output = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None))
				using (SHA256 sha = SHA256.Create())
				{
					byte[] buffer = new byte[65536];
					long total = 0;
					int read;
					while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
					{
						check();
						total += read;
						if (total > update.Offer.Bytes || total > MaxInstaller) throw new IOException("Diegiklis viršijo manifesto dydžio ribą");
						sha.TransformBlock(buffer, 0, read, buffer, 0);
						output.Write(buffer, 0, read);
					}
					sha.TransformFinalBlock(new byte[0], 0, 0);
					output.Flush(true);
					if (total != update.Offer.Bytes || Data.Hex(sha.Hash) != update.Offer.Hash) throw new IOException("Atsisiųsto diegiklio dydis arba SHA-256 nesutampa su pasirašytu manifestu");
				}
				string final = Path.Combine(root, "SARGON_Security_PC_Setup_" + update.Offer.Version + ".exe");
				if (File.Exists(final)) File.Delete(final);
				File.Move(temp, final);
				return final;
			}
			catch
			{
				try { if (File.Exists(temp)) File.Delete(temp); } catch { }
				throw;
			}
		}
	}
}
