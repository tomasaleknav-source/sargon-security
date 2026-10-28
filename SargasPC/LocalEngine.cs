using System;
using System.Globalization;
using System.IO;
using System.Linq;

namespace SargasPC
{
	public static class LocalEngine
	{
		public static LocalEngineVerdict Parse(CommandOutput result)
		{
			if (result == null || result.OutputTruncated || result.ErrorTruncated || result.Output == null || result.Error == null || result.Output.Length > 16384 || result.Error.Length > 16384 || result.ExitCode < 0 || result.ExitCode > 1)
			{
				throw new IOException("Vietinio variklio atsakymas nepatvirtintas");
			}
			string[] source = result.Output.Split(new char[2] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
			string[] array = source.Where((string x) => x.StartsWith("Scanned files:", StringComparison.Ordinal)).ToArray();
			if (array.Length != 1 || !long.TryParse(array[0].Substring(14).Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out var result2) || result2 < 0)
			{
				throw new IOException("Variklis nepatvirtino perskaitytų failų apimties");
			}
			string[] array2 = source.Where((string x) => x.EndsWith(" FOUND", StringComparison.Ordinal)).ToArray();
			bool incomplete = result2 == 0 || result.Error.Length > 0 || source.Any((string x) => x.IndexOf("ERROR", StringComparison.Ordinal) >= 0 || x.IndexOf("Heuristics.Limits.", StringComparison.Ordinal) >= 0 || x.IndexOf("Heuristics.Encrypted.", StringComparison.Ordinal) >= 0);
			string[] array3 = (from x in array2.Select(delegate(string x)
				{
					int num = x.LastIndexOf(": ", StringComparison.Ordinal);
					if (num < 0)
					{
						throw new IOException("Neatpažintas variklio radinio formatas");
					}
					return Data.Clean(x.Substring(num + 2, x.Length - num - 8));
				})
				where x.Length > 0
				select x).ToArray();
			bool found = array3.Any((string x) => !x.StartsWith("Heuristics.Limits.", StringComparison.Ordinal) && !x.StartsWith("Heuristics.Encrypted.", StringComparison.Ordinal));
			if ((result.ExitCode == 0 && array2.Length > 0) || (result.ExitCode == 1 && array2.Length == 0))
			{
				throw new IOException("Variklio išvados ir grąžinimo kodas nesutampa");
			}
			LocalEngineVerdict localEngineVerdict = new LocalEngineVerdict();
			localEngineVerdict.Found = found;
			localEngineVerdict.Incomplete = incomplete;
			localEngineVerdict.Scanned = result2;
			localEngineVerdict.Signatures = array3;
			return localEngineVerdict;
		}

		public static string ReportOutput(string output, LocalEngineVerdict verdict)
		{
			if (output == null) return "";
			string[] lines = output.Split(new char[2] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
			int limitNotices = lines.Count(x => x.EndsWith(" FOUND", StringComparison.Ordinal) && (x.Contains("Heuristics.Limits.") || x.Contains("Heuristics.Encrypted.")));
			for (int i = 0; i < lines.Length; i++)
			{
				if (lines[i].EndsWith(" FOUND", StringComparison.Ordinal) && (lines[i].Contains("Heuristics.Limits.") || lines[i].Contains("Heuristics.Encrypted.")))
				{
					lines[i] = "SARGON APIMTIES / ŠIFRAVIMO ĮSPĖJIMAS — failas nepatikrintas; tai nėra viruso radinys: " + lines[i].Substring(0, lines[i].Length - 6);
				}
				else if (limitNotices > 0 && verdict != null && !verdict.Found && lines[i].StartsWith("Infected files:", StringComparison.Ordinal))
				{
					lines[i] = "ClamAV „Infected files“ skaitiklis (" + lines[i].Substring("Infected files:".Length).Trim() + ") apima aukščiau nurodytą apimties / šifravimo įspėjimą; SARGON viruso žymų radinių: 0.";
				}
			}
			return string.Join("\r\n", lines);
		}

		public static string Quote(string text)
		{
			if (string.IsNullOrEmpty(text) || text.Any((char c) => c < ' ' || c == '"'))
			{
				throw new IOException("Netinkamas vietinio variklio kelias");
			}
			return "\"" + text + ((!text.EndsWith("\\", StringComparison.Ordinal)) ? "" : "\\") + "\"";
		}
	}
}
