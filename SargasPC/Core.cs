using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Cache;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.Win32.SafeHandles;

namespace SargasPC
{
	public sealed partial class Core
	{
		private struct Disposition
		{
			public byte Delete;
		}

		public const long MaxFile = 268435456L;

		public const long MaxVault = 8388608L;

		public string BundledHashPath = "";
        public string EngineBinaryDirectory = "";
        public string EngineDatabaseDirectory = "";
        public bool SharedReadOnlyEngine;

		private readonly object seedLock = new object();

		private string[] parsedSeed;

		public readonly string Root;

		public Settings Config;

		public volatile bool Cancelled;
        public Func<bool> ExternalCancellation=()=>false;
        public bool IsCancellationRequested {get{return Cancelled||ExternalCancellation();}}

		public volatile bool Paused;

		public volatile string VisualEvent = "idle";

		public readonly List<Witness> SessionFindings = new List<Witness>();

		public Action<string> Progress = delegate
		{
		};

		private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("SargasPC-012-local-user");

		public long ReadBytes;

		public long PausedMs;

		public int HashFindings;

		public int EngineFindings;

		public int TestFindings;

		public int NotChecked;
		public int ArchiveReviews, LocalEngineCompleted;
		private readonly HashSet<Witness> pendingArchiveScope = new HashSet<Witness>();

		private static readonly string[] ActionStates = new string[6] { "REQUESTED", "COPY_VERIFIED", "REMOVAL_CONFIRMED", "UNCONFIRMED", "RESTORED", "COPY_REMOVED" };

		private string PlanPath => Path.Combine(Root, "folder-plan.json");

		public string EngineRoot => Path.Combine(Root, "clamav");

		public bool HasLocalEngine => File.Exists(EngineExe("clamscan.exe")) && File.Exists(EngineExe("freshclam.exe"));

		public string EngineDatabase => string.IsNullOrEmpty(EngineDatabaseDirectory)?Path.Combine(EngineRoot, "database"):EngineDatabaseDirectory;

		public Core(string root)
		{
			Root = root;
			Directory.CreateDirectory(Root);
			Directory.CreateDirectory(Path.Combine(Root, "vault"));
			string path = Path.Combine(Root, "settings.json");
			Config = ((!File.Exists(path)) ? new Settings() : Data.Read<Settings>(path, 16384));
			if (Config == null)
			{
				throw new IOException("Nustatymai neperskaitomi; duomenys neperrašyti");
			}
		}

		public void Reset()
		{
            AttemptPath="";AttemptLimit=0;
			pendingArchiveScope.Clear(); ArchiveReviews=LocalEngineCompleted=0;
			VisualEvent = "idle";
			SessionFindings.Clear();
			Cancelled = false;
			Paused = false;
			ReadBytes = (PausedMs = 0L);
			HashFindings = (EngineFindings = (TestFindings = (NotChecked = 0)));
		}

		public void Check()
		{
			if (IsCancellationRequested)
			{
				throw new OperationCanceledException();
			}
			Stopwatch stopwatch = Stopwatch.StartNew();
			try
			{
				while (Paused)
				{
					Thread.Sleep(100);
					if (IsCancellationRequested)
					{
						throw new OperationCanceledException();
					}
				}
			}
			finally
			{
				PausedMs += stopwatch.ElapsedMilliseconds;
			}
		}

		public void SaveSettings()
		{
			Data.Atomic(Path.Combine(Root, "settings.json"), Data.Json().Serialize(Config));
		}

		public Dictionary<string, string> Secrets()
		{
			return ReadKeys();
		}

		public void SaveKeys(string api, string server)
		{
			Dictionary<string, string> dictionary = Secrets();
			if (api.Length > 0)
			{
				dictionary["api"] = Data.Token(api);
				Config.FeedAt = 0L;
				Config.LookupAt = 0L;
			}
			if (server.Length > 0)
			{
				dictionary["server"] = Data.Token(server);
				Config.HealthOk = false;
			}
			byte[] inArray = ProtectedData.Protect(Encoding.UTF8.GetBytes(Data.Json().Serialize(dictionary)), Entropy, DataProtectionScope.CurrentUser);
			string path = Path.Combine(Root, "keys.dpapi");
			Data.Atomic(path, Convert.ToBase64String(inArray));
		}

		public string Key(string name)
		{
			Dictionary<string, string> dictionary = ReadKeys();
			string value;
			return (!dictionary.TryGetValue(name, out value)) ? Data.Token("") : Data.Token(value);
		}

		private Dictionary<string, string> ReadKeys()
		{
			string path = Path.Combine(Root, "keys.dpapi");
			if (!File.Exists(path))
			{
				return new Dictionary<string, string>();
			}
			byte[] bytes;
			using (FileStream input = File.OpenRead(path))
			{
				bytes = Data.Bounded(input, 16384, Check);
			}
			byte[] encryptedData = Convert.FromBase64String(Encoding.UTF8.GetString(bytes));
			return Data.Json().Deserialize<Dictionary<string, string>>(Encoding.UTF8.GetString(ProtectedData.Unprotect(encryptedData, Entropy, DataProtectionScope.CurrentUser)));
		}

		public void ClearKeys()
		{
			string path = Path.Combine(Root, "keys.dpapi");
			if (File.Exists(path))
			{
				File.Delete(path);
			}
			Config.FeedAt = (Config.LookupAt = 0L);
			Config.HealthOk = false;
			SaveSettings();
		}

		public HashSet<string> Database()
		{
			HashSet<string> hashSet = LoadSignedHashes();
			if (BundledHashPath.Length > 0)
			{
				byte[] array;
				using (FileStream input = File.OpenRead(BundledHashPath))
				{
					array = Data.Bounded(input, 8388608, Check);
				}
				using (SHA256 sHA = SHA256.Create())
				{
					if (Data.Hex(sHA.ComputeHash(array)) != "fa2a3304eb92e9f18cab3d53aa161398b07a059ddc866c2f35945e72c5b6e0ee")
					{
						throw new IOException("Pradinė bazė pakeista arba sugadinta");
					}
				}
				lock (seedLock)
				{
					if (parsedSeed == null)
					{
						using (StringReader r = new StringReader(Encoding.UTF8.GetString(array)))
						{
							parsedSeed = Data.ParseHashes(r).ToArray();
						}
					}
					hashSet.UnionWith(parsedSeed);
				}
			}
			string path = Path.Combine(Root, "manual.hdb");
			if (File.Exists(path))
			{
				using (StreamReader r2 = new StreamReader(path, Encoding.UTF8))
				{
					hashSet.UnionWith(Data.ParseHashes(r2));
				}
			}
			string path2 = Path.Combine(Root, "community.json");
			if (File.Exists(path2))
			{
				Dictionary<string, long> dictionary = Data.Read<Dictionary<string, long>>(path2, 1500000);
				if (dictionary == null || dictionary.Count > 10000)
				{
					throw new IOException("Bendruomenės bazė nepatvirtinta");
				}
				long ticks = DateTime.UtcNow.Ticks;
				foreach (KeyValuePair<string, long> item in dictionary)
				{
					if (!Data.Hash(item.Key))
					{
						throw new IOException("Sugadinta bendruomenės bazė");
					}
					if (item.Value > 0 && item.Value <= ticks && ticks - item.Value <= TimeSpan.FromDays(30.0).Ticks)
					{
						hashSet.Add(item.Key);
					}
				}
			}
			return hashSet;
		}

		public int Import(string path)
		{
			return ImportHashes(path, append: false);
		}

		public int AppendHashes(string path)
		{
			return ImportHashes(path, append: true);
		}

		private int ImportHashes(string path, bool append)
		{
			if (new FileInfo(path).Length > 8388608)
			{
				throw new IOException("Sąrašas viršija 8 MiB");
			}
			HashSet<string> hashSet;
			using (StreamReader r = new StreamReader(path, Encoding.UTF8))
			{
				hashSet = Data.ParseHashes(r);
			}
			if (append)
			{
				string path2 = Path.Combine(Root, "manual.hdb");
				if (File.Exists(path2))
				{
					using (StreamReader r2 = new StreamReader(path2, Encoding.UTF8))
					{
						hashSet.UnionWith(Data.ParseHashes(r2));
					}
				}
				if (hashSet.Count > 100000)
				{
					throw new IOException("Bendra rankinės bazės riba: 100000 sumų");
				}
			}
			Check();
			Data.Atomic(Path.Combine(Root, "manual.hdb"), string.Join("\n", hashSet.OrderBy((string x) => x)));
			Config.ImportAt = DateTime.UtcNow.Ticks;
			SaveSettings();
			return hashSet.Count;
		}

		public string CheckLocalFile(string path, out Witness witness)
		{
			witness = null;
			HashSet<string> db = Database();
			Witness witness2 = Scan(path, 268435456L);
			string result = Describe(witness2, db);
			witness = witness2;
			return result;
		}

		public Witness Scan(string path, long maximum = 268435456L, bool lowImpact = false)
		{
			if (maximum < 1 || maximum > 268435456)
			{
				throw new ArgumentOutOfRangeException("maximum");
			}
			Check();
			path = Path.GetFullPath(path);
            AttemptPath=path;AttemptLimit=maximum;
			FolderMonitor.NoLinks(path);
			FileInfo fileInfo = new FileInfo(path);
			long ticks = fileInfo.LastWriteTimeUtc.Ticks;
			long length = fileInfo.Length;
			if ((fileInfo.Attributes & FileAttributes.ReparsePoint) != 0)
			{
				throw new IOException("Nuorodos ir reparse failai praleidžiami");
			}
			Stopwatch stopwatch = Stopwatch.StartNew();
			long pausedMs = PausedMs;
			string archive = "";
			bool test = false;
			long length2;
			string hash;
			using (FileStream fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 65536, FileOptions.SequentialScan))
			{
				length2 = fileStream.Length;
				if (length2 > maximum)
				{
					throw new IOException("Failas viršija šios patikros " + ((maximum >= 1048576) ? (maximum / 1048576 + " MiB") : (maximum + " baitų")) + " ribą");
				}
				using (SHA256 sHA = SHA256.Create())
				{
					byte[] array = new byte[65536];
					byte[] array2 = new byte[68];
					int num = 0;
					long num2 = 0L;
					int num3;
					while ((num3 = fileStream.Read(array, 0, array.Length)) > 0)
					{
						Check();
						int num4 = Math.Min(num3, 68 - num);
						if (num4 > 0)
						{
							Array.Copy(array, 0, array2, num, num4);
							num += num4;
						}
						num2 += num3;
						ReadBytes += num3;
						if (num2 > maximum || stopwatch.ElapsedMilliseconds - (PausedMs - pausedMs) > 90000)
						{
							throw new IOException("Failo dydžio arba 90 s sesijos riba; nepatikrinta");
						}
						sHA.TransformBlock(array, 0, num3, array, 0);
						if (num2 % 1048576 == 0)
						{
							Progress("Skaitoma: " + num2 / 1048576 + " / " + length2 / 1048576 + " MiB");
						}
						if (lowImpact || num2 % 1048576 == 0)
						{
							Thread.Sleep(2);
						}
					}
					sHA.TransformFinalBlock(new byte[0], 0, 0);
					hash = Data.Hex(sHA.Hash);
					test = Data.Marker(array2.Take(num).ToArray());
				}
				archive = ArchiveReview.Inspect(fileStream, path, Check);
			}
			fileInfo.Refresh();
			if (fileInfo.Length != length2 || length != length2 || fileInfo.LastWriteTimeUtc.Ticks != ticks)
			{
				throw new IOException("Failas pasikeitė skaitant; rezultatas nepatvirtintas");
			}
			Witness witness = new Witness();
			witness.Path = path;
			witness.Hash = hash;
			witness.Size = length2;
			witness.WriteTicks = fileInfo.LastWriteTimeUtc.Ticks;
			witness.Test = test;
			witness.Archive = archive;
			return witness;
		}

		public static bool TestMarker(Witness w)
		{
			return w.Test;
		}

		public string Describe(Witness w, HashSet<string> db)
		{
			if (!string.IsNullOrEmpty(w.Archive) && pendingArchiveScope.Add(w))
			{
				ArchiveReviews++; NotChecked++;
			}
			bool flag = TestMarker(w);
			bool flag2 = db.Contains(w.Hash);
			w.Kind = (flag ? "TEST" : ((!flag2) ? "NONE" : "HASH"));
			if (flag)
			{
				TestFindings++;
			}
			else if (flag2)
			{
				HashFindings++;
			}
			if ((flag || flag2) && !SessionFindings.Any((Witness x) => x.Path == w.Path && x.Hash == w.Hash) && SessionFindings.Count < 100)
			{
				SessionFindings.Add(w);
			}
			if (flag || flag2)
			{
				Finding(w, (!flag) ? "Tiksli sumos atitiktis vietinei bazei" : "EICAR bandymo žymuo", (!flag) ? "Rankinis / MalwareBazaar sumų sąrašas" : "Vietinis bandymo tekstas");
			}
			return w.Path + "\r\nSHA-256: " + w.Hash + "\r\nBaitų: " + w.Size + "\r\n" + (flag ? "EICAR BANDYMAS — tai nėra tikras virusas." : ((!flag2) ? "SUMOS ATITIKTIES NĖRA. Tai nėra saugaus failo verdiktas." : "SUMOS ATITIKTIS — tikrinkite kilmę; klaidingas perspėjimas galimas.")) + "\r\nBazėje sumų: " + db.Count + ". Keičiant failą suma pasikeičia; elgsena šia patikra neanalizuojama." + ((!string.IsNullOrEmpty(w.Archive)) ? ("\r\n\r\n" + w.Archive) : "");
		}

		public void Verify(Witness expected)
		{
			if (expected == null || expected.Size < 0 || expected.Size > 268435456)
			{
				throw new IOException("Netinkamas failo liudijimas");
			}
			Witness witness = Scan(expected.Path, Math.Max(1L, expected.Size));
			if (witness.Hash != expected.Hash || witness.Size != expected.Size || witness.WriteTicks != expected.WriteTicks)
			{
				throw new IOException("Failas pasikeitė. Pasirinkite ir tikrinkite jį iš naujo.");
			}
		}

		private void Finding(Witness w, string kind, string source)
		{
			string path = Path.Combine(Root, "findings.json");
			List<Dictionary<string, string>> list = ((!File.Exists(path)) ? new List<Dictionary<string, string>>() : Data.Read<List<Dictionary<string, string>>>(path, 262144));
			if (list == null)
			{
				throw new IOException("Radinių atmintinė sugadinta; neperrašyta");
			}
			list.Insert(0, new Dictionary<string, string>
			{
				{
					"subject",
					Data.Clean(Path.GetFileName(w.Path))
				},
				{ "hash", w.Hash },
				{ "kind", kind },
				{ "source", source },
				{
					"time",
					DateTime.UtcNow.ToString("o")
				}
			});
			Data.Atomic(path, Data.Json().Serialize(list.Take(100).ToList()));
		}

		public string Findings()
		{
			string path = Path.Combine(Root, "findings.json");
			return (!File.Exists(path)) ? "Radinių atmintinė tuščia. Tai nėra saugumo patvirtinimas." : Data.Json().Serialize(Data.Read<List<Dictionary<string, string>>>(path, 262144));
		}

		public void History(string report)
		{
			string path = Path.Combine(Root, "history.json");
			List<string> list = ((!File.Exists(path)) ? new List<string>() : Data.Read<List<string>>(path, 1500000));
			if (list == null)
			{
				throw new IOException("Istorija sugadinta; neperrašyta");
			}
			list.Insert(0, DateTime.Now.ToString("g") + "\r\n" + report.Substring(0, Math.Min(report.Length, 16000)));
			Data.Atomic(path, Data.Json().Serialize(list.Take(20).ToList()));
		}

		private HttpWebRequest Request(Uri url, string token, string header)
		{
			HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
			httpWebRequest.AllowAutoRedirect = false;
			httpWebRequest.Timeout = 15000;
			httpWebRequest.ReadWriteTimeout = 15000;
			httpWebRequest.Method = "POST";
			httpWebRequest.Headers[header] = Data.Token(token);
			return httpWebRequest;
		}

		public Dictionary<string, object> Post(Uri uri, string token, string header, byte[] bytes, string type)
		{
			HttpWebRequest httpWebRequest = Request(uri, token, header);
			httpWebRequest.ContentType = type;
			httpWebRequest.ContentLength = bytes.Length;
			try
			{
				using (Stream stream = httpWebRequest.GetRequestStream())
				{
					Check();
					stream.Write(bytes, 0, bytes.Length);
				}
				using (WebResponse webResponse = httpWebRequest.GetResponse())
				{
					using (Stream input = webResponse.GetResponseStream())
					{
						return Data.Json().Deserialize<Dictionary<string, object>>(Encoding.UTF8.GetString(Data.Bounded(input, 1500000, Check)));
					}
				}
			}
			catch (WebException error)
			{
                var response=error.Response as HttpWebResponse;
                bool feed=uri.Host=="mb-api.abuse.ch" && header=="Auth-Key";
                int code=response==null?0:(int)response.StatusCode;
                if(response!=null)response.Dispose();
                throw new IOException(FeedAccessPolicy.NetworkError(feed,code));
			}
			finally
			{
				httpWebRequest.Abort();
			}
		}

		public string UpdateFeed(bool lastHour = false)
		{
			Check();
			Dictionary<string, object> result = Post(new Uri("https://mb-api.abuse.ch/api/v1/"), RequireFeedKey(), "Auth-Key", Encoding.ASCII.GetBytes("query=get_recent&selector=" + ((!lastHour) ? "100" : "time")), "application/x-www-form-urlencoded");
			return ApplyFeed(result, lastHour);
		}

		internal string ApplyFeed(Dictionary<string, object> result, bool lastHour)
		{
			Check();
			string text = ((result == null || !result.ContainsKey("query_status")) ? "" : Convert.ToString(result["query_status"]));
			if (text == "no_results")
			{
				return "Tiekėjas šiai užklausai naujų įrašų nepateikė. Esama bazė nepakeista; tai nėra saugumo patvirtinimas.";
			}
			if (text != "ok" || !result.ContainsKey("data"))
			{
				throw new IOException(FeedAccessPolicy.ResponseError(text));
			}
			if (!(result["data"] is IList list) || list.Count > ((!lastHour) ? 100 : 10000))
			{
				throw new IOException("Netinkama naujausių sumų apimtis");
			}
			string path = Path.Combine(Root, "community.json");
			Dictionary<string, long> dictionary;
			try
			{
				dictionary = ((!File.Exists(path)) ? new Dictionary<string, long>() : Data.Read<Dictionary<string, long>>(path, 1500000));
			}
			catch (ArgumentException)
			{
				throw new IOException("Esama bendruomenės bazė sugadinta; neperrašyta");
			}
			catch (InvalidOperationException)
			{
				throw new IOException("Esama bendruomenės bazė sugadinta; neperrašyta");
			}
			if (dictionary == null || dictionary.Count > 10000 || dictionary.Any((KeyValuePair<string, long> x) => !Data.Hash(x.Key) || x.Value <= 0))
			{
				throw new IOException("Esama bendruomenės bazė sugadinta; neperrašyta");
			}
			long now = DateTime.UtcNow.Ticks;
			int num = 0;
			foreach (object item in list)
			{
				string text2 = ((!(item is Dictionary<string, object> dictionary2) || !dictionary2.ContainsKey("sha256_hash")) ? "" : Convert.ToString(dictionary2["sha256_hash"]));
				if (!Data.Hash(text2))
				{
					throw new IOException("Netinkama tiekėjo SHA-256 suma");
				}
				if (!dictionary.ContainsKey(text2))
				{
					num++;
				}
				dictionary[text2] = now;
			}
			dictionary = (from x in dictionary
				where x.Value <= now && now - x.Value <= TimeSpan.FromDays(30.0).Ticks
				orderby x.Value descending
				select x).ThenBy((KeyValuePair<string, long> x) => x.Key, StringComparer.Ordinal).Take(10000).ToDictionary((KeyValuePair<string, long> x) => x.Key, (KeyValuePair<string, long> x) => x.Value);
			Check();
			Data.Atomic(path, Data.Json().Serialize(dictionary));
			Config.FeedAt = now;
			SaveSettings();
			return "Gauta įrašų: " + list.Count + "; anksčiau nesaugotų sumų: " + num + ". Užklausa: " + ((!lastHour) ? "100 naujausių įrašų" : "paskutinės 60 minučių") + ". Po ribų pritaikymo saugoma: " + dictionary.Count + " (iki 10000 ir iki 30 dienų). Tai nėra visa grėsmių bazė. Transportas HTTPS; atskiro leidėjo parašo API neteikia.";
		}

		public string Reputation(Witness w)
		{
			Verify(w);
			long ticks = DateTime.UtcNow.Ticks;
			if (ticks < Config.LookupAt || ticks - Config.LookupAt < TimeSpan.FromSeconds(30.0).Ticks)
			{
				throw new IOException("Užklausa atidėta: tarp užklausų reikia bent 30 s");
			}
			string token = RequireFeedKey();
			Config.LookupAt = ticks;
			SaveSettings();
			Dictionary<string, object> dictionary = Post(new Uri("https://mb-api.abuse.ch/api/v1/"), token, "Auth-Key", Encoding.ASCII.GetBytes("query=get_info&hash=" + w.Hash), "application/x-www-form-urlencoded");
			Verify(w);
			string text = ((!dictionary.ContainsKey("query_status")) ? "" : Convert.ToString(dictionary["query_status"]));
			if (text == "hash_not_found")
			{
				return "TIEKĖJUI NEŽINOMA. Tai nėra saugaus failo verdiktas.\r\nSHA-256: " + w.Hash;
			}
			if (text != "ok")
			{
				throw new IOException("Tiekėjo atsakymas nepatvirtintas");
			}
			IList list = ((!dictionary.ContainsKey("data")) ? null : (dictionary["data"] as IList));
			if (list == null || list.Count != 1)
			{
				throw new IOException("Netinkama tiekėjo atsakymo apimtis");
			}
			if (!(list[0] is Dictionary<string, object> dictionary2) || !dictionary2.ContainsKey("sha256_hash") || Convert.ToString(dictionary2["sha256_hash"]) != w.Hash)
			{
				throw new IOException("Atsakymas neatitinka tikrintos sumos");
			}
			if (SessionFindings.Count < 100)
			{
				SessionFindings.Add(w);
			}
			w.Kind = ((!w.Test) ? "HASH" : "TEST");
			if (w.Test)
			{
				TestFindings++;
			}
			else
			{
				HashFindings++;
			}
			Finding(w, "Tiekėjo sumos atitiktis", "MalwareBazaar get_info HTTPS API");
			return "TIEKĖJO SUMOS ATITIKTIS\r\nSHA-256: " + w.Hash + "\r\nŽyma: " + Data.Clean((!dictionary2.ContainsKey("signature")) ? "" : Convert.ToString(dictionary2["signature"])) + "\r\nŠaltinis: MalwareBazaar get_info; " + DateTime.UtcNow.ToString("u") + "\r\nTai tiekėjo teiginys, ne viso kompiuterio užkrėtimo patvirtinimas. Siųsta tik suma.";
		}

		public string Server(Witness w)
		{
			Verify(w);
			if (w.Size > 33554432)
			{
				throw new IOException("Serverio riba 32 MiB");
			}
			Uri uri = new Uri(Data.Endpoint(Config.Endpoint), "v1/scan");
			HttpWebRequest httpWebRequest = Request(uri, Key("server"), "Authorization");
			httpWebRequest.Headers["Authorization"] = "Bearer " + Key("server");
			httpWebRequest.ContentType = "application/octet-stream";
			httpWebRequest.ContentLength = w.Size;
			try
			{
				using (FileStream fileStream = new FileStream(w.Path, FileMode.Open, FileAccess.Read, FileShare.Read))
				{
					using (SHA256 sHA = SHA256.Create())
					{
						if (Data.Hex(sHA.ComputeHash(fileStream)) != w.Hash)
						{
							throw new IOException("Failas pasikeitė");
						}
					}
					fileStream.Position = 0L;
					using (Stream stream = httpWebRequest.GetRequestStream())
					{
						byte[] array = new byte[65536];
						int count;
						while ((count = fileStream.Read(array, 0, array.Length)) > 0)
						{
							Check();
							stream.Write(array, 0, count);
						}
					}
				}
				Dictionary<string, object> dictionary;
				using (WebResponse webResponse = httpWebRequest.GetResponse())
				{
					using (Stream input = webResponse.GetResponseStream())
					{
						dictionary = Data.Json().Deserialize<Dictionary<string, object>>(Encoding.UTF8.GetString(Data.Bounded(input, 32768, Check)));
					}
				}
				if (!dictionary.ContainsKey("sha256") || Convert.ToString(dictionary["sha256"]) != w.Hash || !dictionary.ContainsKey("bytes") || Convert.ToInt64(dictionary["bytes"]) != w.Size)
				{
					throw new IOException("Serverio atsakymas nesutampa su siųstu failu");
				}
				string text = Convert.ToString(dictionary["verdict"]);
				if (!new string[3] { "detected", "not_detected", "unscannable" }.Contains(text))
				{
					throw new IOException("Nežinoma serverio išvada");
				}
				Verify(w);
				if (text == "detected")
				{
					if (SessionFindings.Count < 100)
					{
						SessionFindings.Add(w);
					}
					w.Kind = ((!w.Test) ? "ENGINE" : "TEST");
					if (w.Test)
					{
						TestFindings++;
					}
					else
					{
						EngineFindings++;
					}
					Finding(w, "Serverio variklio žyma", uri.Host + " / ClamAV");
				}
				if (text == "unscannable")
				{
					NotChecked++;
				}
				return "JŪSŲ CLAMAV SERVERIS: " + text + "\r\nSHA-256: " + w.Hash + "\r\nŽyma: " + Data.Clean((!dictionary.ContainsKey("signature")) ? "" : Convert.ToString(dictionary["signature"])) + "\r\nnot_detected nėra saugumo garantija. unscannable reiškia nepatikrinta išsamiai. Pasitikima jūsų serveriu; originalas nekeistas.";
			}
			catch (WebException)
			{
				throw new IOException("Serverio patikra nepatvirtinta. Patikrinkite HTTPS, raktą ir variklio bazę.");
			}
			finally
			{
				httpWebRequest.Abort();
			}
		}

		[DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		private static extern SafeFileHandle CreateFile(string path, uint access, uint share, IntPtr security, uint creation, uint flags, IntPtr template);

		[DllImport("kernel32.dll", SetLastError = true)]
		private static extern bool SetFileInformationByHandle(SafeFileHandle handle, int kind, ref Disposition value, uint length);

		public string Isolate(Witness w, bool deleteOriginal)
		{
			Check();
			if (deleteOriginal && w.Test)
			{
				throw new IOException("EICAR yra bandymo tekstas. Šis veiksmas jo originalo nešalina; galima tik šifruota kopija.");
			}
			if (w.Size > 8388608)
			{
				throw new IOException("Šifruotos kopijos riba 8 MiB");
			}
			string text = Path.Combine(Root, "vault");
			if (Directory.GetFiles(text, "*.dpapi").Length >= 10)
			{
				throw new IOException("Pasiekta 10 šifruotų kopijų riba");
			}
			if ((File.GetAttributes(w.Path) & FileAttributes.ReparsePoint) != 0)
			{
				throw new IOException("Nuorodos neizoliuojamos");
			}
			Journal(Path.GetFileName(w.Path), w.Hash, "REQUESTED");
			string text2 = Guid.NewGuid().ToString("N");
			string path = Path.Combine(text, text2 + ".dpapi");
			string path2 = Path.Combine(text, text2 + ".json");
			bool flag = false;
			bool flag2 = false;
			using (SafeFileHandle safeFileHandle = CreateFile(w.Path, (uint)(int.MinValue | (deleteOriginal ? 65536 : 0)), 1u, IntPtr.Zero, 3u, 136314880u, IntPtr.Zero))
			{
				if (safeFileHandle.IsInvalid)
				{
					throw new IOException("Failas užrakintas arba teisės neleidžia; originalas nekeistas");
				}
				using (FileStream input = new FileStream(safeFileHandle, FileAccess.Read))
				{
					byte[] array = Data.Bounded(input, 8388608, Check);
					if (array.LongLength != w.Size || Data.Digest(array) != w.Hash || File.GetLastWriteTimeUtc(w.Path).Ticks != w.WriteTicks)
					{
						throw new IOException("Originalas pasikeitė. Izoliavimas neatliktas");
					}
					byte[] array2 = ProtectedData.Protect(array, Entropy, DataProtectionScope.CurrentUser);
					try
					{
						using (FileStream fileStream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None))
						{
							fileStream.Write(array2, 0, array2.Length);
							fileStream.Flush(flushToDisk: true);
						}
						byte[] encryptedData;
						using (FileStream input2 = File.OpenRead(path))
						{
							encryptedData = Data.Bounded(input2, 10485760, Check);
						}
						byte[] b = ProtectedData.Unprotect(encryptedData, Entropy, DataProtectionScope.CurrentUser);
						if (Data.Digest(b) != w.Hash)
						{
							throw new IOException("Šifruotos kopijos patvirtinti nepavyko");
						}
						Check();
						VaultEntry vaultEntry = new VaultEntry();
						vaultEntry.Id = text2;
						vaultEntry.Name = Data.Clean(Path.GetFileName(w.Path));
						vaultEntry.Hash = w.Hash;
						vaultEntry.Bytes = w.Size;
						vaultEntry.Deleted = false;
						VaultEntry vaultEntry2 = vaultEntry;
						Data.Atomic(path2, Data.Json().Serialize(vaultEntry2));
						flag = true;
						Journal(vaultEntry2.Name, w.Hash, "COPY_VERIFIED");
						if (deleteOriginal)
						{
							Check();
							Disposition value = new Disposition
							{
								Delete = 1
							};
							flag2 = SetFileInformationByHandle(safeFileHandle, 4, ref value, 1u);
						}
					}
					finally
					{
						Array.Clear(array, 0, array.Length);
						if (!flag)
						{
							if (File.Exists(path))
							{
								File.Delete(path);
							}
							if (File.Exists(path2))
							{
								File.Delete(path2);
							}
						}
					}
				}
			}
			bool flag3 = deleteOriginal && flag2 && !File.Exists(w.Path);
			if (flag3)
			{
				VaultEntry vaultEntry3 = Data.Read<VaultEntry>(path2, 4096);
				vaultEntry3.Deleted = true;
				Data.Atomic(path2, Data.Json().Serialize(vaultEntry3));
			}
			if (deleteOriginal)
			{
				Journal(Path.GetFileName(w.Path), w.Hash, (!flag3) ? "UNCONFIRMED" : "REMOVAL_CONFIRMED");
			}
			return "Šifruota DPAPI kopija sukurta ir perskaičius patikrinta. ID: " + text2 + "\r\n" + (flag3 ? "Pasirinktas originalas pašalintas per patikrintą failo rankeną." : ((!deleteOriginal) ? "Originalas LIKO. Sukurta tik kopija." : "ORIGINALO PAŠALINIMAS NEPATVIRTINTAS. Kopija liko saugykloje; originalas gali būti pasiekiamas.")) + "\r\nKitos kopijos, hardlink nuorodos, procesai ir jau atlikta žala nepašalinti. DPAPI kopija priklauso šiai Windows paskyrai ir kompiuteriui; jos perkėlimas neatstoja atsarginės kopijos.";
		}

		public List<VaultEntry> Vault()
		{
			List<VaultEntry> list = new List<VaultEntry>();
			foreach (string item in Directory.GetFiles(Path.Combine(Root, "vault"), "*.json").Take(100))
			{
				VaultEntry vaultEntry = Data.Read<VaultEntry>(item, 4096);
				if (vaultEntry == null || vaultEntry.Id == null || vaultEntry.Id.Length != 32 || vaultEntry.Id.Any((char c) => !Uri.IsHexDigit(c)) || !Data.Hash(vaultEntry.Hash) || vaultEntry.Bytes < 0 || vaultEntry.Bytes > 8388608)
				{
					throw new IOException("Saugyklos įrašas nepatvirtintas");
				}
				list.Add(vaultEntry);
			}
			return list;
		}

		public void Restore(VaultEntry e, string path)
		{
			if (e.Id == null || e.Id.Length != 32 || e.Id.Any((char c) => !Uri.IsHexDigit(c)))
			{
				throw new IOException("Netinkamas kopijos ID");
			}
			byte[] encryptedData;
			using (FileStream input = File.OpenRead(Path.Combine(Root, "vault", e.Id + ".dpapi")))
			{
				encryptedData = Data.Bounded(input, 10485760, Check);
			}
			byte[] array = ProtectedData.Unprotect(encryptedData, Entropy, DataProtectionScope.CurrentUser);
			try
			{
				if (array.LongLength != e.Bytes || Data.Digest(array) != e.Hash)
				{
					throw new IOException("Kopijos turinys nesutampa su įrašu");
				}
				Check();
				Journal(e.Name, e.Hash, "REQUESTED");
				try
				{
					Data.RestoreNew(path, array, e.Hash, Check);
					Journal(e.Name, e.Hash, "RESTORED");
				}
				catch (Exception ex)
				{
					try
					{
						Journal(e.Name, e.Hash, "UNCONFIRMED");
					}
					catch
					{
					}
					if (ex is OperationCanceledException)
					{
						throw;
					}
					throw new IOException("Atkūrimas nepatvirtintas. Šifruota kopija palikta. Patikrinkite paskirties vietą; po sutrikimo gali likti .sargas-restore-*.tmp failas.", ex);
				}
			}
			finally
			{
				Array.Clear(array, 0, array.Length);
			}
		}

		public void DeleteCopy(VaultEntry entry)
		{
			if (entry.Id == null || entry.Id.Length != 32 || entry.Id.Any((char c) => !Uri.IsHexDigit(c)))
			{
				throw new IOException("Netinkamas kopijos ID");
			}
			Journal(entry.Name, entry.Hash, "REQUESTED");
			string path = Path.Combine(Root, "vault");
			File.Delete(Path.Combine(path, entry.Id + ".dpapi"));
			File.Delete(Path.Combine(path, entry.Id + ".json"));
			Journal(entry.Name, entry.Hash, "COPY_REMOVED");
		}

		public string PastReports()
		{
			string path = Path.Combine(Root, "history.json");
			return (!File.Exists(path)) ? "Patikrų istorija tuščia." : string.Join("\r\n\r\n-----\r\n\r\n", Data.Read<List<string>>(path, 1500000));
		}

		public static string ExplainDefender(string json)
		{
			Dictionary<string, object> dictionary = Data.Json().Deserialize<Dictionary<string, object>>(json);
			if (dictionary == null || !dictionary.ContainsKey("AMServiceEnabled") || !(dictionary["AMServiceEnabled"] is bool) || !dictionary.ContainsKey("AntivirusEnabled") || !(dictionary["AntivirusEnabled"] is bool) || !dictionary.ContainsKey("RealTimeProtectionEnabled") || !(dictionary["RealTimeProtectionEnabled"] is bool))
			{
				throw new IOException("Defender būsenos formatas netinkamas");
			}
			return (!(bool)dictionary["AMServiceEnabled"] || !(bool)dictionary["AntivirusEnabled"] || !(bool)dictionary["RealTimeProtectionEnabled"]) ? "Defender nepatvirtino visų aktyvios apsaugos požymių. Kita antivirusinė (pvz. McAfee) gali būti pagrindinis teikėjas; iš šių duomenų tai nenustatoma. Atverkite Windows saugumą. Tai nėra teiginys, kad kompiuteris neapsaugotas." : "Defender praneša, kad jo paslauga, antivirusinė ir apsauga realiu laiku įjungtos. Tai ne naujos virusų patikros rezultatas.";
		}

		public string DefenderStatus()
		{
			try
			{
				string text = PowerShell("$ErrorActionPreference='Stop'; try{Import-Module Defender}catch{exit 41}; try{$s=Get-MpComputerStatus}catch{exit 42}; $s | Select-Object AMServiceEnabled,AntivirusEnabled,RealTimeProtectionEnabled,BehaviorMonitorEnabled,IoavProtectionEnabled,AMRunningMode,AntivirusSignatureVersion,AntivirusSignatureLastUpdated,AntivirusSignatureAge,QuickScanAge,FullScanAge | ConvertTo-Json -Compress", "", 15000);
				return "MICROSOFT DEFENDER BŪSENA (nuskaityta iš Windows)\r\n" + ExplainDefender(text) + "\r\n" + text + "\r\nŠie duomenys rodo Defender būseną, ne garantiją, kad grėsmių nėra. Windows saugume patikrinkite aktyvų apsaugos teikėją.";
			}
			catch (CommandFailure commandFailure)
			{
				throw new IOException("Defender būsena neperskaityta. " + Data.Diagnostic(commandFailure) + ((commandFailure.Problem == CommandProblem.ExitCode && commandFailure.Code == 41) ? " Nepavyko įkelti Defender modulio." : ((commandFailure.Problem != CommandProblem.ExitCode || commandFailure.Code != 42) ? "" : " Nepavyko vykdyti Defender būsenos užklausos.")) + " Atverkite Windows saugumą ir tikrinkite aktyvų apsaugos teikėją. Esamos antivirusinės neišjunkite.", commandFailure);
			}
		}

		public string Defender(Witness w)
		{
			if (w != null)
			{
				Verify(w);
			}
			string script = "$ErrorActionPreference='Stop'; Import-Module Defender; $s=Get-MpComputerStatus; if(-not $s.AMServiceEnabled -or -not $s.AntivirusEnabled){throw 'Defender is not the active antivirus provider'}; if($env:SARGAS_SCAN_PATH){Start-MpScan -ScanType CustomScan -ScanPath $env:SARGAS_SCAN_PATH}else{Start-MpScan -ScanType QuickScan}; $s=Get-MpComputerStatus; [pscustomobject]@{ScanRequested='QuickScan or CustomScan';Engine=$s.AMEngineVersion;Signature=$s.AntivirusSignatureVersion;SignatureUpdated=$s.AntivirusSignatureLastUpdated;RealTime=$s.RealTimeProtectionEnabled;Completed=(Get-Date).ToString('o')} | ConvertTo-Json -Compress";
			string text = PowerShell(script, (w != null) ? w.Path : "", 600000);
			return "MICROSOFT DEFENDER PATIKROS UŽKLAUSA ĮVYKDYTA\r\n" + text + "\r\nTai Defender sistemos patikros užklausos ir būsenos patvirtinimas; ne atskiras teiginys, kad visi failai švarūs. Aptikimus ir veiksmus tikrinkite Windows saugumo apsaugos istorijoje.";
		}

		public string DefenderUpdate()
		{
			string script = "$ErrorActionPreference='Stop'; Import-Module Defender; $before=Get-MpComputerStatus; if(-not $before.AMServiceEnabled -or -not $before.AntivirusEnabled){throw 'Defender is not the active antivirus provider'}; Update-MpSignature; $s=Get-MpComputerStatus; [pscustomobject]@{SignatureVersion=$s.AntivirusSignatureVersion;SignatureUpdated=$s.AntivirusSignatureLastUpdated;SignatureAgeDays=$s.AntivirusSignatureAge;RealTimeProtection=$s.RealTimeProtectionEnabled;Completed=(Get-Date).ToString('o')} | ConvertTo-Json -Compress";
			return "MICROSOFT DEFENDER BAZIŲ ATNAUJINIMAS\r\n" + PowerShell(script, "", 180000) + "\r\nRezultatas paimtas iš Defender po atnaujinimo užklausos.";
		}

		public string DefenderFullScan()
		{
			string script = "$ErrorActionPreference='Stop'; Import-Module Defender; $s=Get-MpComputerStatus; if(-not $s.AMServiceEnabled -or -not $s.AntivirusEnabled){throw 'Defender is not the active antivirus provider'}; Start-MpScan -ScanType FullScan; $s=Get-MpComputerStatus; [pscustomobject]@{ScanRequested='FullScan';Signature=$s.AntivirusSignatureVersion;SignatureUpdated=$s.AntivirusSignatureLastUpdated;RealTimeProtection=$s.RealTimeProtectionEnabled;Completed=(Get-Date).ToString('o')} | ConvertTo-Json -Compress";
			return "MICROSOFT DEFENDER VISO KOMPIUTERIO PATIKRA\r\n" + PowerShell(script, "", 14400000) + "\r\nDefender pats nustato patikros radinius ir veiksmus. Peržiūrėkite Windows saugumo apsaugos istoriją.";
		}

		public string DefenderThreatHistory()
		{
			string script = "$ErrorActionPreference='Stop'; Import-Module Defender; Get-MpThreatDetection | Sort-Object InitialDetectionTime -Descending | Select-Object -First 30 InitialDetectionTime,ThreatID,ThreatStatusID,ActionSuccess,Resources | ConvertTo-Json -Compress -Depth 4";
			string text = PowerShell(script, "", 30000);
			return "DEFENDER GRĖSMIŲ ISTORIJA (iki 30 naujausių įrašų)\r\n" + (String.IsNullOrWhiteSpace(text) ? "Defender nepateikė grėsmių istorijos įrašų." : text) + "\r\nPilną informaciją ir veiksmus peržiūrėkite Windows saugumo apsaugos istorijoje.";
		}

		private string PowerShell(string script, string path, int timeout)
		{
			string fileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "System32", "WindowsPowerShell", "v1.0", "powershell.exe");
			ProcessStartInfo processStartInfo = new ProcessStartInfo(fileName, "-NoProfile -NonInteractive -EncodedCommand " + Convert.ToBase64String(Encoding.Unicode.GetBytes(script)));
			processStartInfo.UseShellExecute = false;
			processStartInfo.CreateNoWindow = true;
			processStartInfo.RedirectStandardOutput = true;
			processStartInfo.RedirectStandardError = true;
			ProcessStartInfo processStartInfo2 = processStartInfo;
			processStartInfo2.EnvironmentVariables["SARGAS_SCAN_PATH"] = path;
			return BoundedCommand.Run(processStartInfo2, timeout, () => IsCancellationRequested);
		}

		public string SelfCheck()
		{
			Config.SelfRunning = true;
			Config.SelfPassed = false;
			SaveSettings();
			StringBuilder report = new StringBuilder(string.Concat("PC VIETINĖ SAVITIKRA • SARGON Security 0.49\r\nVykdymo aplinka: ", (!(Type.GetType("Mono.Runtime") != null)) ? ".NET CLR " : "Mono ", Environment.Version, "; procesas ", IntPtr.Size * 8, " bitų.\r\n"));
			int failures = 0;
			Action<string, Action> action = delegate(string name, Action action2)
			{
				Check();
				try
				{
					action2();
					report.AppendLine("PRAĖJO: " + name);
				}
				catch (OperationCanceledException)
				{
					throw;
				}
				catch (Exception error2)
				{
					failures++;
					report.AppendLine("NEPATVIRTINTA: " + name + "\r\n  " + Data.Diagnostic(error2));
				}
			};
			action("SHA-256", delegate
			{
				if (Data.Digest(Encoding.ASCII.GetBytes("abc")) != "ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad")
				{
					throw new InvalidOperationException();
				}
			});
			action("EICAR bandymo žymuo atmintyje ir neigiamas tekstas", delegate
			{
				if (!Data.Marker(Encoding.ASCII.GetBytes("X5O!P%@AP[4\\PZX54(P^)7CC)7}$EICAR-STANDARD-ANTIVIRUS-TEST-FILE!$H+H*")) || Data.Marker(Encoding.ASCII.GetBytes("neutralus tekstas")))
				{
					throw new InvalidOperationException();
				}
			});
			action("SHA-256 tuščio turinio kontrolinė suma", delegate
			{
				if (Data.Digest(new byte[0]) != "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855")
				{
					throw new InvalidOperationException();
				}
			});
			action("SHA-256 sąrašo skaitymas (BOM, CRLF, dublikatai, netinkama eilutė)", Data.ProbeHashReader);
			action("Windows DPAPI šifravimas ir pakeistos kopijos atmetimas", delegate
			{
				byte[] bytes = Encoding.UTF8.GetBytes("Sargas local probe");
				byte[] array = ProtectedData.Protect(bytes, Entropy, DataProtectionScope.CurrentUser);
				if (!bytes.SequenceEqual(ProtectedData.Unprotect(array, Entropy, DataProtectionScope.CurrentUser)))
				{
					throw new InvalidOperationException();
				}
				array[array.Length - 1] ^= 1;
				bool flag = false;
				try
				{
					ProtectedData.Unprotect(array, Entropy, DataProtectionScope.CurrentUser);
				}
				catch (CryptographicException)
				{
					flag = true;
				}
				if (!flag)
				{
					throw new InvalidOperationException();
				}
			});
			action("Privatus rašymas ir skaitymas", delegate
			{
				string path = Path.Combine(Root, "probe-" + Guid.NewGuid().ToString("N"));
				try
				{
					Data.Atomic(path, "Sargas probe");
					if (File.ReadAllText(path) != "Sargas probe")
					{
						throw new InvalidOperationException();
					}
				}
				finally
				{
					if (File.Exists(path))
					{
						File.Delete(path);
					}
				}
			});
			action("Vietinės bazės formatas", delegate
			{
				report.AppendLine("Bazėje sumų: " + Database().Count);
			});
			report.AppendLine("\r\nPASIRENKAMA SISTEMOS APSAUGA (ne vietinės savitikros dalis)");
			try
			{
				report.AppendLine(DefenderStatus());
			}
			catch (OperationCanceledException)
			{
				throw;
			}
			catch (Exception error)
			{
				report.AppendLine("Defender būsena neperskaityta: " + Data.Diagnostic(error) + "\r\nTai neįrodo, kad kompiuteris neapsaugotas. Atverkite Windows saugumą ir tikrinkite aktyvų teikėją, pvz. McAfee. Esamos antivirusinės neišjunkite.");
			}
			report.AppendLine("Nepatvirtintų vietinių bandymų: " + failures + ". Defender ir kitų antivirusinių būsena į šį skaičių neįtraukta. Tai nėra tikrų virusų aptikimo ar viso PC saugumo garantija. DPAPI bandymas atliekamas tik jį paleidus jūsų Windows kompiuteryje.");
			report.AppendLine("MalwareBazaar: reikia savo Auth-Key ir sėkmingos užklausos. Vietiniam ClamAV reikia įdiegto variklio ir oficialių bazių; nuosavas HTTPS serveris nebūtinas. Defender: priklauso nuo Windows paslaugos / teisių. Šis leidimas bandomasis, nepasirašytas Authenticode. Atskira suplanuotų patikrų paslauga tikrinama bendros parengties lange. Failų blokavimo tvarkyklės ir apsaugos prieš paleidimą nėra. Esama apsauga neišjungiama.");
			NotChecked += failures;
			Config.SelfRunning = false;
			Config.SelfPassed = failures == 0;
			Config.SelfAt = DateTime.UtcNow.Ticks;
			Config.SelfVersion = 49;
			SaveSettings();
			return report.ToString();
		}

		private string DatabaseId(HashSet<string> db)
		{
			return Data.Digest(Encoding.ASCII.GetBytes(string.Join("\n", db.OrderBy((string x) => x, StringComparer.Ordinal))));
		}

		private FolderPlan LoadPlan()
		{
			FolderPlan folderPlan = Data.Read<FolderPlan>(PlanPath, 6291456);
			if (folderPlan == null || folderPlan.Root == null || folderPlan.Database == null || !Data.Hash(folderPlan.Database) || folderPlan.Pending == null || folderPlan.Pending.Count > 4096 || folderPlan.Done < 0 || folderPlan.Done > 4096 || folderPlan.Omitted < 0 || folderPlan.SkippedFiles < 0 || folderPlan.SkippedFiles>4096 || folderPlan.Done + folderPlan.SkippedFiles + folderPlan.Pending.Count > 4096 || folderPlan.At <= 0 || folderPlan.At > DateTime.UtcNow.Ticks)
			{
				throw new IOException("Eilė sugadinta; neperrašyta");
			}
			string fullPath = Path.GetFullPath(folderPlan.Root);
			if (fullPath != folderPlan.Root)
			{
				throw new IOException("Netinkama eilės šaknis");
			}
			string value = fullPath.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
			HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			foreach (string item in folderPlan.Pending)
			{
				if (item == null || item.Length > 1024 || Path.GetFullPath(item) != item || !item.StartsWith(value, StringComparison.OrdinalIgnoreCase) || !hashSet.Add(item))
				{
					throw new IOException("Netinkamas eilės kelias; eilė neperrašyta");
				}
			}
			return folderPlan;
		}

		private void SavePlan(FolderPlan q)
		{
			Data.Atomic(PlanPath, Data.Json().Serialize(q));
		}

		public string FolderQueueOverview()
		{
			if (!File.Exists(PlanPath))
			{
				return "Aplanko eilės nėra.";
			}
			try
			{
				FolderPlan folderPlan = LoadPlan();
				return "Aplanko eilė: liko " + folderPlan.Pending.Count + "; anksčiau perskaityta " + folderPlan.Done + "; ribota apimtis " + folderPlan.Omitted + "; praleistų failų: "+folderPlan.SkippedFiles+". Ankstesni failai iš naujo netikrinti.";
			}
			catch
			{
				return "Aplanko eilė nepatvirtinta; pradėkite naują patikrą.";
			}
		}

		public bool HasFolderPlan()
		{
			return File.Exists(PlanPath);
		}

		public string Folder(string root)
		{
			root = Path.GetFullPath(root);
			FolderPlan folderPlan = new FolderPlan();
			folderPlan.Root = root;
			folderPlan.Database = DatabaseId(Database());
			folderPlan.At = DateTime.UtcNow.Ticks;
			FolderPlan folderPlan2 = folderPlan;
			Stack<string> stack = new Stack<string>();
			stack.Push(root);
			int num = 0;
			int num2 = 0;
			Stopwatch stopwatch = Stopwatch.StartNew();
			bool flag = false;
			while (stack.Count > 0 && !flag)
			{
				Check();
				if (++num > 512 || stopwatch.ElapsedMilliseconds > 15000)
				{
					folderPlan2.Omitted++;
					break;
				}
				string path = stack.Pop();
				try
				{
					if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
					{
						folderPlan2.Omitted++;
						continue;
					}
					foreach (string item in Directory.EnumerateFileSystemEntries(path))
					{
						Check();
						if (++num2 > 8192 || stopwatch.ElapsedMilliseconds > 15000 || folderPlan2.Pending.Count >= 4096)
						{
							folderPlan2.Omitted++;
							flag = true;
							break;
						}
						try
						{
							FileAttributes attributes = File.GetAttributes(item);
							if ((attributes & FileAttributes.ReparsePoint) != 0)
							{
								folderPlan2.Omitted++;
							}
							else if ((attributes & FileAttributes.Directory) != 0)
							{
								if (stack.Count < 512)
								{
									stack.Push(item);
								}
								else
								{
									folderPlan2.Omitted++;
								}
							}
							else if (item.Length <= 1024)
							{
								folderPlan2.Pending.Add(Path.GetFullPath(item));
							}
							else
							{
								folderPlan2.Omitted++;
							}
						}
						catch (OperationCanceledException)
						{
							throw;
						}
						catch
						{
							folderPlan2.Omitted++;
						}
					}
				}
				catch (OperationCanceledException)
				{
					throw;
				}
				catch
				{
					folderPlan2.Omitted++;
				}
			}
			if (stack.Count > 0)
			{
				folderPlan2.Omitted++;
			}
			SavePlan(folderPlan2);
			return ContinueFolder(32, 120000L);
		}

		public string ContinueFolder(int maximum = 32, long milliseconds = 120000L)
		{
			if (maximum < 1 || maximum > 32 || milliseconds < 1 || milliseconds > 120000)
			{
				throw new ArgumentOutOfRangeException();
			}
			FolderPlan folderPlan = LoadPlan();
			HashSet<string> db = Database();
			if (folderPlan.Database != DatabaseId(db))
			{
				throw new IOException("Grėsmių bazė pasikeitė. Pradėkite naują aplanko patikrą; senoji eilė išsaugota.");
			}
			Stopwatch stopwatch = Stopwatch.StartNew();
			long pausedMs = PausedMs;
			int num = 0;
			int num2 = 0;
			int num3 = 0;
			int count = folderPlan.Pending.Count;
			StringBuilder stringBuilder = new StringBuilder();
			NotChecked = folderPlan.Pending.Count + folderPlan.Omitted;
			while (folderPlan.Pending.Count > 0 && num < maximum && num < count && stopwatch.ElapsedMilliseconds - (PausedMs - pausedMs) < milliseconds)
			{
				Check();
				string text = folderPlan.Pending[0];
				num++;
				try
				{
					EnsureParents(folderPlan.Root, text);
					FileAttempt attempt=TryReadFile(text,268435456L,false,"Rankinė aplanko sumų patikra");
                    if(attempt.Issue!=null){stringBuilder.AppendLine(attempt.Issue.Report());folderPlan.Pending.RemoveAt(0);if(attempt.Issue.Code=="TOO_LARGE"||attempt.Issue.Code=="UNSAFE_LINK"||attempt.Issue.Code=="NOT_FILE"||attempt.Issue.Code=="PATH_TOO_LONG"||attempt.Issue.Code=="INVALID_PATH"){folderPlan.Omitted++;folderPlan.SkippedFiles++;}else folderPlan.Pending.Add(text);SavePlan(folderPlan);NotChecked=folderPlan.Pending.Count+folderPlan.Omitted+num3;Progress("Šioje sesijoje perskaityta: "+num2+"; eilėje liko: "+folderPlan.Pending.Count);continue;}
                    Witness witness=attempt.Witness;
					string value = Describe(witness, db);
					if (witness.Kind != "NONE" || !string.IsNullOrEmpty(witness.Archive))
					{
						stringBuilder.AppendLine(value);
					}
					if (!string.IsNullOrEmpty(witness.Archive))
					{
						num3++;
					}
					folderPlan.Pending.RemoveAt(0);
					folderPlan.Done++;
					num2++;
				}
				catch (OperationCanceledException)
				{
					throw;
				}
				catch (IOException ex2)
				{
					folderPlan.Pending.RemoveAt(0);
					folderPlan.Pending.Add(text);
					stringBuilder.AppendLine(FileIssue.From(text,268435456L,"Rankinė aplanko sumų patikra",ex2).Report());
				}
				catch (UnauthorizedAccessException)
				{
					folderPlan.Pending.RemoveAt(0);
					folderPlan.Pending.Add(text);
					stringBuilder.AppendLine(FileIssue.From(text,268435456L,"Rankinė aplanko sumų patikra",new UnauthorizedAccessException()).Report());
				}
				SavePlan(folderPlan);
				NotChecked = folderPlan.Pending.Count + folderPlan.Omitted + num3;
				Progress("Šioje sesijoje perskaityta: " + num2 + "; eilėje liko: " + folderPlan.Pending.Count);
			}
			return string.Concat("APLANKO SUMŲ PATIKRA — iki 32 failų / 2 min. tarp failų\r\nŠioje sesijoje perskaityta: ", num2, "\r\n", FolderQueueOverview(), "\r\n", stringBuilder, "\r\nTęsti galima mygtuku „Tęsti aplanko eilę“. Išsaugoma pozicija tarp failų; nutrauktas failas skaitomas nuo pradžios. Sąrašas yra momentinė kopija: vėliau atsiradę failai neįtraukti, ankstesnių failų pakeitimai netikrinami. Vienas failas gali pratęsti sesiją iki savo 90 s ribos. Ribotos apimties skaičius reiškia ribotos apimties vietas, ne tikslų neperskaitytų failų skaičių. Jokio viso aplanko saugumo verdikto nėra.");
		}

		private static void EnsureParents(string root, string file)
		{
			string directoryName = Path.GetDirectoryName(file);
			bool flag = false;
			while (directoryName != null)
			{
				if ((File.GetAttributes(directoryName) & FileAttributes.ReparsePoint) != 0)
				{
					throw new FileScopeException("UNSAFE_LINK");
				}
				if (string.Equals(directoryName, root, StringComparison.OrdinalIgnoreCase))
				{
					flag = true;
				}
				directoryName = Path.GetDirectoryName(directoryName);
			}
			if (!flag)
			{
				throw new IOException("Kelias už eilės šaknies");
			}
		}

        public string Outcome(bool complete)
        {return !complete?"VEIKSMAS NEUŽBAIGTAS / NEPATVIRTINTAS":EngineFindings>0?"ANTIVIRUSINIO VARIKLIO RADINYS":HashFindings>0?"SUMOS ATITIKTIS — reikia peržiūros":TestFindings>0?"EICAR BANDYMAS — ne tikras virusas":NotChecked>0?"APIMTIS NEUŽBAIGTA":LocalEngineCompleted>0?"CLAMAV PATIKRA BAIGTA — grėsmių žymų neaptikta":"VEIKSMAS UŽBAIGTAS — skaitykite jo ribas";}

		public string Metrics(long wall, long cpu)
		{
			return "\r\nAPIMTIS IR APKROVA\r\nSHA-256 skaitytuvu perskaityta: " + ReadBytes + " baitų (įskaitant pakartotinius skaitymus). Tai SARGON SHA-256 skaitytuvo apimtis; ClamAV perskaityti baitai čia neskaičiuojami.\r\nTrukmė su pauzėmis: " + wall + " ms; pauzės: " + PausedMs + " ms; programos proceso CPU: " + ((cpu >= 0) ? (cpu + " ms") : "neprieinamas") + ".\r\nSumų atitiktys: " + HashFindings + "; variklio žymos: " + EngineFindings + "; bandymo žymenys: " + TestFindings + "; nepatikrinta / ribota apimtis: " + NotChecked + ".\r\nPapildomos ZIP metaduomenų peržiūros: "+ArchiveReviews+" (šioje dalyje turinys neišpakuojamas). Sėkmingos ClamAV patikros: "+LocalEngineCompleted+".\r\nCPU neapima atskirų ClamAV, FreshClam, Defender procesų ar serverio. Tai nėra baterijos ar sistemos spartos matavimas.";
		}

		public void SaveLast(string text)
		{
			Data.Atomic(Path.Combine(Root, "last-operation.json"), Data.Json().Serialize(DateTime.Now.ToString("g") + " • " + text.Substring(0, Math.Min(1400, text.Length))));
		}

		public string ModuleReadiness()
		{
			StringBuilder stringBuilder = new StringBuilder("KAS VEIKIA DABAR?\r\nBūsenos nėra viso kompiuterio saugumo įvertinimas.\r\n\r\n");
			long ticks = DateTime.UtcNow.Ticks;
			bool flag = Config.SelfPassed && !Config.SelfRunning && Config.SelfVersion == 48 && Config.SelfAt > 0 && Config.SelfAt <= ticks && ticks - Config.SelfAt < TimeSpan.FromHours(24.0).Ticks;
			stringBuilder.AppendLine("Windows Defender — PASIRENKAMA SISTEMOS PASLAUGA\r\nPatikrinkite per Defender būsenos mygtuką. Šis langas pats nekviečia PowerShell ir nepatvirtina paslaugos veikimo.\r\n");
			stringBuilder.AppendLine("Vietinė savitikra — " + ((!flag) ? "REIKIA PATIKROS" : "ATLIKTA") + "\r\nTik vietinių funkcijų bandymai; Defender / kitos antivirusinės būsena vertinama atskirai. Savitikra nėra tikrų virusų aptikimo įrodymas.\r\n");
			try
			{
				int count = Database().Count;
				stringBuilder.AppendLine("Vietinės sumos — " + ((count != 0) ? "RANKINIS TIKRINIMAS" : "REIKIA ŠALTINIO") + "\r\nSumų: " + count + ". Tai žinomų sumų atitiktys, ne visų virusų aptikimas.\r\n");
			}
			catch
			{
				stringBuilder.AppendLine("Vietinės sumos — DUOMENYS NEPATVIRTINTI\r\nBazė neperskaitoma; jos nevadiname tuščia ar parengta.\r\n");
			}
			stringBuilder.AppendLine("Vietinis variklis — " + LocalEngineStatus() + "\r\n");
			bool flag2 = Config.HealthOk && Config.HealthEndpoint == Config.Endpoint && Config.HealthAt > 0 && Config.HealthAt <= ticks && ticks - Config.HealthAt < TimeSpan.FromHours(24.0).Ticks;
			stringBuilder.AppendLine("ClamAV serveris — " + (string.IsNullOrEmpty(Config.Endpoint) ? "REIKIA SERVERIO" : ((!flag2) ? "RYŠYS NEPATVIRTINTAS" : "PASIRENKAMAS SIUNTIMAS")) + "\r\nAdresas ar raktas neįrodo veikiančio ryšio. Turinys siunčiamas tik atskirai patvirtinus.\r\n");
			stringBuilder.AppendLine("Nuolatinė apsauga — VISOS SISTEMOS APSAUGOS NĖRA\r\nPasirenkamas vieno aplanko stebėjimas nėra vykdymo blokavimo tvarkyklė ar elgesio analizė.\r\n");
			stringBuilder.AppendLine("Automatiniai atnaujinimai — NEĮJUNGTI\r\nRankinis leidėjo parašo tikrinimas nepakeičia veikiančios atnaujinimų paslaugos.\r\n");
			stringBuilder.AppendLine("Interneto apsauga — RANKINĖS TAISYKLĖS\r\nDomenų sąrašas ir atskiras plėtinys nėra nuolat prižiūrima reputacijos paslauga.\r\n");
			stringBuilder.AppendLine("Sparta ir kokybė — ĮRENGINIO BANDYMŲ REIKIA\r\nCPU ir perskaityti baitai nepatvirtina baterijos, nepriklausomo aptikimo ar klaidingų perspėjimų rezultatų.");
			return stringBuilder.ToString();
		}

		public string Dashboard()
		{
			StringBuilder stringBuilder = new StringBuilder("PARENGTIS • bandomasis tikrintuvas\r\n");
			long ticks = DateTime.UtcNow.Ticks;
			bool flag = Config.SelfPassed && !Config.SelfRunning && Config.SelfVersion == 48 && Config.SelfAt > 0 && Config.SelfAt <= ticks && ticks - Config.SelfAt < TimeSpan.FromHours(24.0).Ticks;
			stringBuilder.AppendLine("Vietinė savitikra: " + ((!flag) ? "reikia paleisti šiame įrenginyje" : "praėjo šiame įrenginyje (24 h)"));
			try
			{
				stringBuilder.AppendLine("Bazė: " + Database().Count + " sumų; bendruomenės atnaujinimas: " + DateLabel(Config.FeedAt) + "; importas: " + DateLabel(Config.ImportAt));
			}
			catch
			{
				stringBuilder.AppendLine("Bazė neperskaitoma; sumų patikra nepatvirtinta.");
			}
			stringBuilder.AppendLine("Serveris: " + ((!Config.HealthOk || !(Config.HealthEndpoint == Config.Endpoint) || Config.HealthAt > ticks || Config.HealthAt <= 0 || ticks - Config.HealthAt >= TimeSpan.FromHours(24.0).Ticks) ? "ryšys nepatvirtintas arba patikra pasenusi" : "ryšys patvirtintas (serverio teiginys)"));
			stringBuilder.AppendLine("Kitas veiksmas: " + (flag ? "paruoškite šaltinį ir pasirinkite failą. " : "savitikra per paruošimo vedlį."));
			stringBuilder.AppendLine(FolderQueueOverview());
			stringBuilder.AppendLine("Savitikra neįrodo virusų aptikimo. Nuolatinės apsaugos nėra.");
			return stringBuilder.ToString();
		}

		private static string DateLabel(long ticks)
		{
			return (ticks > 0 && ticks <= DateTime.UtcNow.Ticks) ? (new DateTime(ticks, DateTimeKind.Utc).ToLocalTime().ToString("g") + ((DateTime.UtcNow.Ticks - ticks <= TimeSpan.FromHours(48.0).Ticks) ? "" : " (senesnis nei 48 h)")) : "nėra patvirtintos datos";
		}

		public string LastOperation()
		{
			string path = Path.Combine(Root, "last-operation.json");
			try
			{
				return (!File.Exists(path)) ? "Veiksmų dar nėra. Pradėkite paruošimo vedlį." : ("Ankstesnio veiksmo kopija (ne dabartinė būklė)\r\n" + Data.Read<string>(path, 16384));
			}
			catch
			{
				return "Ankstesnio veiksmo suvestinė neperskaitoma.";
			}
		}

		public string CheckServer()
		{
			Config.HealthOk = false;
			Config.HealthAt = DateTime.UtcNow.Ticks;
			Config.HealthEndpoint = Config.Endpoint;
			SaveSettings();
			HttpWebRequest httpWebRequest = Request(new Uri(Data.Endpoint(Config.Endpoint), "v1/health"), Key("server"), "Authorization");
			httpWebRequest.Headers["Authorization"] = "Bearer " + Key("server");
			httpWebRequest.Method = "GET";
			httpWebRequest.CachePolicy = new RequestCachePolicy(RequestCacheLevel.NoCacheNoStore);
			try
			{
				using (WebResponse webResponse = httpWebRequest.GetResponse())
				{
					using (Stream input = webResponse.GetResponseStream())
					{
						Dictionary<string, object> r = Data.Json().Deserialize<Dictionary<string, object>>(Encoding.UTF8.GetString(Data.Bounded(input, 8192, Check)));
						ValidateHealth(r, DateTimeOffset.UtcNow);
					}
				}
				Check();
				Config.HealthOk = true;
				Config.HealthAt = DateTime.UtcNow.Ticks;
				SaveSettings();
				return "Serverio ryšys patvirtintas. /v1/health patikrinti būsenos, variklio ir ne senesnės nei 7 d. bazės laukai. Failas ir jo suma nesiųsti. Pasitikima serverio pateiktais duomenimis; tikrų virusų aptikimas šiuo veiksmu nebandytas.";
			}
			catch (WebException)
			{
				throw new IOException("Serverio ryšys nepatvirtintas. Patikrinkite HTTPS, raktą ir /v1/health.");
			}
			finally
			{
				httpWebRequest.Abort();
			}
		}

		public static void ValidateHealth(Dictionary<string, object> r, DateTimeOffset now)
		{
			if (r == null || !r.ContainsKey("status") || Convert.ToString(r["status"]) != "ready" || !r.ContainsKey("engine") || string.IsNullOrWhiteSpace(Convert.ToString(r["engine"])) || Convert.ToString(r["engine"]).Length > 120 || !r.ContainsKey("database_version") || Convert.ToInt64(r["database_version"]) <= 0 || !r.ContainsKey("database_time") || !Regex.IsMatch(Convert.ToString(r["database_time"]), "(Z|[+-][0-9]{2}:[0-9]{2})$") || !DateTimeOffset.TryParse(Convert.ToString(r["database_time"]), CultureInfo.InvariantCulture, DateTimeStyles.None, out var result) || result > now.AddMinutes(5.0) || result < now.AddDays(-7.0))
			{
				throw new IOException("Serverio variklis ar bazės naujumas nepatvirtinti");
			}
		}

		public void Journal(string subject, string hash, string state)
		{
			if (!ActionStates.Contains(state) || hash == null || (hash != "" && !Data.Hash(hash)))
			{
				throw new IOException("Netinkama veiksmų būsena");
			}
			string path = Path.Combine(Root, "actions.json");
			List<ActionRecord> list = ((!File.Exists(path)) ? new List<ActionRecord>() : Data.Read<List<ActionRecord>>(path, 262144));
			if (list == null || list.Count > 100 || list.Any((ActionRecord r) => r == null || !ActionStates.Contains(r.State) || r.At <= 0))
			{
				throw new IOException("Veiksmų žurnalas sugadintas; neperrašytas");
			}
			list.Insert(0, new ActionRecord
			{
				At = DateTime.UtcNow.Ticks,
				Subject = Data.Clean(subject),
				Hash = hash,
				State = state
			});
			Data.Atomic(path, Data.Json().Serialize(list.Take(100).ToList()));
			VisualEvent = state;
			Progress("Veiksmo būsena atnaujinta");
		}

		public string ActionsReport()
		{
			string path = Path.Combine(Root, "actions.json");
			List<ActionRecord> list = ((!File.Exists(path)) ? new List<ActionRecord>() : Data.Read<List<ActionRecord>>(path, 262144));
			if (list == null || list.Count > 100)
			{
				throw new IOException("Veiksmų žurnalas neperskaitomas");
			}
			return "VEIKSMŲ ŽURNALAS • iki 100 įrašų\r\nIstorinė konkretaus veiksmo būsena nėra dabartinės visos sistemos išvalymo patvirtinimas. REQUESTED be vėlesnio patvirtinimo reiškia nebaigtą / nepatvirtintą veiksmą. RESTORED reiškia, kad turinys vėl gali būti pasiekiamas.\r\n\r\n" + string.Join("\r\n", list.Select((ActionRecord r) => new DateTime(r.At, DateTimeKind.Utc).ToLocalTime().ToString("g") + " • " + r.Subject + " • " + r.State + " • " + r.Hash));
		}

		private byte[] FeedPublisher()
		{
			using (FileStream input = File.OpenRead(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "publisher.der")))
			{
				return Data.Bounded(input, 4096, Check);
			}
		}

		private static long NowMilliseconds()
		{
			return (long)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;
		}

		public string ImportSignedHashes(string path)
		{
			byte[] array;
			using (FileStream input = File.OpenRead(path))
			{
				array = Data.Bounded(input, 8388608, Check);
			}
			byte[] publisher = FeedPublisher();
			long num = Config.SignedDbVersion;
			string path2 = Path.Combine(Root, "signed-feed.json");
			if (File.Exists(path2))
			{
				string s = Data.Read<string>(path2, 12582912);
				num = Math.Max(num, SignedHashFeed.Verify(Convert.FromBase64String(s), publisher, 0L, 1L).Version);
			}
			SignedHashes signedHashes = SignedHashFeed.Verify(array, publisher, num, NowMilliseconds());
			Check();
			Data.Atomic(path2, Data.Json().Serialize(Convert.ToBase64String(array)));
			Config.SignedDbVersion = signedHashes.Version;
			SaveSettings();
			return "Leidėjo bazės parašas patvirtintas. Versija: " + signedHashes.Version + "; sumų: " + signedHashes.Hashes.Count + ". Tai ne aptikimo efektyvumo įrodymas. Saugoma versija saugo nuo senesnio pakartotinio importo.";
		}

		private HashSet<string> LoadSignedHashes()
		{
			string path = Path.Combine(Root, "signed-feed.json");
			if (!File.Exists(path))
			{
				return new HashSet<string>();
			}
			string s = Data.Read<string>(path, 12582912);
			byte[] envelope = Convert.FromBase64String(s);
			SignedHashes signedHashes = SignedHashFeed.Verify(envelope, FeedPublisher(), 0L, NowMilliseconds());
			if (signedHashes.Version < Config.SignedDbVersion)
			{
				throw new IOException("Pasirašyta bazė senesnė už patvirtintą versiją");
			}
			return signedHashes.Hashes;
		}

		private string EngineExe(string name)
		{
			return string.IsNullOrEmpty(EngineBinaryDirectory)?Path.Combine(EngineRoot, "clamav-1.4.6.win.x64", name):Path.Combine(EngineBinaryDirectory,name);
		}

		public string LocalDatabaseReadiness(){try{EngineSafety.ValidateDatabaseFiles(EngineDatabase);return "bazių failai struktūriškai pasiekiami";}catch(Exception e) when(e is IOException||e is UnauthorizedAccessException){return "DĖMESIO: bazės neparuoštos arba nepasiekiamos — atnaujinkite jas";}}
        public string LocalEngineCheckStatus()
		{
			long ticks = DateTime.UtcNow.Ticks;
			long localEngineCheckAt = Config.LocalEngineCheckAt;
			if (localEngineCheckAt <= 0 || localEngineCheckAt > ticks)
			{
				return "kontrolinė patikra dar nepatvirtinta";
			}
			DateTime dateTime = new DateTime(localEngineCheckAt, DateTimeKind.Utc);
			return ((ticks - localEngineCheckAt >= TimeSpan.FromHours(24.0).Ticks) ? "kontrolinė patikra senesnė nei 24 h: " : "kontrolinė patikra praėjo: ") + dateTime.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss zzz") + " (kompiuterio laikas)";
		}

		public string LocalEngineStatus()
		{
			if (!File.Exists(EngineExe("clamscan.exe")) || !File.Exists(EngineExe("freshclam.exe")))
			{
				return "Vietinis ClamAV: neįdiegtas. Pasiruošimas atsisiųs oficialų Windows x64 variklį (~183 MiB).";
			}
			string text = new string[2] { "daily.cvd", "daily.cld" }.Select((string x) => Path.Combine(EngineDatabase, x)).FirstOrDefault(File.Exists);
			bool flag = new string[2] { "main.cvd", "main.cld" }.Any((string x) => File.Exists(Path.Combine(EngineDatabase, x)));
			return "Vietinis ClamAV 1.4.6 LTS: failai įdiegti. " + ((text == null || !flag) ? "Trūksta pagrindinių bazių — atnaujinkite jas." : ("Pagrindinės bazės yra; daily failo pakeitimas: " + File.GetLastWriteTimeUtc(text).ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss zzz") + " (kompiuterio laikas). Failo data nėra parašo ar bazės galiojimo patvirtinimas.")) + "\r\n" + LocalEngineCheckStatus() + ".\r\n" + DetectionControlStatus() + ". Tai istorinė patikra, ne apsauga realiu laiku.";
		}

		public string LocalEngineEvidence()
		{
			return "VARIKLIO IR BAZIŲ BŪSENA PATIKROS METU\r\n" + LocalEngineStatus();
		}

		private void RequireEngine()
		{
			if (Environment.OSVersion.Platform != PlatformID.Win32NT || !Environment.Is64BitOperatingSystem)
			{
				throw new IOException("Šiai integracijai reikia Windows x64");
			}
			if (!File.Exists(EngineExe("clamscan.exe")) || !File.Exists(EngineExe("freshclam.exe")))
			{
				throw new IOException("Pirmiausia paruoškite vietinį ClamAV variklį");
			}
			FolderMonitor.NoLinks(EngineExe("clamscan.exe"));
			FolderMonitor.NoLinks(EngineExe("freshclam.exe"));
		}

		public string InstallLocalEngine()
		{
            if(SharedReadOnlyEngine)throw new IOException("Bendrą variklį prižiūri paslauga; naudokite bendrą diegiklį ir parengties langą.");
			if (Environment.OSVersion.Platform != PlatformID.Win32NT)
			{
				throw new IOException("Diegimas vykdomas tik Windows");
			}
			string script;
			using (Stream stream = typeof(Core).Assembly.GetManifestResourceStream("Sargon.EngineSetup.ps1"))
			{
				if (stream == null)
				{
					throw new IOException("Trūksta variklio paruošimo scenarijaus");
				}
				using (StreamReader streamReader = new StreamReader(stream))
				{
					script = streamReader.ReadToEnd();
				}
			}
			return PowerShell(script, "", 900000) + "\r\n" + LocalEngineStatus();
		}

		public string UpdateLocalEngine()
		{
            if(SharedReadOnlyEngine)throw new IOException("Bendras bazes atnaujina paslauga. Vartotojo sąsaja jų neperrašo.");
			RequireEngine();
            EngineSafety.PrepareWorkspace(Root);
			Config.LocalEngineCheckAt = 0L;
            Config.EngineDetectionAt=0;Config.AutoDbSuccessAt=0;Config.AutoDbAttemptAt=DateTime.UtcNow.Ticks;Config.AutoDbFailures=Math.Max(1,Math.Min(12,Config.AutoDbFailures));
			SaveSettings();
			Directory.CreateDirectory(EngineDatabase);
            FolderMonitor.NoLinks(EngineDatabase);
			string text = Path.Combine(EngineRoot, "freshclam.conf");
			Data.Atomic(text, "DatabaseDirectory " + LocalEngine.Quote(EngineDatabase) + "\nDatabaseMirror database.clamav.net\nConnectTimeout 30\nReceiveTimeout 60\nTestDatabases yes\nScriptedUpdates yes\n");
			ProcessStartInfo processStartInfo = new ProcessStartInfo(EngineExe("freshclam.exe"), "--config-file=" + LocalEngine.Quote(text) + " --stdout --quiet");
			processStartInfo.UseShellExecute = false;
			processStartInfo.CreateNoWindow = true;
			processStartInfo.RedirectStandardOutput = true;
			processStartInfo.RedirectStandardError = true;
			processStartInfo.WorkingDirectory = EngineRoot;
			ProcessStartInfo info = processStartInfo;
			CommandOutput commandOutput = BoundedCommand.RunResult(info, 900000, () => IsCancellationRequested, acceptFound: false, tail: true);
			Check();
			string path = Path.Combine(Root, "engine-control-" + Guid.NewGuid().ToString("N") + ".txt");
			try
			{
				File.WriteAllText(path, "SARGON neutral local engine readiness control.");
				LocalEngineVerdict localEngineVerdict = LocalEngine.Parse(EngineScan(path, recursive: false));
				if (localEngineVerdict.Found || localEngineVerdict.Incomplete || localEngineVerdict.Scanned < 1)
				{
					throw new IOException("Bazių atnaujinimo komanda baigta, bet kontrolinė patikra nepatvirtinta");
				}
				Config.LocalEngineCheckAt = DateTime.UtcNow.Ticks;
                Config.AutoDbSuccessAt=DateTime.UtcNow.Ticks;Config.AutoDbFailures=0;
				SaveSettings();
			}
			finally
			{
				if (File.Exists(path))
				{
					File.Delete(path);
				}
			}
			return "ClamAV bazių atnaujinimas ir neutralaus failo kontrolinė patikra pavyko. FreshClam tikrina CVD parašus; ši programa jų neapeina. Tai nėra visų virusų aptikimo garantija.\r\n" + ((!commandOutput.OutputTruncated && !commandOutput.ErrorTruncated) ? "" : "Ilgas atnaujinimo žurnalas sutrumpintas; saugoma tik pabaiga.\r\n") + "FRESHCLAM ŽURNALAS\r\n" + Data.CleanLog(commandOutput.Output) + ((!string.IsNullOrEmpty(commandOutput.Error)) ? ("\r\nFRESHCLAM PRANEŠIMAI\r\n" + Data.CleanLog(commandOutput.Error)) : "") + "\r\n" + LocalEngineStatus();
		}

		private CommandOutput EngineScan(string path, bool recursive)
		{
			RequireEngine();
            EngineSafety.PrepareWorkspace(Root);
            EngineSafety.ValidateDatabaseFiles(EngineDatabase);
			string arguments = "--database=" + LocalEngine.Quote(EngineDatabase) + " --infected --stdout --max-filesize=256M --max-scansize=512M --max-recursion=16 --max-files=10000 --alert-exceeds-max=yes --alert-encrypted=yes --follow-dir-symlinks=0 --follow-file-symlinks=0 " + ((!recursive) ? "" : "--recursive=yes ") + LocalEngine.Quote(path);
			ProcessStartInfo processStartInfo = new ProcessStartInfo(EngineExe("clamscan.exe"), arguments);
			processStartInfo.UseShellExecute = false;
			processStartInfo.CreateNoWindow = true;
			processStartInfo.RedirectStandardOutput = true;
			processStartInfo.RedirectStandardError = true;
			processStartInfo.WorkingDirectory = EngineRoot;
			ProcessStartInfo processStartInfo2 = processStartInfo;
			processStartInfo2.EnvironmentVariables["LC_ALL"] = "C";
			return BoundedCommand.RunResult(processStartInfo2, (!recursive) ? 300000 : 1800000, () => IsCancellationRequested, acceptFound: true);
		}

		internal void ApplyEngineScope(Witness witness, LocalEngineVerdict verdict)
        {
            if(verdict.Incomplete){NotChecked++;return;}
            LocalEngineCompleted++;
            if(pendingArchiveScope.Remove(witness))NotChecked=Math.Max(0,NotChecked-1);
        }
        public string ScanLocalEngine(Witness witness)
		{
            using(var readLease=new FileStream(witness.Path,FileMode.Open,FileAccess.Read,FileShare.Read)){
			Verify(witness);
			CommandOutput result = EngineScan(witness.Path, recursive: false);
			LocalEngineVerdict localEngineVerdict = LocalEngine.Parse(result);
			Verify(witness);
			if(EngineSafety.EicarOnly(localEngineVerdict))witness.Test=true;
            ApplyEngineScope(witness, localEngineVerdict);
			if (localEngineVerdict.Found)
			{
				witness.Kind = ((!witness.Test) ? "ENGINE" : "TEST");
				if (witness.Test)
				{
					TestFindings++;
				}
				else
				{
					EngineFindings++;
				}
				if (!SessionFindings.Contains(witness) && SessionFindings.Count < 100)
				{
					SessionFindings.Add(witness);
				}
				Finding(witness, "Vietinio ClamAV žyma", "ClamAV 1.4.6 LTS šiame kompiuteryje");
			}
			return "VIETINIS CLAMAV: " + (localEngineVerdict.Found ? "VARIKLIO RADINYS" : ((!localEngineVerdict.Incomplete) ? "žymų neaptikta; tai nėra saugumo garantija" : "APIMTIS NEPATVIRTINTA")) + "\r\nSHA-256: " + witness.Hash + "\r\nPerskaitytų failų pagal variklį: " + localEngineVerdict.Scanned + "\r\nŽymos: " + string.Join(", ", localEngineVerdict.Signatures) + "\r\n" + ((!localEngineVerdict.Incomplete) ? "" : "Yra ribų, šifravimo ar perspėjimų; turinys nepatikrintas išsamiai.\r\nVARIKLIO PRANEŠIMAI\r\n"+LocalEngine.ReportOutput(Data.CleanLog(result.Output),localEngineVerdict)+"\r\n"+Data.CleanLog(result.Error)+"\r\n") + "ClamAV rezultatas atskirtas nuo papildomos SARGON ZIP metaduomenų peržiūros; taikomos variklio ribos.\r\nFailas nebuvo siųstas į serverį. Variklis originalo nešalina; izoliuoti galite patvirtinę veiksmų centre.\r\n\r\n"+LocalEngineEvidence();
		}
		}

		public string ScanFixedDrives()
		{
			string[] array = (from x in DriveInfo.GetDrives()
				where x.DriveType == DriveType.Fixed && x.IsReady
				select x.RootDirectory.FullName).ToArray();
			if (array.Length == 0)
			{
				throw new IOException("Nerasta pasiekiamų vietinių diskų");
			}
			StringBuilder stringBuilder = new StringBuilder("VIETINIŲ DISKŲ CLAMAV PATIKRA — rankinė, pagal šios paskyros prieigą\r\n");
			string[] array2 = array;
			foreach (string text in array2)
			{
				Check();
				Progress("ClamAV tikrina diską " + text);
				try
				{
					CommandOutput commandOutput = EngineScan(text, recursive: true);
					LocalEngineVerdict localEngineVerdict = LocalEngine.Parse(commandOutput);
					stringBuilder.AppendLine(Data.Clean(text) + ": failų " + localEngineVerdict.Scanned + "; radinys " + localEngineVerdict.Found + "; ribota apimtis " + localEngineVerdict.Incomplete);
					stringBuilder.AppendLine(Data.CleanLog(commandOutput.Output));
					if (localEngineVerdict.Incomplete)
					{
						NotChecked++;
					}
					if (localEngineVerdict.Found)
					{
						EngineFindings++;
						NotChecked++;
						stringBuilder.AppendLine("Disko radinius tikrinkite atskirai pagal ataskaitos kelius. Automatinio failų izoliavimo nėra.");
					}
				}
				catch (OperationCanceledException)
				{
					throw;
				}
				catch (CommandFailure commandFailure)
				{
					if (commandFailure.Problem == CommandProblem.Cancelled)
					{
						throw;
					}
					NotChecked++;
					stringBuilder.AppendLine(Data.Clean(text) + ": NEPATVIRTINTA. " + Data.Diagnostic(commandFailure));
				}
				catch (IOException)
				{
					NotChecked++;
					stringBuilder.AppendLine(Data.Clean(text) + ": atsakymas nepatvirtintas; ne viskas patikrinta.");
				}
			}
			return string.Concat(stringBuilder, "\r\nPatikra neapima nepasiekiamų failų, procesų atminties ar sistemos įkrovos. Iki 30 min. diskui, iki 256 MiB failui / 512 MiB išskleistai apimčiai / 10000 vidinių failų / 16 lizdų. Išvesties riba 16384 simboliai kiekvienam srautui. Tai ne nuolatinė visos sistemos apsauga.");
		}
	}
}
