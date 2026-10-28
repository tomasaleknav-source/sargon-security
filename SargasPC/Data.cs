using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web.Script.Serialization;

namespace SargasPC
{
	public static class Data
	{
		public const string EmptyHash = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855";

		public static string Diagnostic(Exception error)
		{
			if (error.InnerException is CommandFailure)
			{
				error = error.InnerException;
			}
			if (error is CommandFailure commandFailure)
			{
				string text = ((commandFailure.Problem == CommandProblem.Cancelled) ? "Veiksmas atšauktas" : ((commandFailure.Problem == CommandProblem.Timeout) ? "Viršytas laukimo terminas" : ((commandFailure.Problem == CommandProblem.OutputLimit) ? "Atsakymas viršijo 16384 simbolių ribą" : ((commandFailure.Problem == CommandProblem.ReadFailure) ? "Nepavyko perskaityti komandos srauto" : ((commandFailure.Problem != CommandProblem.ExitCode) ? "Nepavyko paleisti sistemos komandos" : "Komanda baigėsi klaidos kodu")))));
				return text + " [CMD_" + commandFailure.Problem.ToString().ToUpperInvariant() + "; kodas=" + commandFailure.Code + "].";
			}
			string text2 = ((error is MissingMethodException) ? "Nesuderinamas .NET metodas (programos surinkimo klaida)" : ((error is UnauthorizedAccessException) ? "Prieiga nesuteikta" : ((error is CryptographicException) ? "Kriptografijos patikra nepavyko" : ((error is IOException) ? "Skaitymo / rašymo patikra nepavyko" : ((error is FormatException || error is ArgumentException) ? "Netinkamas duomenų formatas" : ((!(error is InvalidOperationException)) ? "Vykdymo klaida" : "Bandymo rezultatas neatitiko lūkesčio"))))));
			return text2 + " [tipas=" + error.GetType().Name + "; HRESULT=0x" + error.HResult.ToString("X8") + "].";
		}

		public static void ProbeHashReader()
		{
			string s = "\ufeff" + "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855".ToUpperInvariant() + "\r\n# bandymas\r\ne3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855\n";
			HashSet<string> hashSet = ParseHashes(new StringReader(s));
			if (hashSet.Count != 1 || !hashSet.Contains("e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855"))
			{
				throw new InvalidOperationException();
			}
			bool flag = false;
			try
			{
				ParseHashes(new StringReader("netinkama-suma"));
			}
			catch (IOException)
			{
				flag = true;
			}
			if (!flag)
			{
				throw new InvalidOperationException();
			}
		}

		public static JavaScriptSerializer Json()
		{
			JavaScriptSerializer javaScriptSerializer = new JavaScriptSerializer();
			javaScriptSerializer.MaxJsonLength = 12582912;
			javaScriptSerializer.RecursionLimit = 32;
			return javaScriptSerializer;
		}

		public static byte[] Bounded(Stream input, int maximum, Action check)
		{
			using (MemoryStream memoryStream = new MemoryStream())
			{
				byte[] array = new byte[65536];
				int num;
				while ((num = input.Read(array, 0, array.Length)) > 0)
				{
					check?.Invoke();
					if (memoryStream.Length + num > maximum)
					{
						throw new IOException("Viršyta atsakymo arba failo dydžio riba");
					}
					memoryStream.Write(array, 0, num);
				}
				return memoryStream.ToArray();
			}
		}

		public static T Read<T>(string path, int maximum)
		{
			using (FileStream input = File.OpenRead(path))
			{
				return Json().Deserialize<T>(Encoding.UTF8.GetString(Bounded(input, maximum, null)));
			}
		}

		public static void Atomic(string path, string text)
		{
			string text2 = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
			try
			{
				using (FileStream fileStream = new FileStream(text2, FileMode.CreateNew, FileAccess.Write, FileShare.None))
				{
					byte[] bytes = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false).GetBytes(text);
					fileStream.Write(bytes, 0, bytes.Length);
					fileStream.Flush(flushToDisk: true);
				}
				if (File.Exists(path))
				{
					File.Replace(text2, path, null);
				}
				else
				{
					File.Move(text2, path);
				}
			}
			finally
			{
				if (File.Exists(text2))
				{
					File.Delete(text2);
				}
			}
		}

		public static string FindingAdvice(string kind)
		{
			switch (kind)
			{
			case "TEST":
				return "Tai EICAR bandymo tekstas, ne tikras virusas. Patikrinkite bandymo rezultatą; automatinis šalinimas nereikalingas.";
			case "HASH":
			case "ENGINE":
			case "REPUTATION":
				return "Neatverkite įtartino failo. Peržiūrėkite šaltinį ir SHA-256; klaidingas perspėjimas galimas. Prieš veiksmą failas bus patikrintas iš naujo.\r\nVien šifruota kopija originalo nepašalina. Izoliavimas reikalauja atskiro originalo pašalinimo patvirtinimo. Atkūrus turinys vėl tampa pasiekiamas. Vienas veiksmas nepatvirtina viso PC išvalymo.";
			default:
				return "Radinio pagrindas nepatvirtintas. Pirmiausia atlikite naują tikrinimą; vien šis įrašas nėra pagrindas šalinti failą.";
			}
		}

		public static string BackgroundPowerHold(bool chargingOnly, bool online, bool known)
		{
			if (!chargingOnly)
			{
				return "";
			}
			if (!known)
			{
				return "Aplanko patikra atidėta: maitinimo būsena nepatvirtinta. Rankinis tikrinimas prieinamas.";
			}
			return (!online) ? "Aplanko patikra atidėta: naudojama baterija. Prijunkite maitinimą arba pakeiskite maitinimo nuostatas. Rankinis tikrinimas prieinamas." : "";
		}

		public static string CleanLog(string value)
		{
			if (string.IsNullOrEmpty(value))
			{
				return "";
			}
			string text = value.Replace("\r\n", "\n").Replace('\r', '\n');
			bool flag = text.Length > 16384;
			string text2 = new string((from c in text.Take(16384)
				where (!char.IsControl(c) || c == '\n' || c == '\t') && char.GetUnicodeCategory(c) != UnicodeCategory.Format
				select c).ToArray());
			return text2.Replace("\n", "\r\n") + ((!flag) ? "" : "\r\n[Žurnalas sutrumpintas ties 16384 simbolių riba.]");
		}

		public static string Clean(string s)
		{
			if (s == null)
			{
				return "";
			}
			return new string(s.Where((char c) => !char.IsControl(c)).Take(160).ToArray());
		}

		public static string Digest(byte[] b)
		{
			using (SHA256 sHA = SHA256.Create())
			{
				return Hex(sHA.ComputeHash(b));
			}
		}

		public static void RestoreNew(string path, byte[] plain, string expected, Action check)
		{
			if (plain == null || plain.LongLength > 8388608 || !Hash(expected))
			{
				throw new IOException("Netinkami atkūrimo duomenys");
			}
			path = Path.GetFullPath(path);
			if (File.Exists(path) || Directory.Exists(path))
			{
				throw new IOException("Pasirinktas failas jau yra; pasirinkite naują vardą");
			}
			string text = Path.Combine(Path.GetDirectoryName(path), ".sargas-restore-" + Guid.NewGuid().ToString("N") + ".tmp");
			bool flag = false;
			try
			{
				check();
				using (FileStream fileStream = new FileStream(text, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None))
				{
					flag = true;
					for (int i = 0; i < plain.Length; i += 65536)
					{
						check();
						fileStream.Write(plain, i, Math.Min(65536, plain.Length - i));
					}
					fileStream.Flush(flushToDisk: true);
					fileStream.Position = 0L;
					using (SHA256 sHA = SHA256.Create())
					{
						byte[] array = new byte[65536];
						long num = 0L;
						int num2;
						while ((num2 = fileStream.Read(array, 0, array.Length)) > 0)
						{
							check();
							num += num2;
							sHA.TransformBlock(array, 0, num2, array, 0);
						}
						sHA.TransformFinalBlock(new byte[0], 0, 0);
						if (num != plain.LongLength || Hex(sHA.Hash) != expected)
						{
							throw new IOException("Įrašyto failo patikra nesutapo");
						}
					}
				}
				check();
				File.Move(text, path);
				flag = false;
			}
			finally
			{
				if (flag)
				{
					try
					{
						File.Delete(text);
					}
					catch
					{
					}
				}
			}
		}

		public static string Hex(byte[] b)
		{
			return BitConverter.ToString(b).Replace("-", "").ToLowerInvariant();
		}

		public static bool Marker(byte[] head)
		{
			return Encoding.ASCII.GetString(head).StartsWith("X5O!P%@AP[4\\PZX54(P^)7CC)7}$EICAR-STANDARD-ANTIVIRUS-TEST-FILE!$H+H*");
		}

		public static bool Hash(string s)
		{
			return s != null && s.Length == 64 && s.All((char c) => (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f'));
		}

		public static HashSet<string> ParseHashes(TextReader r)
		{
			HashSet<string> result = new HashSet<string>(StringComparer.Ordinal);
			int num = 0;
			int num2 = 0;
			StringBuilder stringBuilder = new StringBuilder();
			char[] array = new char[4096];
			int num3;
			while ((num3 = r.Read(array, 0, array.Length)) > 0)
			{
				for (int i = 0; i < num3; i++)
				{
					int num4 = array[i];
					if (++num2 > 8388608)
					{
						throw new IOException("Sąrašas viršija 8 MiB simbolių");
					}
					if (num4 == 10)
					{
						AddLine(result, stringBuilder.ToString(), ++num);
						stringBuilder.Length = 0;
						continue;
					}
					if (stringBuilder.Length >= 256)
					{
						throw new IOException("Per ilga sąrašo eilutė");
					}
					stringBuilder.Append((char)num4);
				}
			}
			if (stringBuilder.Length > 0)
			{
				AddLine(result, stringBuilder.ToString(), ++num);
			}
			return result;
		}

		private static void AddLine(HashSet<string> result, string line, int row)
		{
			if (row > 100000)
			{
				throw new IOException("Daugiau nei 100000 eilučių");
			}
			string text = line.Trim().TrimStart('\ufeff');
			if (text.Length != 0 && !text.StartsWith("#"))
			{
				text = text.ToLowerInvariant();
				if (!Hash(text))
				{
					throw new IOException("Netinkama SHA-256 suma");
				}
				result.Add(text);
			}
		}

		public static Uri Endpoint(string raw)
		{
			if (!Uri.TryCreate(raw, UriKind.Absolute, out var result) || result.Scheme != "https" || result.UserInfo.Length > 0 || result.Query.Length > 0 || result.Fragment.Length > 0 || string.IsNullOrWhiteSpace(result.Host))
			{
				throw new IOException("Reikia HTTPS serverio adreso be rakto, užklausos ar fragmento");
			}
			return new Uri(result.ToString().TrimEnd('/') + "/");
		}

		public static string Token(string value)
		{
			if (string.IsNullOrEmpty(value) || value.Length > 512 || value.Any((char c) => c < '!' || c > '~'))
			{
				throw new IOException("Netinkamas arba trūkstamas prieigos raktas");
			}
			return value;
		}
	}
}
