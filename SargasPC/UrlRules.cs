using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace SargasPC
{
	public static class UrlRules
	{
		public static string Domain(string raw)
		{
			try
			{
				if (raw == null || raw.Length > 253 || raw != raw.Trim() || raw.Contains("/") || raw.Contains(":"))
				{
					throw new Exception();
				}
				IdnMapping idnMapping = new IdnMapping();
				idnMapping.UseStd3AsciiRules = true;
				string text = idnMapping.GetAscii(raw).ToLowerInvariant();
				if (text.EndsWith("."))
				{
					text = text.Substring(0, text.Length - 1);
				}
				if (text.Length > 253 || text.IndexOf('.') < 1 || text.All((char c) => char.IsDigit(c) || c == '.'))
				{
					throw new Exception();
				}
				string[] array = text.Split('.');
				foreach (string text2 in array)
				{
					if (text2.Length < 1 || text2.Length > 63 || text2.StartsWith("-") || text2.EndsWith("-"))
					{
						throw new Exception();
					}
				}
				return text;
			}
			catch
			{
				throw new IOException("Netinkamas domenas; reikia domeno be kelio ar prievado");
			}
		}

		public static HashSet<string> Parse(string raw)
		{
			if (raw.Length > 1048576)
			{
				throw new IOException("Domenų sąrašo riba 1 MiB");
			}
			HashSet<string> hashSet = new HashSet<string>(StringComparer.Ordinal);
			string[] array = raw.Split('\n');
			if (array.Length > 10000)
			{
				throw new IOException("Per daug sąrašo eilučių");
			}
			string[] array2 = array;
			foreach (string text in array2)
			{
				string text2 = text.Trim();
				if (text2.Length != 0 && !text2.StartsWith("#"))
				{
					hashSet.Add(Domain(text2));
					if (hashSet.Count > 5000)
					{
						throw new IOException("Daugiau nei 5000 domenų");
					}
				}
			}
			return hashSet;
		}

		public static string Compare(string raw, HashSet<string> rules, string expected)
		{
			string text = Check(raw, rules);
			if (string.IsNullOrEmpty(expected))
			{
				return text;
			}
			string text2 = Domain(expected);
			string text3 = Domain(new Uri(raw).DnsSafeHost);
			bool flag = text3 == text2 || text3.EndsWith("." + text2, StringComparison.Ordinal);
			return text + "\r\n\r\nPALYGINIMAS SU JŪSŲ NURODYTU DOMENU\r\nNurodytas domenas: " + text2 + "\r\n" + ((!flag) ? "DĖMESIO: DOMENAS NESUTAMPA. Nuoroda nepriklauso jūsų nurodytam domenui ar jo subdomenams. Neveskite prisijungimo ar mokėjimo duomenų, kol nepatikrinote oficialaus adreso." : "Domenas sutampa arba yra jo subdomenas. Tai nepatvirtina svetainės saugumo.") + "\r\nPalyginimas tik tekstinis; klaidingai nurodytas domenas klaidina. Peradresavimai ir puslapio turinys netikrinti.";
		}

		public static string Check(string raw, HashSet<string> rules)
		{
			if (raw == null || raw.Length > 4096 || raw.Contains("\\") || raw.Any((char c) => c <= ' ' || c == '\u007f') || !Uri.TryCreate(raw, UriKind.Absolute, out var result) || (result.Scheme != "https" && result.Scheme != "http") || result.UserInfo.Length > 0 || result.HostNameType != UriHostNameType.Dns)
			{
				throw new IOException("Reikia HTTP(S) nuorodos be prisijungimo duomenų, tarpų ar IP literalų");
			}
			string h = Domain(result.DnsSafeHost);
			string text = rules.FirstOrDefault((string d) => h == d || h.EndsWith("." + d, StringComparison.Ordinal));
			return "NUORODOS VIETINĖ PERŽIŪRA\r\nDomenas: " + h + "\r\n" + ((text == null) ? "Sąrašo atitikties nėra. Reputacija ir svetainės saugumas NEPATVIRTINTI." : ("ATITIKTIS JŪSŲ SĄRAŠUI: " + text + ". Nuorodos neatverkite, kol neišsiaiškinote šaltinio.")) + ((!(result.Scheme == "http")) ? "" : "\r\nHTTP: šifruotas HTTPS ryšys nenurodytas.") + ((!h.Contains("xn--") && !result.Host.Any((char c) => c > '\u007f')) ? "" : "\r\nTarptautinis domenas (Punycode): patikrinkite tikrąjį pavadinimą.") + "\r\nSvetainė neaplankyta. Nuoroda ir jos slapti parametrai niekam nesiųsti. Automatinis naršyklės blokavimas galimas tik atskirai įdiegus pateiktą plėtinį ir įkėlus domenų sąrašą.";
		}
	}
}
