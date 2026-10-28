using System.Collections.Generic;

namespace SargasPC
{
	public sealed class FolderPlan
	{
		public string Root;

		public string Database;

		public List<string> Pending = new List<string>();

		public int Done;

		public int Omitted;
        public int SkippedFiles;

		public long At;
	}
}
