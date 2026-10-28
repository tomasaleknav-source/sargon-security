using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace SargasPC
{
	public static class ArchiveReview
	{
		public static bool Named(string name)
		{
			return new string[9] { ".zip", ".apk", ".jar", ".docx", ".xlsx", ".pptx", ".odt", ".ods", ".epub" }.Any((string e) => name.EndsWith(e, StringComparison.OrdinalIgnoreCase));
		}

		private static int U16(byte[] b, int p)
		{
			return b[p] | (b[p + 1] << 8);
		}

		private static long U32(byte[] b, int p)
		{
			return (uint)U16(b, p) | ((long)U16(b, p + 2) << 16);
		}

		private static byte[] Read(Stream s, long at, int size)
		{
			s.Position = at;
			byte[] array = new byte[size];
			int num;
			for (int i = 0; i < size; i += num)
			{
				num = s.Read(array, i, size - i);
				if (num == 0)
				{
					throw new IOException("Nutrūkę ZIP duomenys");
				}
			}
			return array;
		}

		public static string Inspect(Stream input, string name, Action check)
		{
			long length = input.Length;
			byte[] b = ((length < 4) ? new byte[4] : Read(input, 0L, 4));
			if ((U16(b, 0) != 19280 || !new int[3] { 1027, 1541, 2055 }.Contains(U16(b, 2))) && !Named(name))
			{
				return "";
			}
			try
			{
				check();
				int num = (int)Math.Min(length, 65557L);
				byte[] b2 = Read(input, length - num, num);
				int num2 = -1;
				for (int num3 = num - 22; num3 >= 0; num3--)
				{
					if (U32(b2, num3) == 101010256 && num3 + 22 + U16(b2, num3 + 20) == num)
					{
						num2 = num3;
						break;
					}
				}
				if (num2 < 0)
				{
					throw new IOException("Nerastas ZIP pabaigos įrašas");
				}
				int num4 = U16(b2, num2 + 10);
				long num5 = U32(b2, num2 + 12);
				long num6 = U32(b2, num2 + 16);
				if (U16(b2, num2 + 4) != 0 || U16(b2, num2 + 6) != 0 || U16(b2, num2 + 8) != num4 || num4 == 65535 || num5 == uint.MaxValue || num6 == uint.MaxValue)
				{
					throw new IOException("Daugiadalis arba ZIP64 formatas neanalizuojamas");
				}
				if (num4 > 512 || num5 > 1048576)
				{
					throw new IOException("Viršyta 512 įrašų / 1 MiB katalogo riba");
				}
				if (num6 + num5 != length - num + num2)
				{
					throw new IOException("ZIP katalogo ribos nepatvirtintos");
				}
				long num7 = num6;
				long num8 = 0L;
				int num9 = 0;
				int num10 = 0;
				int num11 = 0;
				int num12 = 0;
				int num13 = 0;
				for (int i = 0; i < num4; i++)
				{
					check();
					if (num7 + 46 > num6 + num5)
					{
						throw new IOException("Nutrūkęs katalogo įrašas");
					}
					byte[] b3 = Read(input, num7, 46);
					if (U32(b3, 0) != 33639248)
					{
						throw new IOException("Netinkamas katalogo įrašas");
					}
					int num14 = U16(b3, 28);
					int num15 = U16(b3, 30);
					int num16 = U16(b3, 32);
					long num17 = U32(b3, 20);
					long num18 = U32(b3, 24);
					long num19 = U32(b3, 42);
					if (num14 > 1024 || num14 == 0 || num7 + 46 + num14 + num15 + num16 > num6 + num5 || U16(b3, 34) != 0 || num17 == uint.MaxValue || num18 == uint.MaxValue || num19 == uint.MaxValue)
					{
						throw new IOException("Įrašo ribos arba ZIP64 nepatvirtinti");
					}
					string text = Encoding.UTF8.GetString(Read(input, num7 + 46, num14)).Replace('\\', '/');
					if (text.StartsWith("/") || Regex.IsMatch(text, "^[A-Za-z]:") || text.Split('/').Contains("..") || text.IndexOf('\0') >= 0)
					{
						num11++;
					}
					if ((U16(b3, 8) & 1) != 0)
					{
						num9++;
					}
					if (Named(text))
					{
						num10++;
					}
					if (U16(b3, 10) != 0 && U16(b3, 10) != 8)
					{
						num13++;
					}
					if (num18 > 268435456 || (num18 > 1048576 && num18 > Math.Max(1L, num17) * 100))
					{
						num12++;
					}
					num8 += num18;
					num7 += 46L + (long)num14 + num15 + num16;
				}
				if (num7 != num6 + num5)
				{
					throw new IOException("Katalogo įrašų skaičius nesutampa");
				}
				return "ZIP METADUOMENŲ PERŽIŪRA\r\nĮrašų: " + num4 + "; deklaruota išskleista apimtis: " + num8 + " baitų.\r\nŠifruotų: " + num9 + "; vidinių archyvų: " + num10 + "; neįprastų kelių: " + num11 + "; dydžio / suspaudimo perspėjimų: " + num12 + "; kitų metodų: " + num13 + ((num8 <= 2147483648u) ? "" : "\r\nDeklaruotas bendras dydis viršija 2 GiB.") + "\r\nTai struktūros požymiai, ne viruso diagnozė. Turinys NEIŠPAKUOTAS ir jo failų sumos NETIKRINTOS. Rekursinė analizė nevykdoma; šifruotas turinys neperskaitomas. Metaduomenys gali būti klaidingi.";
			}
			catch (IOException ex)
			{
				check();
				return "ZIP PERŽIŪRA NEUŽBAIGTA: " + ex.Message + ". Turinys neišpakuotas ir netikrintas. Tai nėra viruso diagnozė.";
			}
		}
	}
}
