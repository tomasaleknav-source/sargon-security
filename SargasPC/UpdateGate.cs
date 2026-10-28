using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace SargasPC
{
	public static class UpdateGate
	{
		private sealed class Der
		{
			private readonly byte[] b;

			private int p;

			public bool End => p == b.Length;

			public Der(byte[] value)
			{
				b = value;
			}

			public byte[] Read(int tag)
			{
				if (p >= b.Length || b[p++] != tag || p >= b.Length)
				{
					throw new IOException("Netinkamas leidėjo DER raktas");
				}
				int num = b[p++];
				if (num >= 128)
				{
					int num2 = num & 0x7F;
					if (num2 < 1 || num2 > 3 || p + num2 > b.Length)
					{
						throw new IOException("DER riba");
					}
					num = 0;
					for (int i = 0; i < num2; i++)
					{
						num = (num << 8) | b[p++];
					}
				}
				if (num < 0 || p + num > b.Length)
				{
					throw new IOException("DER riba");
				}
				byte[] array = new byte[num];
				Array.Copy(b, p, array, 0, num);
				p += num;
				return array;
			}
		}

		private static RSACryptoServiceProvider Publisher(byte[] bytes)
		{
			if (bytes.Length > 4096)
			{
				throw new IOException("Leidėjo rakto riba");
			}
			Der der = new Der(bytes);
			Der der2 = new Der(der.Read(48));
			if (!der.End)
			{
				throw new IOException("Netinkamas DER");
			}
			byte[] array = der2.Read(48);
			if (BitConverter.ToString(array) != "06-09-2A-86-48-86-F7-0D-01-01-01-05-00")
			{
				throw new IOException("Reikia RSA leidėjo rakto");
			}
			byte[] array2 = der2.Read(3);
			if (!der2.End || array2.Length < 1 || array2[0] != 0)
			{
				throw new IOException("Netinkamas DER");
			}
			Der der3 = new Der(array2.Skip(1).ToArray());
			Der der4 = new Der(der3.Read(48));
			if (!der3.End)
			{
				throw new IOException("Netinkamas DER");
			}
			byte[] array3 = der4.Read(2);
			byte[] array4 = der4.Read(2);
			if (!der4.End)
			{
				throw new IOException("Netinkamas DER");
			}
			if (array3.Length > 1 && array3[0] == 0)
			{
				array3 = array3.Skip(1).ToArray();
			}
			if (array3.Length < 256 || array3.Length > 1024 || array3[0] < 128 || array4.Length < 1 || array4.Length > 8)
			{
				throw new IOException("Reikia 2048–8192 bitų RSA rakto");
			}
			ulong num = 0uL;
			byte[] array5 = array4;
			foreach (byte b in array5)
			{
				num = (num << 8) | b;
			}
			if (num < 3 || (num & 1) == 0 || num > uint.MaxValue)
			{
				throw new IOException("Netinkamas RSA eksponentas");
			}
			RSACryptoServiceProvider rSACryptoServiceProvider = new RSACryptoServiceProvider();
			rSACryptoServiceProvider.PersistKeyInCsp = false;
			rSACryptoServiceProvider.ImportParameters(new RSAParameters
			{
				Modulus = array3,
				Exponent = array4
			});
			return rSACryptoServiceProvider;
		}

		public static void VerifySignature(byte[] payload, byte[] signature, byte[] publisher)
		{
			using (RSACryptoServiceProvider rSACryptoServiceProvider = Publisher(publisher))
			{
				if (!rSACryptoServiceProvider.VerifyData(payload, CryptoConfig.MapNameToOID("SHA256"), signature))
				{
					throw new IOException("Leidėjo parašas nepatvirtintas");
				}
			}
		}

		public static UpdateOffer Manifest(byte[] envelope, byte[] publisher, string platform, long installed, long now)
		{
			if (envelope.Length > 4096)
			{
				throw new IOException("Manifesto riba");
			}
			string text = Encoding.ASCII.GetString(envelope);
			int num = text.IndexOf('\n');
			int num2 = text.IndexOf('\n', num + 1);
			if (num < 0 || num2 < 0 || text.Substring(0, num) != "SARGAS-UPDATE-1")
			{
				throw new IOException("Netinkamas atnaujinimo formatas");
			}
			string text2 = text.Substring(num + 1, num2 - num - 1);
			if (text2.Any(char.IsWhiteSpace))
			{
				throw new IOException("Netinkamas parašas");
			}
			byte[] array = envelope.Skip(num2 + 1).ToArray();
			using (RSACryptoServiceProvider rSACryptoServiceProvider = Publisher(publisher))
			{
				if (!rSACryptoServiceProvider.VerifyData(array, CryptoConfig.MapNameToOID("SHA256"), Convert.FromBase64String(text2)))
				{
					throw new IOException("Atnaujinimo parašas nepatvirtintas");
				}
			}
			string[] array2 = Encoding.ASCII.GetString(array).Split('\n');
			string[] array3 = new string[5] { "platform=", "version=", "expires=", "bytes=", "sha256=" };
			if (array2.Length != 6 || array2[5] != "")
			{
				throw new IOException("Netinkami manifesto laukai");
			}
			for (int i = 0; i < 5; i++)
			{
				if (!array2[i].StartsWith(array3[i], StringComparison.Ordinal))
				{
					throw new IOException("Netinkami manifesto laukai");
				}
			}
			if (array2[0] != "platform=" + platform)
			{
				throw new IOException("Atnaujinimas skirtas kitai platformai");
			}
			for (int j = 1; j < 4; j++)
			{
				if (!Regex.IsMatch(array2[j].Substring(array3[j].Length), "^[0-9]{1,16}$"))
				{
					throw new IOException("Netinkami manifesto skaičiai");
				}
			}
			long num3 = long.Parse(array2[1].Substring(8));
			long num4 = long.Parse(array2[2].Substring(8));
			long num5 = long.Parse(array2[3].Substring(6));
			string text3 = array2[4].Substring(7);
			if (num3 <= installed || num3 > int.MaxValue || num4 <= now || num4 - now > 7776000000L || num5 < 1 || num5 > 536870912 || !Data.Hash(text3))
			{
				throw new IOException("Pasenęs, ne naujesnis arba netinkamas atnaujinimas");
			}
			UpdateOffer updateOffer = new UpdateOffer();
			updateOffer.Version = num3;
			updateOffer.Bytes = num5;
			updateOffer.Hash = text3;
			return updateOffer;
		}

		public static void Binary(UpdateOffer offer, Stream input, Action check)
		{
			using (SHA256 sHA = SHA256.Create())
			{
				byte[] array = new byte[65536];
				long num = 0L;
				int num2;
				while ((num2 = input.Read(array, 0, array.Length)) > 0)
				{
					check();
					num += num2;
					if (num > offer.Bytes)
					{
						throw new IOException("Atnaujinimo failo dydis nesutampa");
					}
					sHA.TransformBlock(array, 0, num2, array, 0);
				}
				sHA.TransformFinalBlock(new byte[0], 0, 0);
				if (num != offer.Bytes || Data.Hex(sHA.Hash) != offer.Hash)
				{
					throw new IOException("Atnaujinimo failo dydis ar suma nesutampa");
				}
			}
		}
	}
}
