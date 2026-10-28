using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace SargasPC
{
	public sealed class ShieldPanel : Control
	{
		private readonly Timer timer = new Timer
		{
			Interval = 33
		};

		private readonly Stopwatch signal = new Stopwatch();

		private readonly Stopwatch frame = new Stopwatch();

		private Font titleFont;

		private Font detailFont;

		private float angle;

		private string lastMode = "idle";

		public bool Active;
        public string OperationName = "";
        private string CurrentTitle(string mode){return Active ? (OperationName=="Stebimo aplanko failo patikra" ? "Foninė failo patikra" : "Vykdomas pasirinktas veiksmas") : Title(mode);}
        private string CurrentDetail(string mode){return Active ? OperationName+" • rezultatas bus pateiktas baigus" : Detail(mode);}

		public bool Paused;

		public bool Failed;

		public bool Motion = true;

		public int Findings;

		public int Tests;

		public string ActionState = "idle";

		public string Headline = "Pasirinkite patikrą";

		public string Mode => Resolve(Active, Failed, ActionState, Findings, Tests);

		private bool Signal => Mode != "idle" && Mode != "scan" && Mode != "handling";

		private bool CanAnimate
		{
			get
			{
				Form form = FindForm();
				return Motion && !Paused && base.Visible && form != null && form.ContainsFocus && form.WindowState != FormWindowState.Minimized;
			}
		}

		public ShieldPanel()
		{
			DoubleBuffered = true;
			BackColor = Color.FromArgb(20, 36, 60);
			ForeColor = Color.White;
			timer.Tick += delegate
			{
				if (!CanAnimate || (!Active && (!Signal || signal.ElapsedMilliseconds >= 3000)))
				{
					timer.Stop();
					frame.Reset();
					Invalidate();
				}
				else
				{
					angle = (angle + (float)Math.Min(100L, frame.ElapsedMilliseconds) * 0.14f) % 360f;
					frame.Restart();
					Invalidate();
				}
			};
			base.AccessibleName = "Patikros būsena";
		}

		public static string Resolve(bool active, bool failed, string action, int findings, int tests)
		{
			if (failed || action == "UNCONFIRMED")
			{
				return "warning";
			}
			if (active && action != "idle")
			{
				return "handling";
			}
			if (action == "REMOVAL_CONFIRMED")
			{
				return "confirmed";
			}
			if (action == "RESTORED")
			{
				return "restored";
			}
			if (findings > 0)
			{
				return "detected";
			}
			if (active)
			{
				return "scan";
			}
			if (tests > 0)
			{
				return "test";
			}
			if(action=="SCAN_COMPLETED") return "scanned";
			if (action == "COPY_VERIFIED")
			{
				return "copy";
			}
			return "idle";
		}

		private static string Title(string m)
		{
			switch (m)
			{
			case "scanned": return "ClamAV patikra baigta";
			case "scan":
				return "Vyksta patikra";
			case "handling":
				return "Tvarkomas failas…";
			case "confirmed":
				return "Originalo šalinimas patvirtintas";
			case "restored":
				return "Failo atkūrimas patvirtintas";
			case "detected":
				return "Aptikta grėsmės atitiktis";
			case "test":
				return "EICAR bandymo žymuo";
			case "copy":
				return "Sukurta tik šifruota kopija";
			case "warning":
				return "Rezultatas nepatvirtintas";
			default:
				return "Pasirinkite patikrą";
			}
		}

		private static string Detail(string m)
		{
			switch (m)
			{
			case "scanned": return "Grėsmių žymų neaptikta • taikomos patikros ribos";
			case "confirmed":
				return "Šifruota kopija liko • ne viso PC išvalymas";
			case "restored":
				return "Dydis ir SHA-256 sutapo • failas vėl pasiekiamas";
			case "detected":
				return "Neatverkite failo • peržiūrėkite veiksmų centre";
			case "copy":
				return "Originalas galėjo likti pasiekiamas";
			case "test":
				return "Bandymo tekstas, ne tikras virusas";
			case "handling":
				return "Laukiama konkretaus veiksmo patvirtinimo";
			case "warning":
				return "Atverkite ataskaitą • sėkmė nepatvirtinta";
			default:
				return "Rodoma veikla, ne viso kompiuterio saugumo garantija";
			}
		}

		public void RefreshState()
		{
			string mode = Mode;
			if (mode != lastMode)
			{
				lastMode = mode;
				signal.Restart();
			}
			base.AccessibleDescription = ((!Paused) ? "" : "Pristabdyta. ") + CurrentTitle(mode) + ". " + CurrentDetail(mode);
			if (CanAnimate && (Active || (Signal && signal.ElapsedMilliseconds < 3000)))
			{
				if (!timer.Enabled)
				{
					frame.Restart();
				}
				timer.Start();
			}
			else
			{
				timer.Stop();
				frame.Reset();
			}
			Invalidate();
		}

		protected override void OnVisibleChanged(EventArgs e)
		{
			base.OnVisibleChanged(e);
			RefreshState();
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);
			if (base.Width < 2 || base.Height < 2)
			{
				return;
			}
			Graphics graphics = e.Graphics;
			graphics.SmoothingMode = SmoothingMode.AntiAlias;
			using (LinearGradientBrush brush = new LinearGradientBrush(base.ClientRectangle, Color.FromArgb(23, 54, 87), Color.FromArgb(20, 36, 60), 30f))
			{
				graphics.FillRectangle(brush, base.ClientRectangle);
			}
			float num = 68f;
			float num2 = base.Height / 2;
			string mode = Mode;
			bool flag = mode == "confirmed" || mode == "restored";
			Color color;
			if (mode == "detected")
			{
				color = Color.FromArgb(255, 116, 116);
			}
			else if (flag)
			{
				color = Color.FromArgb(117, 223, 164);
			}
			else
			{
				switch (mode)
				{
				case "handling":
					color = Color.FromArgb(189, 157, 255);
					break;
				case "warning":
				case "test":
				case "copy":
					color = Color.FromArgb(255, 187, 112);
					break;
				default:
					color = Color.FromArgb(100, 181, 255);
					break;
				}
			}
			Color color2 = color;
			int alpha = ((!CanAnimate || !Signal || signal.ElapsedMilliseconds >= 3000) ? 255 : (150 + (int)(105.0 * (0.5 + 0.5 * Math.Sin((double)signal.ElapsedMilliseconds * 0.004)))));
			using (Pen pen = new Pen(Color.FromArgb(alpha, color2), 3f))
			{
				graphics.DrawArc(pen, num - 42f, num2 - 42f, 84f, 84f, (!Active || !CanAnimate) ? (-90f) : angle, (!Active) ? 320 : 110);
			}
			using (GraphicsPath graphicsPath = new GraphicsPath())
			{
				graphicsPath.AddPolygon(new PointF[8]
				{
					new PointF(num, num2 - 31f),
					new PointF(num + 25f, num2 - 21f),
					new PointF(num + 22f, num2 + 10f),
					new PointF(num + 12f, num2 + 28f),
					new PointF(num, num2 + 35f),
					new PointF(num - 12f, num2 + 28f),
					new PointF(num - 22f, num2 + 10f),
					new PointF(num - 25f, num2 - 21f)
				});
				using (SolidBrush brush2 = new SolidBrush(Color.FromArgb(36, 119, 212)))
				{
					graphics.FillPath(brush2, graphicsPath);
				}
			}
			graphics.FillRectangle(Brushes.White, num - 4f, num2 - 17f, 8f, 35f);
			graphics.FillRectangle(Brushes.White, num - 16f, num2 - 4f, 32f, 8f);
			if (Signal && mode!="scanned")
			{
				float num3 = num + 28f;
				float num4 = num2 + 28f;
				using (SolidBrush brush3 = new SolidBrush(color2))
				{
					graphics.FillEllipse(brush3, num3 - 13f, num4 - 13f, 26f, 26f);
				}
				using (Pen pen2 = new Pen(Color.FromArgb(20, 36, 60), 3f))
				{
					if (flag)
					{
						graphics.DrawLines(pen2, new PointF[3]
						{
							new PointF(num3 - 7f, num4),
							new PointF(num3 - 2f, num4 + 5f),
							new PointF(num3 + 7f, num4 - 6f)
						});
					}
					else
					{
						graphics.DrawLine(pen2, num3, num4 - 7f, num3, num4 + 2f);
						graphics.DrawEllipse(pen2, num3 - 0.5f, num4 + 7f, 1f, 1f);
					}
				}
			}
			if (titleFont == null)
			{
				titleFont = new Font("Segoe UI", 16f * Math.Max(1f, Font.Size / 10f), FontStyle.Bold);
				detailFont = new Font("Segoe UI", 10f * Math.Max(1f, Font.Size / 10f));
			}
			Font font = titleFont;
			Font font2 = detailFont;
			graphics.DrawString((!Paused) ? CurrentTitle(mode) : "Veiksmas pristabdytas", font, Brushes.White, new RectangleF(132f, 15f, Math.Max(10, base.Width - 145), 62f));
			using (SolidBrush brush4 = new SolidBrush(Color.FromArgb(184, 203, 224)))
			{
				graphics.DrawString(CurrentDetail(mode), font2, brush4, new RectangleF(132f, 80f, Math.Max(10, base.Width - 145), 48f));
			}
		}

		protected override void OnFontChanged(EventArgs e)
		{
			if (titleFont != null)
			{
				titleFont.Dispose();
				titleFont = null;
			}
			if (detailFont != null)
			{
				detailFont.Dispose();
				detailFont = null;
			}
			base.OnFontChanged(e);
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				timer.Dispose();
				if (titleFont != null)
				{
					titleFont.Dispose();
				}
				if (detailFont != null)
				{
					detailFont.Dispose();
				}
			}
			base.Dispose(disposing);
		}
	}
}
