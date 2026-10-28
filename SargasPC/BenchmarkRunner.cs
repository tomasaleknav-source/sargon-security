using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace SargasPC
{
	public static class BenchmarkRunner
	{
		public static string Run(string output)
		{
			string text = Path.Combine(Path.GetTempPath(), "sargas-bench-" + Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(text);
			StringBuilder stringBuilder = new StringBuilder("environment;run;bytes;wall_ms;process_cpu_ms_or_NA;peak_process_bytes_or_NA\r\n");
			try
			{
				string path = Path.Combine(text, "neutral.bin");
				using (FileStream fileStream = File.Create(path))
				{
					byte[] array = new byte[65536];
					for (int i = 0; i < 512; i++)
					{
						fileStream.Write(array, 0, array.Length);
					}
				}
				Core core = new Core(Path.Combine(text, "data"));
				for (int j = 1; j <= 3; j++)
				{
					core.Reset();
					Process currentProcess = Process.GetCurrentProcess();
					TimeSpan totalProcessorTime = currentProcess.TotalProcessorTime;
					Stopwatch stopwatch = Stopwatch.StartNew();
					core.Scan(path, 268435456L);
					stopwatch.Stop();
					currentProcess.Refresh();
					stringBuilder.AppendLine(string.Concat(Environment.OSVersion.Platform, ";", j, ";", core.ReadBytes, ";", stopwatch.ElapsedMilliseconds, ";", (!((currentProcess.TotalProcessorTime - totalProcessorTime).TotalMilliseconds > 0.0)) ? "NA" : ((long)(currentProcess.TotalProcessorTime - totalProcessorTime).TotalMilliseconds/*cast due to .constrained prefix*/).ToString(), ";", (currentProcess.PeakWorkingSet64 <= 0) ? "NA" : currentProcess.PeakWorkingSet64.ToString()));
				}
				stringBuilder.AppendLine("LIMITS: neutral 32 MiB SHA-256 only; no malware efficacy, native Defender, battery drain or controlled whole-system impact measurement.");
				Directory.CreateDirectory(output);
				string text2 = Path.Combine(output, "Sargas_spartos_bandymas_" + DateTime.UtcNow.ToString("yyyyMMdd_HHmmss") + "_" + Guid.NewGuid().ToString("N").Substring(0, 8) + ".csv");
				using (FileStream fileStream2 = new FileStream(text2, FileMode.CreateNew, FileAccess.Write))
				{
					byte[] bytes = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true).GetBytes(stringBuilder.ToString());
					fileStream2.Write(bytes, 0, bytes.Length);
				}
				return text2;
			}
			finally
			{
				Directory.Delete(text, recursive: true);
			}
		}
	}
}
