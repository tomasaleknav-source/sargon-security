using System.Collections.Generic;

namespace SargasPC
{
	public sealed class SignedHashes
	{
		public long Version;

		public long Expires;

		public HashSet<string> Hashes;
	}
}
