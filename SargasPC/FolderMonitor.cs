using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SargasPC
{
	public sealed class FolderMonitor : IDisposable
	{
		private readonly string root;

		private readonly string prefix;

		private readonly object sync = new object();

		private readonly Dictionary<string, long> pending = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);

		private readonly Dictionary<string,int> retryCounts=new Dictionary<string,int>(StringComparer.OrdinalIgnoreCase);
        private FileSystemWatcher watcher;

		public bool Lost { get; private set; }

		public int Dropped { get; private set; }

		public int Count
		{
			get
			{
				lock (sync)
				{
					return pending.Count;
				}
			}
		}

		public string Summary
		{
			get
			{
				lock (sync)
				{
					return (!Lost) ? ("Stebimas vienas aplankas be poaplankių; eilėje: " + pending.Count + "; neįtrauktų įvykių: " + Dropped + ". Tai nėra visos sistemos apsauga.") : "STEBĖJIMAS SUTRIKO — pradėkite rankinę patikrą ir įjunkite iš naujo.";
				}
			}
		}

		public FolderMonitor(string directory, bool native = true)
		{
			root = Path.GetFullPath(directory);
			if (!Directory.Exists(root))
			{
				throw new IOException("Aplankas neegzistuoja");
			}
			NoLinks(root);
			prefix = root.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
			if (!native)
			{
				return;
			}
			watcher = new FileSystemWatcher(root)
			{
				IncludeSubdirectories = false,
				NotifyFilter = (NotifyFilters.FileName | NotifyFilters.Size | NotifyFilters.LastWrite),
				InternalBufferSize = 8192
			};
			watcher.Created += delegate(object s, FileSystemEventArgs e)
			{
				Enqueue(e.FullPath, DateTime.UtcNow.Ticks);
			};
			watcher.Changed += delegate(object s, FileSystemEventArgs e)
			{
				Enqueue(e.FullPath, DateTime.UtcNow.Ticks);
			};
			watcher.Renamed += delegate(object s, RenamedEventArgs e)
			{
				Enqueue(e.FullPath, DateTime.UtcNow.Ticks);
			};
			watcher.Error += delegate
			{
				lock (sync)
				{
					Lost = true;
					pending.Clear();retryCounts.Clear();
				}
				try
				{
					watcher.EnableRaisingEvents = false;
				}
				catch
				{
				}
			};
			watcher.EnableRaisingEvents = true;
		}

		public static void NoLinks(string path)
		{
			for (string text = path; text != null; text = Path.GetDirectoryName(text))
			{
				if ((File.GetAttributes(text) & FileAttributes.ReparsePoint) != 0)
				{
					throw new IOException("Kelias per nuorodą neleidžiamas");
				}
			}
		}

		public void SeedExisting()
        {
            int count=0;
            try { foreach(string file in Directory.EnumerateFiles(root,"*",SearchOption.TopDirectoryOnly)) {
                if(count++>=128){lock(sync){Dropped=Math.Min(1000000,Dropped+1);}break;}
                Enqueue(file,DateTime.UtcNow.Ticks);
            }} catch(Exception e) when(e is IOException || e is UnauthorizedAccessException){lock(sync){Dropped=Math.Min(1000000,Dropped+1);}}
        }

        public void Enqueue(string file, long now)
		{
			try
			{
				string fullPath = Path.GetFullPath(file);
				if (!fullPath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) || !string.Equals(Path.GetDirectoryName(fullPath), root, StringComparison.OrdinalIgnoreCase) || fullPath.Length > 1024)
				{
					return;
				}
				lock (sync)
				{
					if (!Lost)
					{
						if (!pending.ContainsKey(fullPath) && pending.Count >= 128)
						{
							Dropped = Math.Min(1000000, Dropped + 1);
						}
						else
						{
							pending[fullPath] = now;retryCounts.Remove(fullPath);
						}
					}
				}
			}
			catch
			{
				lock (sync)
				{
					Dropped = Math.Min(1000000, Dropped + 1);
				}
			}
		}

        public void Succeeded(string file){lock(sync)retryCounts.Remove(file);}
        public void ScheduleRetry(string file,long now){string full=Path.GetFullPath(file);if(now<=0||!full.StartsWith(prefix,StringComparison.OrdinalIgnoreCase)||!string.Equals(Path.GetDirectoryName(full),root,StringComparison.OrdinalIgnoreCase)||full.Length>1024)return;lock(sync){if(Lost||pending.ContainsKey(full))return;if(pending.Count>=128||(!retryCounts.ContainsKey(full)&&retryCounts.Count>=128)){Dropped=Math.Min(1000000,Dropped+1);return;}int prior;retryCounts.TryGetValue(full,out prior);int count=Math.Min(5,prior+1);retryCounts[full]=count;pending[full]=now+TimeSpan.FromMinutes(1<<(count-1)).Ticks-TimeSpan.FromSeconds(5).Ticks;}}

		public string Take(long now)
		{
			lock (sync)
			{
				List<KeyValuePair<string, long>> list = (from p in pending
					where p.Value <= now && now - p.Value >= TimeSpan.FromSeconds(5.0).Ticks
					orderby p.Value
					select p).Take(1).ToList();
				if (list.Count == 0)
				{
					return null;
				}
				pending.Remove(list[0].Key);
				return list[0].Key;
			}
		}

		public void Dispose()
		{
			if (watcher != null)
			{
				watcher.EnableRaisingEvents = false;
				watcher.Dispose();
				watcher = null;
			}
			lock (sync)
			{
				pending.Clear();retryCounts.Clear();
			}
		}
	}
}
