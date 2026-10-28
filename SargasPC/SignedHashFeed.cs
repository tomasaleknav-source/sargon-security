using System;
using System.IO;
using System.Linq;
using System.Text;

namespace SargasPC
{
	public static class SignedHashFeed
	{
		public static SignedHashes Verify(byte[] envelope, byte[] publisher, long minimum, long now)
		{
			if (envelope.Length > 8388608)
			{
				throw new IOException("Pasirašytos bazės riba 8 MiB");
			}
			int num = Array.IndexOf(envelope, (byte)10);
			int num2 = ((num >= 0) ? Array.IndexOf(envelope, (byte)10, num + 1) : (-1));
			if (num < 0 || num2 < 0 || num2 > 4096 || Encoding.ASCII.GetString(envelope, 0, num) != "SARGAS-DB-1")
			{
				throw new IOException("Netinkamas pasirašytos bazės formatas");
			}
			string text = Encoding.ASCII.GetString(envelope, num + 1, num2 - num - 1);
			if (text.Any(char.IsWhiteSpace))
			{
				throw new IOException("Netinkamas bazės parašas");
			}
			byte[] array = envelope.Skip(num2 + 1).ToArray();
			UpdateGate.VerifySignature(array, Convert.FromBase64String(text), publisher);
			string text2 = Encoding.UTF8.GetString(array);
			int num3 = text2.IndexOf('\n');
			int num4 = text2.IndexOf('\n', num3 + 1);
			if (num3 < 0 || num4 < 0 || !text2.StartsWith("version=") || !text2.Substring(num3 + 1, num4 - num3 - 1).StartsWith("expires="))
			{
				throw new IOException("Trūksta bazės versijos ar datos");
			}
			if (!long.TryParse(text2.Substring(8, num3 - 8), out var result) || !long.TryParse(text2.Substring(num3 + 9, num4 - num3 - 9), out var result2) || result <= minimum || result2 <= now)
			{
				throw new IOException("Pasenusi arba ne naujesnė pasirašyta bazė");
			}
			SignedHashes signedHashes = new SignedHashes();
			signedHashes.Version = result;
			signedHashes.Expires = result2;
			signedHashes.Hashes = Data.ParseHashes(new StringReader(text2.Substring(num4 + 1)));
			return signedHashes;
		}
	}
}
