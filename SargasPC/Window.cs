using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SargasPC
{
	public sealed partial class Window : Form
	{
		[CompilerGenerated]
		private sealed class _003CViewReport_003Ec__AnonStorey5
		{
			internal Func<string> read;
		}

		[CompilerGenerated]
		private sealed class _003CViewReport_003Ec__AnonStorey3
		{
			internal Form d;
		}

		[CompilerGenerated]
		private sealed class _003CViewReport_003Ec__AnonStorey6
		{
			[StructLayout(LayoutKind.Auto)]
			internal struct _003CViewReport_003Ec__async4 : IAsyncStateMachine
			{
				internal string _003Cresult_003E__1;

				internal _003CViewReport_003Ec__AnonStorey5 _003C_003Ef__ref_00245;

				internal _003CViewReport_003Ec__AnonStorey3 _003C_003Ef__ref_00243;

				internal _003CViewReport_003Ec__AnonStorey6 _003C_003Ef__ref_00246;

				internal AsyncVoidMethodBuilder _0024builder;

				internal int _0024PC;

				private TaskAwaiter<string> _0024awaiter0;

				public void MoveNext()
				{
					uint num = (uint)_0024PC;
					_0024PC = -1;
					bool flag = false;
					try
					{
						switch (num)
						{
						default:
							return;
						case 0u:
							num = 4294967293u;
							break;
						case 1u:
							break;
						}
						try
						{
							switch (num)
							{
							default:
								_0024awaiter0 = Task.Run(_003C_003Ef__ref_00245.read).GetAwaiter();
								if (!_0024awaiter0.IsCompleted)
								{
									_0024PC = 1;
									flag = true;
									_0024builder.AwaitUnsafeOnCompleted(ref _0024awaiter0, ref this);
									return;
								}
								break;
							case 1u:
								break;
							}
							_003Cresult_003E__1 = _0024awaiter0.GetResult();
							if (!_003C_003Ef__ref_00243.d.IsDisposed)
							{
								_003C_003Ef__ref_00246.box.Text = _003Cresult_003E__1;
							}
						}
						catch
						{
							if (!_003C_003Ef__ref_00243.d.IsDisposed)
							{
								_003C_003Ef__ref_00246.box.Text = "Ataskaita neperskaityta. Ankstesni radiniai ir ataskaita nepakeisti.";
							}
						}
					}
					catch (Exception exception)
					{
						_0024PC = -1;
						_0024builder.SetException(exception);
						return;
					}
					_0024PC = -1;
					_0024builder.SetResult();
				}

				[DebuggerHidden]
				public void SetStateMachine(IAsyncStateMachine stateMachine)
				{
					_0024builder.SetStateMachine(stateMachine);
				}
			}

			internal TextBox box;

			internal _003CViewReport_003Ec__AnonStorey5 _003C_003Ef__ref_00245;

			internal _003CViewReport_003Ec__AnonStorey3 _003C_003Ef__ref_00243;

			internal void _003C_003Em__0(object s, EventArgs e)
			{
				_003C_003Ef__ref_00243.d.Close();
			}

			[AsyncStateMachine(typeof(_003CViewReport_003Ec__async4))]
			internal void _003C_003Em__1(object s, EventArgs e)
			{
				_003CViewReport_003Ec__async4 stateMachine = default(_003CViewReport_003Ec__async4);
				stateMachine._003C_003Ef__ref_00245 = _003C_003Ef__ref_00245;
				stateMachine._003C_003Ef__ref_00243 = _003C_003Ef__ref_00243;
				stateMachine._003C_003Ef__ref_00246 = this;
				stateMachine._0024builder = AsyncVoidMethodBuilder.Create();
				stateMachine._0024builder.Start(ref stateMachine);
			}
		}

		[StructLayout(LayoutKind.Auto)]
		[CompilerGenerated]
		private struct _003CRun_003Ec__async0 : IAsyncStateMachine
		{
			private sealed class _003CRun_003Ec__AnonStorey1A
			{
				internal Func<string> work;

				internal _003CRun_003Ec__async0 _003C_003Ef__ref_00240;

				internal string _003C_003Em__0()
				{
					Thread.CurrentThread.Priority = ThreadPriority.BelowNormal;
					return work();
				}
			}

			internal List<Button>.Enumerator _0024locvar0;

			internal bool external;

			internal string name;

			internal Stopwatch _003Cclock_003E__0;

			internal long _003CcpuAt_003E__0;

			internal bool _003Ccomplete_003E__0;

			internal Func<string> work;

			internal string _003Ctext_003E__1;

			internal Window _0024this;

			internal AsyncVoidMethodBuilder _0024builder;

			internal int _0024PC;

			private _003CRun_003Ec__AnonStorey1A _0024locvar2;

			private TaskAwaiter<string> _0024awaiter0;

			public void MoveNext()
			{
				uint num = (uint)_0024PC;
				_0024PC = -1;
				bool flag = false;
				try
				{
					switch (num)
					{
					default:
						return;
					case 0u:
						_0024locvar2 = new _003CRun_003Ec__AnonStorey1A();
						_0024locvar2._003C_003Ef__ref_00240 = this;
						_0024locvar2.work = work;
						if (_0024this.busy)
						{
							return;
						}
						_0024this.busy = true;
                        _0024this.hero.OperationName = name;
                        _0024this.report.Text = "DABAR VYKDOMA: "+name+"\r\nPradėta: "+DateTime.Now.ToString("G")+"\r\n\r\nDabartinio veiksmo rezultato dar nėra. Ataskaita čia pasirodys jam pasibaigus.\r\nAnkstesni rezultatai pasiekiami patikrų istorijoje.\r\nTai nėra viso kompiuterio patikra ar naujų grėsmių radinys.";
						_0024this.selected = null;
						_0024this.core.Reset();
						_0024this.motion.Enabled = false;
						_0024this.hero.Motion = _0024this.motion.Checked;
						_0024this.hero.Failed = false;
						_0024this.hero.ActionState = "idle";
						_0024this.hero.Active = true;
						_0024this.hero.Paused = false;
						_0024this.hero.Findings = 0;
						_0024this.hero.RefreshState();
						_0024locvar0 = _0024this.actions.GetEnumerator();
						try
						{
							while (_0024locvar0.MoveNext())
							{
								Button current = _0024locvar0.Current;
								current.Enabled = false;
							}
						}
						finally
						{
							((IDisposable)_0024locvar0/*cast due to .constrained prefix*/).Dispose();
						}
						_0024this.pause.Enabled = !external;
						_0024this.cancel.Enabled = true;
						_0024this.status.Text = name + "…";
						_003Cclock_003E__0 = Stopwatch.StartNew();
						_003CcpuAt_003E__0 = Cpu();
						_003Ccomplete_003E__0 = false;
						num = 4294967293u;
						break;
					case 1u:
						break;
					}
					try
					{
						switch (num)
						{
						default:
							var localWork = _0024locvar2.work;
							_0024awaiter0 = Task.Run(delegate
							{
								Thread.CurrentThread.Priority = ThreadPriority.BelowNormal;
								return localWork();
							}).GetAwaiter();
							if (!_0024awaiter0.IsCompleted)
							{
								_0024PC = 1;
								flag = true;
								_0024builder.AwaitUnsafeOnCompleted(ref _0024awaiter0, ref this);
								return;
							}
							break;
						case 1u:
							break;
						}
						_003Ctext_003E__1 = _0024awaiter0.GetResult();
						_003Ccomplete_003E__0 = !_0024this.core.Cancelled;
						_0024this.lastReport = DateTime.Now.ToString("g") + " • " + name + "\r\n\r\n" + _003Ctext_003E__1;
						if (_0024this.core.Cancelled)
						{
							_0024this.selected = null;
							_0024this.lastReport = "NEUŽBAIGTA / ATŠAUKTA\r\n" + _0024this.lastReport;
						}
					}
					catch (OperationCanceledException)
					{
						_0024this.selected = null;
						_0024this.lastReport = "VEIKSMAS ATŠAUKTAS. Nebaigtos apimties saugumo išvados nėra. Ankstesni veiksmai (pvz., sukurta kopija) gali būti jau atlikti.";
					}
					catch (Exception ex2)
					{
						_0024this.selected = null;
						_0024this.core.NotChecked=Math.Max(1,_0024this.core.NotChecked);
                        _0024this.lastReport = DateTime.Now.ToString("g")+" • "+name+"\r\nKontekstas: "+name+(_0024this.core.AttemptPath.Length>0?"\r\nFailas: "+Data.Clean(_0024this.core.AttemptPath):"")+"\r\nVEIKSMAS NEPATVIRTINTAS\r\n" + ((!(ex2 is IOException)) ? "Patikrinkite prieigos teises, duomenų formatą ir programos parengtį. Prisijungimo duomenys neišvedami." : Data.Clean(ex2.Message)) + "\r\nNeigiamos saugumo išvados nėra. Duomenų, kurių nepavyko perskaityti, nevadiname patikrintais.";
					}
					finally
					{
						if (!flag)
						{
							long num2 = Cpu();
							string text = _0024this.core.Outcome(_003Ccomplete_003E__0);
							string text2 = _0024this.core.Metrics(_003Cclock_003E__0.ElapsedMilliseconds, (_003CcpuAt_003E__0 >= 0 && num2 >= 0) ? Math.Max(0L, num2 - _003CcpuAt_003E__0) : (-1));
							_0024this.lastReport = text + "\r\n" + _0024this.lastReport + text2;
							try
							{
								_0024this.core.SaveLast(text + text2);
							}
							catch
							{
								_0024this.lastReport += "\r\nSuvestinės išsaugoti nepavyko.";
							}
							try
							{
								_0024this.core.History(_0024this.lastReport);
							}
							catch
							{
								_0024this.lastReport += "\r\nIstorijos išsaugoti nepavyko; ji neperrašyta. Eksportuokite šią ataskaitą.";
							}
							if (!_0024this.IsDisposed)
							{
								_0024this.report.Text = _0024this.lastReport;
								foreach (Button action in _0024this.actions)
								{
									action.Enabled = true;
								}
								Button pause = _0024this.pause;
								bool enabled = false;
								_0024this.cancel.Enabled = enabled;
								pause.Enabled = enabled;
								_0024this.pause.Text = "Pristabdyti";
								_0024this.status.Text = text;
								_0024this.RefreshReadiness();
							}
							_0024this.busy = false;
							if (!_0024this.IsDisposed)
							{
								_0024this.motion.Enabled = true;
								_0024this.hero.Active = false;
								_0024this.hero.Failed = !_003Ccomplete_003E__0 || _0024this.core.NotChecked>0;
								_0024this.hero.ActionState = _0024this.core.LocalEngineCompleted>0 && _0024this.core.NotChecked==0 && _0024this.core.VisualEvent=="idle" ? "SCAN_COMPLETED" : _0024this.core.VisualEvent;
								_0024this.hero.Tests = _0024this.core.TestFindings;
								_0024this.hero.Findings = _0024this.core.HashFindings + _0024this.core.EngineFindings;
								_0024this.hero.RefreshState();
								_0024this.RememberOutcome();
                                _0024this.MonitorWorkFinished(_003Ccomplete_003E__0);
                                _0024this.NotifyOutcome(_003Ccomplete_003E__0);
                                if (_0024this.hero.Findings > 0 && _0024this.Visible && _0024this.WindowState!=FormWindowState.Minimized)
								{
									MessageBox.Show(_0024this, "Užfiksuotas radinys. Neatverkite įtartino failo; galimas klaidingas perspėjimas. Peržiūrėkite ataskaitą. Pasirinktų failų veiksmus rasite grėsmių ir veiksmų centre.", "Reikia peržiūros", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
								}
							}
						}
					}
				}
				catch (Exception exception)
				{
					_0024PC = -1;
					_0024builder.SetException(exception);
					return;
				}
				_0024PC = -1;
				_0024builder.SetResult();
			}

			[DebuggerHidden]
			public void SetStateMachine(IAsyncStateMachine stateMachine)
			{
				_0024builder.SetStateMachine(stateMachine);
			}
		}

		private Func<PowerLineStatus> powerLine = () => SystemInformation.PowerStatus.PowerLineStatus;

		private FolderMonitor folderMonitor;

		private bool monitorLocalEngine;
        private bool monitorAutoQuarantine;
        private readonly System.Windows.Forms.Timer automationTimer=new System.Windows.Forms.Timer{Interval=30000};
        private readonly System.Windows.Forms.Timer appUpdateTimer=new System.Windows.Forms.Timer{Interval=60000};
        private bool appUpdateCheckRunning;
        private string monitoredDirectory="";

		private long nextMonitorAt;

		private readonly System.Windows.Forms.Timer watchTimer = new System.Windows.Forms.Timer
		{
			Interval = 1000
		};

		private readonly ShieldPanel hero = new ShieldPanel();

		private readonly CheckBox motion = new CheckBox();

		private readonly Core core;

		private readonly List<Button> actions = new List<Button>();

		private readonly TextBox report = new TextBox();

		private readonly Label status = new Label();

		private readonly Label readiness = new Label();

		private readonly Button pause = new Button();

		private readonly Button cancel = new Button();

		private Witness selected;

		private bool busy;

		private string lastReport = "Dar netikrinta. Pasirinkite savitikrą arba failą.";

		private static Color Bg = Color.FromArgb(11, 21, 36);

		private static Color Card = Color.FromArgb(23, 36, 56);

		private static Color Ink = Color.FromArgb(225, 236, 245);

		private static Color Accent = Color.FromArgb(104, 221, 195);

		private float FontScale => (float)Math.Max(100, Math.Min(130, core.Config.FontPercent)) / 100f;

		public Window(Core c)
		{
			core = c;
			ConfigureColors(c.Config.LightTheme);
			using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("SargasPC.Shield.ico"))
			{
				if (stream != null)
				{
					using (Icon icon = new Icon(stream))
					{
						base.Icon = (Icon)icon.Clone();
					}
				}
			}
			Text = "SARGON Security 0.49 — bandomoji";
			base.Width = 1160;
			base.Height = 790;
			MinimumSize = new Size(930, 650);
			base.StartPosition = FormStartPosition.CenterScreen;
			BackColor = Bg;
			ForeColor = Ink;
			Font = new Font("Segoe UI", 10f * FontScale);
			base.AutoScaleMode = AutoScaleMode.Dpi;
			TableLayoutPanel tableLayoutPanel = new TableLayoutPanel
			{
				Dock = DockStyle.Fill,
				ColumnCount = 2,
				RowCount = 3,
				Padding = new Padding(20)
			};
			tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 290f));
			tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
			tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 96f));
			tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
			tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 50f));
			base.Controls.Add(tableLayoutPanel);
			Label control = new Label
			{
				Dock = DockStyle.Fill,
				Text = "◈  SARGON Security\r\n0.49 • Defender valdymas • ClamAV papildomai",
				Font = new Font("Segoe UI", 17f, FontStyle.Bold),
				ForeColor = Accent,
				Padding = new Padding(5)
			};
			tableLayoutPanel.Controls.Add(control, 0, 0);
			tableLayoutPanel.SetColumnSpan(control, 2);
			FlowLayoutPanel flowLayoutPanel = new FlowLayoutPanel
			{
				Dock = DockStyle.Fill,
				FlowDirection = FlowDirection.TopDown,
				WrapContents = false,
				AutoScroll = true,
				Padding = new Padding(0, 0, 14, 0)
			};
			tableLayoutPanel.Controls.Add(flowLayoutPanel, 0, 1);
			FlowLayoutPanel panel = NavSection(flowLayoutPanel, "Windows saugumas ir Defender", expanded: true);
            Add(panel, "Atverti saugumo valdymo langą", delegate
            {
                try
                {
                    string script = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SARGON_Defender_Valdymas.ps1");
                    if (!File.Exists(script))
                    {
                        MessageBox.Show(this, "Saugumo valdymo failas nerastas. Iš naujo įdiekite visą SARGON paketą.");
                        return;
                    }
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), @"WindowsPowerShell\v1.0\powershell.exe"),
                        Arguments = "-NoLogo -NoProfile -WindowStyle Hidden -ExecutionPolicy Bypass -STA -File \"" + script + "\"",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, "Nepavyko atverti saugumo valdymo lango: " + ex.Message);
                }
            });
			Add(panel, "Patikrinti Defender būseną", delegate { Run("Defender būsena", () => core.DefenderStatus(), external: true); });
			Add(panel, "Pereiti nuo McAfee prie nemokamo Defender…", delegate
			{
				string instructions = "Nemokamas antivirusinis McAfee pakaitalas yra Windows integruotas Microsoft Defender. SARGON tik atveria nustatymus ir tikrina būseną; pats apsaugos variklis yra Defender.\r\n\r\nMcAfee prenumeratos automatinio pratęsimo išjungimas jo nepašalina. Jei norite pereiti dabar, Windows Programose pašalinkite McAfee ir, jei prašoma, paleiskite kompiuterį iš naujo. Tada grįžkite čia, pasirinkite „Patikrinti Defender būseną“ ir Windows saugoje patvirtinkite, kad Microsoft Defender yra aktyvus teikėjas, antivirusinė bei apsauga realiuoju laiku įjungtos. Kol to nepatvirtinote, laikykite perėjimą neužbaigtu. SARGON McAfee automatiškai nešalina ir apsaugos neišjungia.";
				if (Confirm("Perėjimas prie Microsoft Defender", instructions + "\r\n\r\nAtverti Windows Programų nustatymus?"))
				{
					try { Process.Start(new ProcessStartInfo("ms-settings:appsfeatures") { UseShellExecute = true }); }
					catch (Exception ex) { MessageBox.Show(this, "Nepavyko atverti Windows Programų nustatymų: " + ex.Message); }
				}
			});
			Add(panel, "Atnaujinti Defender apsaugos bazes", delegate
			{
				if (Confirm("Atnaujinti Microsoft Defender?", "Bus prašoma Defender atsisiųsti naujausias apsaugos bazes. Windows apsaugos nustatymai nekeičiami."))
					Run("Defender bazių atnaujinimas", () => core.DefenderUpdate(), external: true);
			});
			Add(panel, "Greitoji Defender patikra", delegate
			{
				if (Confirm("Pradėti greitąją patikrą?", "Patikrą vykdys Microsoft Defender. Radinius ir veiksmus peržiūrėkite Windows saugumo apsaugos istorijoje."))
					Run("Defender greitoji patikra", () => core.Defender(null), external: true);
			});
			Add(panel, "Viso kompiuterio Defender patikra", delegate
			{
				if (Confirm("Pradėti viso kompiuterio patikrą?", "Microsoft Defender tikrins visą kompiuterį. Tai gali užtrukti kelias valandas; uždarius SARGON, Defender patikra gali tęstis."))
					Run("Defender viso kompiuterio patikra", () => core.DefenderFullScan(), external: true);
			});
			Add(panel, "Peržiūrėti Defender radinių istoriją", delegate { Run("Defender grėsmių istorija", () => core.DefenderThreatHistory(), external: true); });
			panel = NavSection(flowLayoutPanel, "Papildomos patikros", expanded: false);
			Add(panel, "Vietinis antivirusinis variklis…", LocalEngineDialog);
            Add(panel,"Diskų ClamAV patikra ir eiga…",LaunchPcScan);
            Add(panel,"Bendra parengties patikra…",UnifiedReadinessDialog);
            Add(panel,"Windows kontrolė ir apkrova…",StartNativeAudit);
            Add(panel,"Paslaugos radinių valdymas…",LaunchCaseReview);
			Add(panel, "Kas veikia dabar?", Modules);
			Add(panel, "Tęsti aplanko eilę", delegate
			{
				Run("Aplanko eilės tęsimas", () => core.ContinueFolder(32, 120000L));
			});
			Add(panel, "Savitikra ir parengtis", delegate
			{
				Run("Savitikra", () => core.SelfCheck());
			});
			Add(panel, "Tikrinti failą…", ScanFile);
			Add(panel, "Tikrinti aplanką", delegate
			{
				using (FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog())
				{
					folderBrowserDialog.Description = "Iki 4096 failų / 512 katalogų momentinis sąrašas. Sesija iki 32 failų / 2 min., tęsiama tarp failų.";
					if (folderBrowserDialog.ShowDialog(this) == DialogResult.OK && (!core.HasFolderPlan() || Confirm("Pakeisti aplanko eilę?", "Ankstesnė eilė bus pakeista. Išsaugotos ataskaitos lieka.")))
					{
						string p = folderBrowserDialog.SelectedPath;
						Run("Aplanko patikra", () => core.Folder(p));
					}
				}
			});
			Add(panel, "Paruošimo vedlys", Wizard);
			Add(panel, "Atverti Windows saugumą", delegate
			{
				try
				{
					Process.Start(new ProcessStartInfo("windowsdefender://threat")
					{
						UseShellExecute = true
					});
				}
				catch
				{
					MessageBox.Show(this, "Atverkite Windows nustatymai → Privatumas ir sauga → Windows saugumas.");
				}
			});
			panel = NavSection(flowLayoutPanel, "Radinių tvarkymas", expanded: false);
			Add(panel, "Grėsmių ir veiksmų centras", ResponseCenter);
			Add(panel, "Karantino veiksmų žurnalas", ShowJournal);
			Add(panel, "Šifruoti / izoliuoti…", Isolation);
			Add(panel, "Šifruotų kopijų saugykla…", Vault);
			Add(panel, "Radinių atmintinė", FindingsView);
			panel = NavSection(flowLayoutPanel, "Istorija ir ataskaitos", expanded: false);
			Add(panel, "Patikrų istorija", HistoryView);
			Add(panel, "Eksportuoti ataskaitą…", Export);
			panel = NavSection(flowLayoutPanel, "Tinklas ir bazės", expanded: false);
			Add(panel, "Windows Defender būsena", delegate
			{
				Run("Defender būsena", () => core.DefenderStatus(), external: true);
			});
			Add(panel, "Tikrinti nuorodą vietoje…", UrlDialog);
			Add(panel, "Importuoti domenų sąrašą…", ImportDomains);
			Add(panel, "Pasirašyto atnaujinimo patikra…", VerifyUpdate);
			Add(panel, "Tikrinti SARGON programos naujinimus…", delegate { CheckAppUpdate(interactive: true); });
			Add(panel, "Importuoti pasirašytą bazę…", delegate
			{
				if (!File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "publisher.der")))
				{
					MessageBox.Show(this, "Patikimas publisher.der bazės leidėjo raktas nesukonfigūruotas. Įrankis ir savininko instrukcija pateikti pakete.");
				}
				else
				{
					string p = Pick();
					if (p != null)
					{
						Run("Pasirašytos bazės importas", () => core.ImportSignedHashes(p));
					}
				}
			});
			Add(panel, "Patikrinti serverio ryšį", delegate
			{
				if (Confirm("Patikrinti HTTPS ryšį?", "Į jūsų serverio /v1/health bus siunčiamas prieigos raktas ir IP. Failas ar suma nesiunčiami."))
				{
					Run("Serverio ryšys", () => core.CheckServer());
				}
			});
			Add(panel, "Windows Defender…", delegate
			{
				Witness target = selected;
				if (Confirm("Naudoti Windows Defender?", ((target != null) ? ("Bus paprašyta pasirinkto failo patikros: " + target.Path) : "Bus paprašyta GREITOSIOS sistemos patikros.") + "\r\nWindows Defender gali pats karantinuoti pagal savo politiką. Šio lango atšaukimas nebūtinai sustabdo sistemos skenavimą. Esami apsaugos nustatymai nekeičiami."))
				{
					Run("Windows Defender", () => core.Defender(target), external: true);
				}
			});
			Add(panel, "Failo reputacija…", delegate
			{
				Witness target = Target();
				if (target != null && Confirm("Siųsti SHA-256?", "MalwareBazaar gaus tik failo sumą, jūsų Auth-Key ir IP. Turinys ir pavadinimas nesiunčiami. Reikia jūsų rakto ir naudojimo teisių. Nežinomas failas nėra saugus."))
				{
					Run("Reputacija", delegate
					{
						string result = core.Reputation(target);
						selected = target;
						return result;
					});
				}
			});
			Add(panel, "ClamAV serveris…", delegate
			{
				Witness target = Target();
				if (target != null && Confirm("Siųsti failo TURINĮ?", "Iki 32 MiB failo turinys bus išsiųstas į jūsų serverį: " + core.Config.Endpoint + "\r\nServerio administratorius galės jį perskaityti. Reikia jūsų veikiančio HTTPS serverio ir rakto. Automatinio siuntimo nėra."))
				{
					Run("Serverio patikra", delegate
					{
						string result = core.Server(target);
						selected = target;
						return result;
					});
				}
			});
			Add(panel, "Papildyti SHA-256 bazę…", delegate
			{
				string p = Pick();
				if (p != null && Confirm("Papildyti rankinę bazę?", "Esamos sumos bus išsaugotos, naujos pridėtos, dublikatai pašalinti. Naudokite patikimo šaltinio SHA-256 tekstinį sąrašą; leidėjo parašas netikrinamas."))
				{
					Run("Bazės papildymas", () => "Rankinėje bazėje iš viso sumų: " + core.AppendHashes(p));
				}
			});
			Add(panel, "Importuoti SHA-256…", delegate
			{
				string p = Pick();
				if (p != null && Confirm("Pakeisti rankinę bazę?", "Importas pakeis rankinį SHA-256 sąrašą. Leidėjo parašas netikrinamas. Naudokite tik patikimą šaltinį; bazės buvimas neįrodo jos kokybės."))
				{
					Run("Bazės importas", () => "Importuota sumų: " + core.Import(p));
				}
			});
            Add(panel, "MalwareBazaar sumos (papildoma)…", FeedDialog);
			panel = NavSection(flowLayoutPanel, "Nuostatos", expanded: false);
			Add(panel, "Išvaizda ir teksto dydis", Appearance);
			Add(panel, "Stebėti pasirinktą aplanką…", MonitorSettings);
			Add(panel, "Maitinimas ir aplanko patikros", PowerSettings);
            Add(panel,"Automatika ir stebėjimo atkūrimas…",AutomationSettings);
            Add(panel,"Windows paleistis ir foninis režimas…",BackgroundSettings);
            Add(panel,"Išsaugoti perspėjimai…",PendingAlertsDialog);
            Add(panel,"Atskiros paslaugos būsena…",ServiceStatusDialog);
			Add(panel, "Ryšio nustatymai…", Network);
			TableLayoutPanel tableLayoutPanel2 = new TableLayoutPanel
			{
				Dock = DockStyle.Fill,
				ColumnCount = 1,
				RowCount = 5
			};
			tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Absolute, 135f));
			tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Absolute, 170f));
			tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.AutoSize));
			tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Absolute, 36f));
			tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
			tableLayoutPanel.Controls.Add(tableLayoutPanel2, 1, 1);
			readiness.Dock = DockStyle.Fill;
			readiness.BackColor = Card;
			readiness.Padding = new Padding(12);
			readiness.AutoEllipsis = false;
			RefreshReadiness();
			hero.Dock = DockStyle.Fill;
			tableLayoutPanel2.Controls.Add(hero, 0, 0);
			tableLayoutPanel2.Controls.Add(readiness, 0, 1);
			motion.Text = "Animuoti patikros eigą";
			motion.Checked = core.Config.Motion;
			motion.AutoSize = true;
			motion.CheckedChanged += delegate
			{
				core.Config.Motion = motion.Checked;
				hero.Motion = motion.Checked;
				hero.RefreshState();
				try
				{
					core.SaveSettings();
				}
				catch
				{
					status.Text = "Išvaizdos nuostatos neišsaugotos.";
				}
			};
			tableLayoutPanel2.Controls.Add(motion, 0, 3);
			FlowLayoutPanel flowLayoutPanel2 = new FlowLayoutPanel
			{
				Dock = DockStyle.Fill,
				AutoSize = true,
				WrapContents = false
			};
			tableLayoutPanel2.Controls.Add(flowLayoutPanel2, 0, 2);
			Button button = new Button
			{
				Text = "Tikrinti…",
				Width = 180
			};
			Style(button);
			button.Click += delegate
			{
				if (!busy)
				{
					ScanFile();
				}
			};
			flowLayoutPanel2.Controls.Add(button);
			actions.Add(button);
			Button button2 = new Button
			{
				Text = "Peržiūrėti radinius",
				Width = 205
			};
			Style(button2);
			button2.Click += delegate
			{
				if (!busy)
				{
					FindingsChoice();
				}
			};
			var updateButton=new Button{Text="Atnaujinti…",Width=180};Style(updateButton);updateButton.Click+=(s,e)=>{if(!busy)SimpleUpdates();};flowLayoutPanel2.Controls.Add(updateButton);actions.Add(updateButton);
			flowLayoutPanel2.Controls.Add(button2);
			actions.Add(button2);
			base.Resize += delegate
			{
				hero.RefreshState();
			};
			lastReport = core.LastOperation();
			report.Dock = DockStyle.Fill;
			report.Multiline = true;
			report.AutoSize = false;
			report.ReadOnly = true;
			report.BackColor = Bg;
			report.ForeColor = Ink;
			report.Font = new Font("Consolas", 10f);
			report.ScrollBars = ScrollBars.Both;
			report.WordWrap = true;
			report.BorderStyle = BorderStyle.FixedSingle;
			report.Text = lastReport;
			tableLayoutPanel2.Controls.Add(report, 0, 4);
			FlowLayoutPanel flowLayoutPanel3 = new FlowLayoutPanel
			{
				Dock = DockStyle.Fill,
				FlowDirection = FlowDirection.LeftToRight
			};
			tableLayoutPanel.Controls.Add(flowLayoutPanel3, 0, 2);
			tableLayoutPanel.SetColumnSpan(flowLayoutPanel3, 2);
			status.Width = 620;
			status.Height = 40;
			status.Text = "Aplanko stebėjimas išjungtas • įjunkite nuostatose";
			flowLayoutPanel3.Controls.Add(status);
			Style(pause);
			pause.Text = "Pristabdyti";
			pause.Width = 120;
			pause.Enabled = false;
			pause.Click += delegate
			{
				core.Paused = !core.Paused;
				pause.Text = ((!core.Paused) ? "Pristabdyti" : "Tęsti");
				hero.Paused = core.Paused;
				hero.RefreshState();
			};
			flowLayoutPanel3.Controls.Add(pause);
			Style(cancel);
			cancel.Text = "Atšaukti";
			cancel.Width = 110;
			cancel.Enabled = false;
			cancel.Click += delegate
			{
				core.Cancelled = true;
				StopMonitor(true);
				status.Text = "Atšaukiama; tinklo operacija gali laukti iki skirto laiko ribos.";
			};
			flowLayoutPanel3.Controls.Add(cancel);
			watchTimer.Tick += delegate
			{
                AutomationTick();
				MonitorTick();
			};
			automationTimer.Tick+=delegate{ServiceStatusTick();HealthTick();AutomationTick();};
            InitializeReliability();InitializeTray();
            appUpdateTimer.Tick += delegate { appUpdateTimer.Stop(); CheckAppUpdate(interactive: false); appUpdateTimer.Interval = (int)TimeSpan.FromHours(24).TotalMilliseconds; appUpdateTimer.Start(); };
			base.Shown+=delegate{ApplyBackgroundLaunch();RestoreSavedMonitor();automationTimer.Start();ServiceStatusTick();HealthTick();AutomationTick();appUpdateTimer.Start();};
            base.FormClosed += delegate
			{
                DisposeTray();automationTimer.Stop();automationTimer.Dispose();appUpdateTimer.Stop();appUpdateTimer.Dispose();
				StopMonitor();
				watchTimer.Dispose();
			};
			core.Progress = delegate(string text)
			{
				if (!base.IsDisposed)
				{
					BeginInvoke((Action)delegate
					{
						status.Text = text;
						hero.ActionState = core.VisualEvent;
						hero.RefreshState();
					});
				}
			};
			base.Activated += delegate
			{
				hero.RefreshState();
			};
			base.Deactivate += delegate
			{
				hero.RefreshState();
			};
			base.FormClosing += delegate(object s, FormClosingEventArgs e)
			{
				if (e.Cancel) return;
                if (busy)
				{
					if (!Confirm("Uždaryti?", "Veiksmas bus nutrauktas. Sistemos Defender patikra gali tęstis. Neužbaigtas rezultatas nėra saugumo patvirtinimas."))
					{
						e.Cancel = true;
					}
					else
					{
						core.Cancelled = true;
					}
				}
			};
		}

		private string MonitorPowerHold()
		{
			if (!core.Config.MonitorOnAc)
			{
				return "";
			}
			try
			{
				PowerLineStatus powerLineStatus = powerLine();
				return Data.BackgroundPowerHold(chargingOnly: true, powerLineStatus == PowerLineStatus.Online, powerLineStatus == PowerLineStatus.Online || powerLineStatus == PowerLineStatus.Offline);
			}
			catch
			{
				return Data.BackgroundPowerHold(chargingOnly: true, online: false, known: false);
			}
		}

		private void PowerSettings()
		{
			Form d = new Form
			{
				Text = "Maitinimas ir aplanko patikros",
				Width = 700,
				Height = 420,
				StartPosition = FormStartPosition.CenterParent
			};
			try
			{
				CheckBox onlyAc = new CheckBox
				{
					Left = 18,
					Top = 20,
					Width = 650,
					Height = 68,
					Text = "Aplanko stebėjimo patikras vykdyti tik prijungus maitinimą",
					Checked = core.Config.MonitorOnAc
				};
				d.Controls.Add(onlyAc);
				d.Controls.Add(new Label
				{
					Left = 18,
					Top = 100,
					Width = 650,
					Height = 175,
					Text = "Numatyta — įjungta. Naudojant bateriją ar nepatvirtinus maitinimo būsenos, laukiančių failų eilė lieka. Prijungus maitinimą darbai tęsiami pagal esamus ribojimus. Eilė ribota: įvykiai gali būti praleisti.\r\nRankinį tikrinimą galite pradėti ir su baterija. Jau pradėto failo ar ClamAV veiksmo atjungus maitinimą tai nesustabdo."
				});
				Button button = new Button();
				button.Left = 18;
				button.Top = 300;
				button.Width = 240;
				button.Height = 42;
				button.Text = "Išsaugoti";
				Button button2 = button;
				d.Controls.Add(button2);
				button2.Click += delegate
				{
					bool monitorOnAc = core.Config.MonitorOnAc;
					try
					{
						core.Config.MonitorOnAc = onlyAc.Checked;
						core.SaveSettings();
						d.Close();
					}
					catch
					{
						core.Config.MonitorOnAc = monitorOnAc;
						MessageBox.Show(d, "Maitinimo nuostatų išsaugoti nepavyko.");
					}
				};
				ThemeDialog(d);
				d.ShowDialog(this);
			}
			finally
			{
				if (d != null)
				{
					((IDisposable)d).Dispose();
				}
			}
		}

		private void Modules()
		{
			ViewReport("Kas veikia dabar?", () => core.ModuleReadiness());
		}

		private void HistoryView()
		{
			ViewReport("Istorinės ataskaitos", () => core.PastReports() + "\r\nIstorinės kopijos nepatvirtina dabartinės failų būklės.");
		}

		private void FindingsView()
		{
			ViewReport("Istoriniai radiniai", () => core.Findings() + "\r\nTai istoriniai įrašai, ne dabartinio užkrėtimo ar neutralizavimo patvirtinimas.");
		}

		private void ViewReport(string title, Func<string> read)
		{
			_003CViewReport_003Ec__AnonStorey5 _003CViewReport_003Ec__AnonStorey = new _003CViewReport_003Ec__AnonStorey5();
			_003CViewReport_003Ec__AnonStorey.read = read;
			if (busy)
			{
				return;
			}
			_003CViewReport_003Ec__AnonStorey3 _003CViewReport_003Ec__AnonStorey2 = new _003CViewReport_003Ec__AnonStorey3();
			_003CViewReport_003Ec__AnonStorey2.d = new Form
			{
				Text = title,
				Width = 780,
				Height = 680,
				MinimumSize = new Size(600, 460),
				StartPosition = FormStartPosition.CenterParent
			};
			try
			{
				_003CViewReport_003Ec__AnonStorey6 CS_0024_003C_003E8__locals9 = new _003CViewReport_003Ec__AnonStorey6();
				CS_0024_003C_003E8__locals9._003C_003Ef__ref_00245 = _003CViewReport_003Ec__AnonStorey;
				CS_0024_003C_003E8__locals9._003C_003Ef__ref_00243 = _003CViewReport_003Ec__AnonStorey2;
				CS_0024_003C_003E8__locals9.box = new TextBox
				{
					Multiline = true,
					ReadOnly = true,
					ScrollBars = ScrollBars.Both,
					Dock = DockStyle.Fill,
					Text = "Skaitoma…"
				};
				Button button = new Button();
				button.Text = "Uždaryti";
				button.Dock = DockStyle.Bottom;
				button.Height = 46;
				Button button2 = button;
				button2.Click += delegate
				{
					CS_0024_003C_003E8__locals9._003C_003Ef__ref_00243.d.Close();
				};
				_003CViewReport_003Ec__AnonStorey2.d.Controls.Add(CS_0024_003C_003E8__locals9.box);
				_003CViewReport_003Ec__AnonStorey2.d.Controls.Add(button2);
				ThemeDialog(_003CViewReport_003Ec__AnonStorey2.d);
				CS_0024_003C_003E8__locals9.box.Font = new Font("Segoe UI", 11f * FontScale);
				_003CViewReport_003Ec__AnonStorey2.d.Shown += [AsyncStateMachine(typeof(_003CViewReport_003Ec__AnonStorey6._003CViewReport_003Ec__async4))] (object s, EventArgs e) =>
				{
					_003CViewReport_003Ec__AnonStorey6._003CViewReport_003Ec__async4 stateMachine = default(_003CViewReport_003Ec__AnonStorey6._003CViewReport_003Ec__async4);
					stateMachine._003C_003Ef__ref_00245 = CS_0024_003C_003E8__locals9._003C_003Ef__ref_00245;
					stateMachine._003C_003Ef__ref_00243 = CS_0024_003C_003E8__locals9._003C_003Ef__ref_00243;
					stateMachine._003C_003Ef__ref_00246 = CS_0024_003C_003E8__locals9;
					stateMachine._0024builder = AsyncVoidMethodBuilder.Create();
					stateMachine._0024builder.Start(ref stateMachine);
				};
				_003CViewReport_003Ec__AnonStorey2.d.ShowDialog(this);
			}
			finally
			{
				if (_003CViewReport_003Ec__AnonStorey2.d != null)
				{
					((IDisposable)_003CViewReport_003Ec__AnonStorey2.d).Dispose();
				}
			}
		}

		private static void ConfigureColors(bool light)
		{
			Bg = ((!light) ? Color.FromArgb(11, 21, 36) : Color.FromArgb(243, 246, 251));
			Card = ((!light) ? Color.FromArgb(23, 36, 56) : Color.White);
			Ink = ((!light) ? Color.FromArgb(225, 236, 245) : Color.FromArgb(23, 36, 56));
			Accent = ((!light) ? Color.FromArgb(104, 221, 195) : Color.FromArgb(23, 105, 189));
		}

		private void RefreshReadiness()
		{
			readiness.Text = (string.IsNullOrEmpty(healthText)?"":healthText+"\r\n")+Overview();
			readiness.ForeColor = ((!readiness.Text.Contains("DĖMESIO") && !readiness.Text.Contains("neperskaitoma")) ? Ink : Color.FromArgb(230, 165, 45));
		}

		private string Overview()
		{
			int count;
			try
			{
				count = core.Database().Count;
			}
			catch
			{
				return "APSAUGOS BŪSENA\r\nBazė neperskaitoma; vietinė sumų patikra neparengta. Esamos antivirusinės neišjunkite.";
			}
			return "APSAUGOS BŪSENA\r\n" + ((count != 0) ? ("Vietinė bazė: " + count + " sumų; jų kokybė nėra patvirtinta.") : "DĖMESIO: vietinė bazė tuščia (0 sumų). Vietinė patikra neturi žinomų grėsmių sumų.") + "\r\nClamAV: " + (core.HasLocalEngine ? ("vietinis įdiegtas; " + core.LocalDatabaseReadiness()+"; "+core.LocalEngineCheckStatus()+"; "+core.DetectionControlStatus()) : ((!string.IsNullOrWhiteSpace(core.Config.Endpoint)) ? "jūsų serveris; ryšio patikra atskira" : "neparuoštas")) + ". Defender būsena tikrinama atskirai.\r\nWindows saugume tikrinkite aktyvų teikėją. Sargas nėra esamos antivirusinės pakaitalas.";
		}

		private void ApplyAppearance()
		{
			ConfigureColors(core.Config.LightTheme);
			BackColor = Bg;
			ForeColor = Ink;
			Font = new Font("Segoe UI", 10f * FontScale);
			Action<Control> theme = null;
			theme = delegate(Control c)
			{
				if (c == hero)
				{
					c.Font = Font;
					return;
				}
				c.ForeColor = Ink;
				if (c is Button)
				{
					Style((Button)c);
				}
				else if (c == report)
				{
					c.BackColor = Bg;
					c.Font = new Font("Consolas", 10f * FontScale);
					c.AutoSize = false;
					report.Multiline = true;
				}
				else if (c == readiness)
				{
					c.BackColor = Card;
					c.Font = Font;
				}
				else if (c is Label && c.Text.StartsWith("◈"))
				{
					c.ForeColor = Accent;
					c.Font = new Font("Segoe UI", 17f * FontScale, FontStyle.Bold);
				}
				else if (c is Label || c is CheckBox)
				{
					c.Font = Font;
				}
				else
				{
					c.BackColor = Bg;
				}
				foreach (Control control in c.Controls)
				{
					theme(control);
				}
			};
			foreach (Control control2 in base.Controls)
			{
				theme(control2);
			}
			report.Dock = DockStyle.Fill;
			report.Parent.PerformLayout();
			PerformLayout();
			RefreshReadiness();
			Invalidate(invalidateChildren: true);
		}

		private void Appearance()
		{
			Form d = new Form
			{
				Text = "Tema ir teksto dydis",
				Width = 500,
				Height = 280,
				StartPosition = FormStartPosition.CenterParent
			};
			try
			{
				CheckBox light = new CheckBox
				{
					Left = 20,
					Top = 25,
					Width = 430,
					Text = "Šviesi tema",
					Checked = core.Config.LightTheme
				};
				NumericUpDown size = new NumericUpDown
				{
					Left = 20,
					Top = 105,
					Width = 130,
					Minimum = 100m,
					Maximum = 130m,
					Increment = 5m,
					Value = Math.Max(100, Math.Min(130, core.Config.FontPercent))
				};
				d.Controls.Add(light);
				d.Controls.Add(new Label
				{
					Left = 20,
					Top = 73,
					Width = 430,
					Text = "Teksto dydis, procentais (100–130)"
				});
				d.Controls.Add(size);
				Button button = new Button();
				button.Left = 20;
				button.Top = 163;
				button.Width = 430;
				button.Text = "Taikyti ir išsaugoti";
				Button button2 = button;
				d.Controls.Add(button2);
				button2.Click += delegate
				{
					core.Config.LightTheme = light.Checked;
					core.Config.FontPercent = (int)size.Value;
					try
					{
						core.SaveSettings();
						d.Close();
						ApplyAppearance();
					}
					catch
					{
						MessageBox.Show(d, "Išvaizdos nuostatų išsaugoti nepavyko.");
					}
				};
				ThemeDialog(d);
				d.ShowDialog(this);
			}
			finally
			{
				if (d != null)
				{
					((IDisposable)d).Dispose();
				}
			}
		}

		private void ShowJournal()
		{
			Form form = new Form();
			form.Text = "Karantino veiksmų rezultatai";
			form.Width = 850;
			form.Height = 570;
			form.StartPosition = FormStartPosition.CenterParent;
			using (Form form2 = form)
			{
				TextBox textBox = new TextBox();
				textBox.Dock = DockStyle.Fill;
				textBox.ReadOnly = true;
				textBox.Multiline = true;
				textBox.ScrollBars = ScrollBars.Both;
				textBox.Text = core.ActionsReport();
				TextBox value = textBox;
				form2.Controls.Add(value);
				ThemeDialog(form2);
				form2.ShowDialog(this);
			}
		}

		private void UrlDialog()
		{
			Form d = new Form
			{
				Text = "Nuorodos peržiūra be tinklo",
				Width = 720,
				Height = 560,
				StartPosition = FormStartPosition.CenterParent
			};
			try
			{
				d.Controls.Add(new Label
				{
					Left = 18,
					Top = 18,
					Width = 665,
					Height = 150,
					Text = "Svetainė nebus aplankyta. Jei žinote oficialų domeną iš patikimo šaltinio, įveskite jį be https:// ir kelio. Palyginimas atskirs, pvz., example.com nuo example.com.svetimas.lt. Sutapimas nepatvirtina saugumo; peradresavimai ir puslapio turinys netikrinami."
				});
				d.Controls.Add(new Label
				{
					Left = 18,
					Top = 175,
					Width = 665,
					Height = 32,
					Text = "Tikrinama HTTP(S) nuoroda"
				});
				TextBox field = new TextBox
				{
					Left = 18,
					Top = 215,
					Width = 665,
					MaxLength = 4096
				};
				d.Controls.Add(field);
				d.Controls.Add(new Label
				{
					Left = 18,
					Top = 265,
					Width = 665,
					Height = 60,
					Text = "Tikrasis domenas, pvz. example.com (nebūtina)"
				});
				TextBox expected = new TextBox
				{
					Left = 18,
					Top = 330,
					Width = 665,
					MaxLength = 253
				};
				d.Controls.Add(expected);
				Button button = new Button();
				button.Left = 18;
				button.Top = 400;
				button.Width = 665;
				button.Height = 48;
				button.Text = "Tikrinti vietoje";
				Button button2 = button;
				d.Controls.Add(button2);
				button2.Click += delegate
				{
					string raw = field.Text;
					string known = expected.Text;
					d.Close();
					Run("Nuorodos vietinė peržiūra", delegate
					{
						string path = Path.Combine(core.Root, "domains.txt");
						string raw2 = "";
						if (File.Exists(path))
						{
							using (FileStream input = File.OpenRead(path))
							{
								raw2 = Encoding.UTF8.GetString(Data.Bounded(input, 1048576, core.Check));
							}
						}
						core.NotChecked++;
						return UrlRules.Compare(raw, UrlRules.Parse(raw2), known);
					});
				};
				ThemeDialog(d);
				d.ShowDialog(this);
			}
			finally
			{
				if (d != null)
				{
					((IDisposable)d).Dispose();
				}
			}
		}

		private void ImportDomains()
		{
			string source = Pick();
			if (source == null || !Confirm("Pakeisti domenų taisykles?", "Rankinis sąrašas be leidėjo parašo. Iki 5000 domenų, po vieną eilutėje. Naudokite patikimą šaltinį. Šis importas nekeičia naršyklės plėtinio taisyklių."))
			{
				return;
			}
			Run("Domenų taisyklių importas", delegate
			{
				string raw;
				using (FileStream input = File.OpenRead(source))
				{
					raw = Encoding.UTF8.GetString(Data.Bounded(input, 1048576, core.Check));
				}
				HashSet<string> hashSet = UrlRules.Parse(raw);
				Data.Atomic(Path.Combine(core.Root, "domains.txt"), string.Join("\n", hashSet.OrderBy((string x) => x)));
				return "Rankinių domenų taisyklių: " + hashSet.Count + ". Tai nėra savarankiškas svetainių reputacijos variklis ar naršyklės blokavimas.";
			});
		}

		private void VerifyUpdate()
		{
			string keyPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "update-publisher.der");
			if (!File.Exists(keyPath))
			{
				MessageBox.Show(this, "Savininko viešasis update-publisher.der raktas šiame leidime nesukonfigūruotas. Patvirtinimas išjungtas. Pakete pateiktas leidimo pasirašymo įrankis ir instrukcija. Privatus raktas programai neperduodamas.", "Leidėjas nesukonfigūruotas");
				return;
			}
			MessageBox.Show(this, "Pasirinkite savininko pasirašytą manifestą, po jo — atnaujinimo EXE. Programa tik patikrins failą; automatiškai jo nepaleis.");
			string manifest = Pick();
			if (manifest == null)
			{
				return;
			}
			string binary = Pick();
			if (binary == null)
			{
				return;
			}
			Run("Pasirašyto atnaujinimo patikra", delegate
			{
				byte[] publisher;
				using (FileStream input = File.OpenRead(keyPath))
				{
					publisher = Data.Bounded(input, 4096, core.Check);
				}
				byte[] envelope;
				using (FileStream input2 = File.OpenRead(manifest))
				{
					envelope = Data.Bounded(input2, 4096, core.Check);
				}
				long now = (long)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;
				UpdateOffer updateOffer = UpdateGate.Manifest(envelope, publisher, "windows", ReleaseIdentity.Version, now);
				using (FileStream input3 = new FileStream(binary, FileMode.Open, FileAccess.Read, FileShare.Read))
				{
					UpdateGate.Binary(updateOffer, input3, core.Check);
				}
				return "Leidėjo parašas, versija, galiojimas ir pasirinkto EXE dydis / SHA-256 sutapo. Versija: " + updateOffer.Version + ". Failas automatiškai NEPALEISTAS. Tai nėra Authenticode, antivirusinio variklio ar Windows diegimo patikra.";
			});
		}

        private void CheckAppUpdate(bool interactive)
        {
            string keyPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "update-publisher.der");
            if (!File.Exists(keyPath))
            {
                if (interactive) MessageBox.Show(this, "Programos naujinimų parašo raktas šiame leidime nesukonfigūruotas. Automatinis naujinimas lieka išjungtas, kol savininko viešasis raktas įdedamas į diegiklį.", "Naujinimai nesukonfigūruoti");
                return;
            }
            long nowTicks = DateTime.UtcNow.Ticks;
            if (!interactive && core.Config.LastAppUpdateCheckUtcTicks > 0 && nowTicks >= core.Config.LastAppUpdateCheckUtcTicks && nowTicks - core.Config.LastAppUpdateCheckUtcTicks < TimeSpan.FromHours(24).Ticks) return;
            if (appUpdateCheckRunning) return;
            appUpdateCheckRunning = true;
            if (interactive) status.Text = "Tikrinamas pasirašytas SARGON leidimas…";
            Task.Run(delegate
            {
                try
                {
                    byte[] publisher;
                    using (FileStream input = File.OpenRead(keyPath)) publisher = Data.Bounded(input, 4096, delegate { });
                    long unixNow = (long)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;
                    ReleaseUpdate release = ReleaseUpdateClient.CheckLatest(publisher, ReleaseIdentity.Version, unixNow, delegate { });
                    return Tuple.Create(release, publisher, (Exception)null);
                }
                catch (Exception error) when (error is IOException || error is UnauthorizedAccessException || error is WebException || error is CryptographicException || error is FormatException || error is InvalidOperationException)
                {
                    return Tuple.Create((ReleaseUpdate)null, (byte[])null, error);
                }
            }).ContinueWith(delegate(Task<Tuple<ReleaseUpdate, byte[], Exception>> task)
            {
                if (IsDisposed || !IsHandleCreated) return;
                BeginInvoke((Action)delegate
                {
                    appUpdateCheckRunning = false;
                    core.Config.LastAppUpdateCheckUtcTicks = DateTime.UtcNow.Ticks;
                    try { core.SaveSettings(); } catch (Exception error) when (error is IOException || error is UnauthorizedAccessException) { }
                    if (task.IsFaulted || task.Result.Item3 != null)
                    {
                        if (interactive) MessageBox.Show(this, "Naujinimo patikra nepavyko. Nieko neatsisiųsta ir neįdiegta.\r\n" + Data.Clean(task.IsFaulted ? task.Exception.GetBaseException().Message : task.Result.Item3.Message), "SARGON naujinimas");
                        return;
                    }
                    ReleaseUpdate release = task.Result.Item1;
                    if (release == null || release.Offer == null)
                    {
                        if (interactive) MessageBox.Show(this, "Naujesnio stabilaus SARGON leidimo nėra.", "SARGON naujinimas");
                        return;
                    }
                    if (!Confirm("Rastas pasirašytas SARGON naujinimas", "Nauja versija: " + release.Offer.Version + "\r\n\r\nSARGON atsisiųs Windows programos diegiklį iš oficialios GitHub saugyklos, patikrins parašą, dydį ir SHA-256, tada uždarys programą ir paleis diegimą. Windows paleisties pasirinkimas išliks. Bendros Windows paslaugos atnaujinamos atskirai. Diegiklis nėra Authenticode pasirašytas. Tęsti?")) return;
                    status.Text = "Atsisiunčiamas ir tikrinamas pasirašytas naujinimas…";
                    Task.Run(delegate
                    {
                        string root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TelefonoSargasPC", "Updates");
                        return ReleaseUpdateClient.DownloadVerified(release, task.Result.Item2, root, delegate { });
                    }).ContinueWith(delegate(Task<string> download)
                    {
                        if (IsDisposed || !IsHandleCreated) return;
                        BeginInvoke((Action)delegate
                        {
                            if (download.IsFaulted)
                            {
                                MessageBox.Show(this, "Naujinio atsisiuntimas ar SHA-256 patikra nepavyko. Diegiklis nepaleistas.\r\n" + Data.Clean(download.Exception.GetBaseException().Message), "SARGON naujinimas");
                                return;
                            }
                            try
                            {
                                Process.Start(new ProcessStartInfo(download.Result, "/S /SARGON-UPDATE") { UseShellExecute = true });
                                exitRequested = true;
                                Close();
                            }
                            catch (Exception error) when (error is IOException || error is UnauthorizedAccessException || error is System.ComponentModel.Win32Exception)
                            {
                                exitRequested = false;
                                MessageBox.Show(this, "Patikrintas naujinys išsaugotas, bet diegiklio paleisti nepavyko: " + Data.Clean(error.Message), "SARGON naujinimas");
                            }
                        });
                    });
                });
            });
        }

		private void StopMonitor(bool disableSaved = false)
		{
			watchTimer.Stop();
            if(disableSaved){core.Config.SavedWatchEnabled=false;try{core.SaveSettings();}catch(Exception e) when(e is IOException||e is UnauthorizedAccessException){status.Text="Stebėjimas išjungtas šiai sesijai; nuostatos neišsaugotos.";}}
			if (folderMonitor != null)
			{
				folderMonitor.Dispose();
				folderMonitor = null;
			}
		}

        private void MonitorSettings()
        {
            if(folderMonitor!=null){StopMonitor(true);status.Text="Aplanko stebėjimas išjungtas.";return;}
            if(!core.HasLocalEngine){MessageBox.Show(this,"Pirmiausia paruoškite vietinį ClamAV variklį ir atnaujinkite jo bazes. Defender šiam stebėjimui nenaudojamas.");return;}
            using(var d=new FolderBrowserDialog()){
                d.Description="Vietinis ClamAV: vienas aplankas be poaplankių, iki 32 MiB failui, iki 128 laukiančių įvykių.";
                if(d.ShowDialog(this)!=DialogResult.OK)return;
                if(!MonitorSafety.SafeWatchRoot(d.SelectedPath,core.Root,AppDomain.CurrentDomain.BaseDirectory)){MessageBox.Show(this,"SARGON duomenų ir programos aplankai nestebimi, kad nebūtų savęs tikrinimo ciklo. Pasirinkite kitą aplanką.");return;}
                if(!Confirm("Įjungti vietinį aplanko stebėjimą?","Vietinis ClamAV tikrins naujus / pakeistus failus pasirinktame aplanke. Programa turi veikti, gali būti sumažinta. Defender ir serveris nenaudojami. Tai nėra visos sistemos apsauga ar failo paleidimo blokavimas: 5 s ramybės, iki vieno failo kas 10 s, iki 32 MiB, be poaplankių. Stebėjimo atkūrimą po paleidimo galima įjungti automatikos nuostatose. Praleisti įvykiai ir neperskaityti failai nepatikrinti. Maitinimo ribą keiskite nuostatose."))return;
                bool auto=false;
                if(MonitorSafety.IsPersonalFolder(d.SelectedPath,Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)) && MonitorSafety.RecentControl(core.Config.EngineDetectionAt,DateTime.UtcNow.Ticks))
                    auto=MessageBox.Show(this,"Automatiškai karantinuoti tinkamus vietinio ClamAV radinius? Iki 8 MiB ir 10 kopijų. Tik asmeninio aplanko failams po pilnos variklio patikros; hash sutapimai ir bandymo žymenys savaime nešalinami. Prieš originalo šalinimą patikrinama šifruota atkuriama kopija. Galimi klaidingi radiniai. NE — perspėti ir leisti karantiną pasirinkti veiksmų centre.","Pasirenkamas automatinis karantinas",MessageBoxButtons.YesNo,MessageBoxIcon.Question,MessageBoxDefaultButton.Button2)==DialogResult.Yes;
                else MessageBox.Show(this,"Veiks perspėjimai ir rankinis karantinas. Automatiniam karantinui reikia asmeninio aplanko ir per 24 h praėjusio EICAR aptikimo bandymo (vietinio variklio meniu 5 punktas).");
                try{EngineSafety.ValidateDatabaseFiles(core.EngineDatabase);folderMonitor=new FolderMonitor(d.SelectedPath);folderMonitor.SeedExisting();monitoredDirectory=Path.GetFullPath(d.SelectedPath);monitorLocalEngine=true;monitorAutoQuarantine=auto;core.Config.SavedWatchDirectory=monitoredDirectory;core.Config.SavedWatchEnabled=true;core.Config.SavedAutoQuarantine=auto;core.SaveSettings();nextMonitorAt=0;watchTimer.Start();status.Text=folderMonitor.Summary+" • Vietinis ClamAV; automatinis karantinas: "+auto;}
                catch(Exception e) when(e is IOException||e is UnauthorizedAccessException){StopMonitor();MessageBox.Show(this,Data.Clean(e.Message));}
            }
        }

		private void MonitorTick()
		{
			if (folderMonitor == null || busy || !base.Enabled || base.OwnedForms.Length > 0 || BackgroundBlocked() || DateTime.UtcNow.Ticks<monitorScanHold)
			{
				return;
			}
			status.Text = folderMonitor.Summary;
			if (folderMonitor.Lost)
			{
				return;
			}
			string text = MonitorPowerHold();
			if (text.Length > 0)
			{
				status.Text = text + " • " + folderMonitor.Summary;
				return;
			}
			long ticks = DateTime.UtcNow.Ticks;
			if (ticks < nextMonitorAt)
			{
				return;
			}
			string path = folderMonitor.Take(ticks);
			if (path == null)
			{
				return;
			}
			nextMonitorAt = ticks + TimeSpan.FromSeconds(10.0).Ticks;
			activeMonitorPath=path;activeMonitorIssue=null;
            bool native = monitorLocalEngine;
            bool autoQuarantine=monitorAutoQuarantine; string watchRoot=monitoredDirectory;
			Run("Stebimo aplanko failo patikra", delegate
			{
                var attempt=core.TryReadFile(path,33554432L,true,"Foninio aplanko stebėjimo patikra");
                if(attempt.Issue!=null){activeMonitorIssue=attempt.Issue;selected=null;return attempt.Issue.Report()+"\r\nKitų eilėje esančių failų patikra tęsiama. Šio režimo riba nedidinama automatiškai.";}
                Witness w=attempt.Witness;
				string text2 = core.Describe(w, core.Database());
				selected = w;
				if(!native)return text2+"\r\nTik vietinės sumos; variklis neįjungtas.";
                text2+="\r\n\r\n"+core.ScanLocalEngine(w);
                if(autoQuarantine && MonitorSafety.CanAutoQuarantine(w,core.NotChecked,watchRoot,Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),core.Config.EngineDetectionAt,DateTime.UtcNow.Ticks)){
                    try{FolderMonitor.NoLinks(w.Path);text2+="\r\n\r\nAUTOMATINIO KARANTINO VEIKSMAS\r\n"+core.IsolateAndConfirm(w);if(core.VisualEvent!="REMOVAL_CONFIRMED")core.NotChecked++;}
                    catch(IOException e){core.NotChecked++;text2+="\r\nKARANTINAS NEPATVIRTINTAS: "+Data.Clean(e.Message)+". Peržiūrėkite veiksmų centre.";}
                    catch(UnauthorizedAccessException){core.NotChecked++;text2+="\r\nKarantinui nepakanka teisių. Originalas gali likti pasiekiamas.";}
                }else if(w.Kind=="ENGINE")text2+="\r\nRadinys paliktas peržiūrai veiksmų centre; automatinio karantino sąlygos netenkinamos.";
                return text2;
			}, native);
		}

		private FlowLayoutPanel NavSection(FlowLayoutPanel nav, string title, bool expanded)
		{
			FlowLayoutPanel flowLayoutPanel = new FlowLayoutPanel();
			flowLayoutPanel.FlowDirection = FlowDirection.TopDown;
			flowLayoutPanel.WrapContents = false;
			flowLayoutPanel.AutoSize = true;
			flowLayoutPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
			flowLayoutPanel.Margin = new Padding(0, 0, 0, 8);
			flowLayoutPanel.Padding = Padding.Empty;
			FlowLayoutPanel flowLayoutPanel2 = flowLayoutPanel;
			Button heading = new Button
			{
				Text = ((!expanded) ? "▶ " : "▼ ") + title,
				Width = 248,
				TextAlign = ContentAlignment.MiddleLeft
			};
			Style(heading);
			FlowLayoutPanel body = new FlowLayoutPanel
			{
				FlowDirection = FlowDirection.TopDown,
				WrapContents = false,
				AutoSize = true,
				AutoSizeMode = AutoSizeMode.GrowAndShrink,
				Margin = Padding.Empty,
				Padding = Padding.Empty,
				Visible = expanded
			};
			flowLayoutPanel2.Controls.Add(heading);
			flowLayoutPanel2.Controls.Add(body);
			heading.Click += delegate
			{
				body.Visible = !body.Visible;
				heading.Text = ((!body.Visible) ? "▶ " : "▼ ") + title;
			};
			actions.Add(heading);
			nav.Controls.Add(flowLayoutPanel2);
			return body;
		}

		private void Style(Button b)
		{
			b.Height = (int)Math.Ceiling(42f * FontScale);
			b.AutoEllipsis = true;
			b.FlatStyle = FlatStyle.Flat;
			b.FlatAppearance.BorderColor = Color.FromArgb(45, 65, 88);
			b.BackColor = Card;
			b.ForeColor = Ink;
			b.Font = Font;
			b.Cursor = Cursors.Hand;
		}

		private void Add(FlowLayoutPanel panel, string name, Action action)
		{
			Button button = new Button();
			button.Text = name;
			button.Width = 248;
			button.TextAlign = ContentAlignment.MiddleLeft;
			button.Padding = new Padding(8, 0, 0, 0);
			button.Margin = new Padding(0, 0, 0, 6);
			Button button2 = button;
			Style(button2);
			button2.Click += delegate
			{
				if (!busy)
				{
					try
					{
						action();
					}
					catch (Exception)
					{
						MessageBox.Show(this, "Veiksmo pradėti nepavyko. Patikrinkite pasirinktą failą ir parengtį.", "Nepatvirtinta");
					}
				}
			};
			panel.Controls.Add(button2);
			actions.Add(button2);
		}

		private bool Confirm(string title, string text)
		{
			return MessageBox.Show(this, text, title, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;
		}

		private string Pick()
		{
			OpenFileDialog openFileDialog = new OpenFileDialog();
			openFileDialog.CheckFileExists = true;
			openFileDialog.Multiselect = false;
			openFileDialog.Title = "Pasirinkite vieną failą";
			openFileDialog.Filter = "Visi failai|*.*";
			using (OpenFileDialog openFileDialog2 = openFileDialog)
			{
				return (openFileDialog2.ShowDialog(this) != DialogResult.OK) ? null : openFileDialog2.FileName;
			}
		}

		private Witness Target()
		{
			if (selected == null)
			{
				MessageBox.Show(this, "Pirmiausia atlikite vieno failo vietinę patikrą. Istoriniai radiniai savaime nesuteikia teisės keisti seną failą.");
				return null;
			}
			return selected;
		}

		[DebuggerStepThrough]
		[AsyncStateMachine(typeof(_003CRun_003Ec__async0))]
		private void Run(string name, Func<string> work, bool external = false)
		{
			_003CRun_003Ec__async0 stateMachine = default(_003CRun_003Ec__async0);
			stateMachine.external = external;
			stateMachine.name = name;
			stateMachine.work = work;
			stateMachine._0024this = this;
			stateMachine._0024builder = AsyncVoidMethodBuilder.Create();
			stateMachine._0024builder.Start(ref stateMachine);
		}

		private void ScanFile()
		{
			string path = Pick();
			if (path == null)
			{
				return;
			}
			bool engine = core.HasLocalEngine;
			Run((!engine) ? "Failo patikra pagal vietinę bazę" : "Vietinė sumų ir ClamAV patikra", delegate
			{
				Witness witness;
				string text = core.CheckLocalFile(path, out witness);
				selected = witness;
				if (!engine)
				{
					return text + "\r\nVietinis ClamAV neįdiegtas — atlikta tik sumų patikra.";
				}
				try
				{
					return text + "\r\n\r\n" + core.ScanLocalEngine(witness);
				}
				catch (OperationCanceledException)
				{
					throw;
				}
				catch (CommandFailure commandFailure)
				{
					if (commandFailure.Problem == CommandProblem.Cancelled)
					{
						throw;
					}
					core.NotChecked++;
					return text + "\r\nVIETINIS VARIKLIS NEPATVIRTINTAS: " + Data.Diagnostic(commandFailure) + ". Atnaujinkite ClamAV bazes ir tikrinkite iš naujo.";
				}
				catch (IOException error)
				{
					core.NotChecked++;
					return text + "\r\n"+Data.Clean(error.Message)+"\r\nVIETINIS VARIKLIS NEPATVIRTINTAS. Patikrinkite jo parengtį ir bazes. Vietinis rezultatas nėra viso failo saugumo garantija.";
				}
			}, engine);
		}

		private void LocalEngineDialog()
		{
			Form dialog = new Form
			{
				Text = "Vietinis ClamAV variklis",
				Width = 760,
				Height = 650,
				StartPosition = FormStartPosition.CenterParent
			};
			try
			{
				dialog.Controls.Add(new Label
				{
					Left = 18,
					Top = 18,
					Width = 700,
					Height = 200,
					Text = core.LocalEngineStatus() + (core.SharedReadOnlyEngine?"\r\nBendras variklis jau įdiegtas; paruošimo mygtukas išjungtas. Bazes galima atnaujinti administratoriaus lange.":"") + "\r\nClamAV tikrina turinį ir archyvus pagal savo bazes. Windows apsauga realiu laiku šiuo leidimu neįdiegiama."
				});
				FlowLayoutPanel panel = new FlowLayoutPanel
				{
					Left = 18,
					Top = 225,
					Width = 700,
					Height = 340,
					FlowDirection = FlowDirection.TopDown,
					WrapContents = false,
					AutoScroll = true
				};
				dialog.Controls.Add(panel);
				Action<string, Action> action = delegate(string title, Action action2)
				{
					Button button = new Button
					{
						Text = title,
						Width = 660,
						Height = 55
					};
					
					if(core.SharedReadOnlyEngine&&title.StartsWith("1."))button.Enabled=false;
                    panel.Controls.Add(button);
					button.Click += delegate
					{
						dialog.Close();
						action2();
					};
				};
				action("1. Paruošti oficialų ClamAV 1.4.6 LTS (Windows x64)", delegate
				{
					if (Confirm("Atsisiųsti vietinį variklį?", "Bus atsisiųstas ~183 MiB oficialus ClamAV archyvas. Tikrinama programoje įrašyta SHA-256 suma, parengimo metu patvirtinta pagal leidėjo PGP parašą. Reikia interneto ir apie 1 GiB vietos. Windows x64. Po to atskirai atnaujinkite bazes. Esama antivirusinė neišjungiama."))
					{
						Run("Vietinio variklio paruošimas", () => core.InstallLocalEngine(), external: true);
					}
				});
				action("2. Atsisiųsti / atnaujinti ClamAV virusų bazes", delegate
				{
					if(core.SharedReadOnlyEngine){LaunchDatabaseUpdate();return;}
					if (Confirm("Atnaujinti vietinio variklio bazes?", "FreshClam jungsis prie oficialių ClamAV bazių serverių ir tikrins CVD parašus. Pirmas atsisiuntimas gali būti keli šimtai MB; palaukite iki 15 min. Jūsų failai nesiunčiami. Tai ne automatinis foninis atnaujinimas."))
					{
						Run("ClamAV bazių atnaujinimas", () => core.UpdateLocalEngine(), external: true);
					}
				});
				action("3. Tikrinti pasirinktą failą vietoje", ScanFile);

				action("4. Tikrinti pasiekiamus vietinius diskus", delegate
				{
					if (Confirm("Pradėti vietinių diskų patikrą?", "ClamAV rekursiškai tikrins pasiekiamus fiksuotus diskus šios paskyros teisėmis, iki 30 min. kiekvienam. Gali užtrukti ir apkrauti diską. Turinys nebus siunčiamas ir failai nebus automatiškai šalinami. Neprieinami failai, procesų atmintis ir įkrova neįtraukiami. Tai nėra apsauga realiu laiku."))
					{
						Run("Vietinių diskų ClamAV patikra", () => core.ScanFixedDrives(), external: true);
					}
				});
                action("5. Patikrinti variklio aptikimą (EICAR bandymas)", delegate {
                    if(Confirm("Atlikti antivirusinio aptikimo bandymą?", "Bus trumpam sukurti neutralus failas ir nekenksmingas EICAR bandymo žymuo privačiame programos aplanke. Jie nebus vykdomi ar siunčiami. Kita antivirusinė gali parodyti bandymo perspėjimą ir pašalinti žymenį — tada SARGON rezultatas bus nepatvirtintas. Apsaugos neišjunkite. Po bandymo laikini failai šalinami."))
                        Run("Variklio neutralus ir EICAR aptikimo bandymas",()=>core.TestLocalEngineDetection(), external:true);
                });
				ThemeDialog(dialog);
				dialog.ShowDialog(this);
			}
			finally
			{
				if (dialog != null)
				{
					((IDisposable)dialog).Dispose();
				}
			}
		}

		private void Isolation()
		{
			Witness target = Target();
			if (target == null)
			{
				return;
			}
			Form d = new Form
			{
				Text = "Šifruota kopija / izoliavimas",
				Width = 620,
				Height = 370,
				StartPosition = FormStartPosition.CenterParent
			};
			Label label = new Label();
			label.Left = 18;
			label.Top = 18;
			label.Width = 565;
			label.Height = 210;
			label.Text = "Iki 8 MiB failas bus šifruojamas Windows DPAPI šiai paskyrai / kompiuteriui. Sukūrus kopiją jos suma bus patikrinta. Vien kopija originalo nepašalina.\r\nOriginalas šalinamas tik pasirinkus atskirą žemiau esantį veiksmą, per užrakintą ir pakartotinai patikrintą failo rankeną. Kitų kopijų ar procesų tai nepašalina.";
			Label label2 = label;
			d.Controls.Add(label2);
			Button button = new Button();
			button.Left = 18;
			button.Top = 250;
			button.Width = 240;
			button.Height = 40;
			button.Text = "Tik šifruota kopija";
			Button button2 = button;
			button = new Button();
			button.Left = 270;
			button.Top = 250;
			button.Width = 300;
			button.Height = 40;
			button.Text = "Kopija IR originalo pašalinimas";
			Button button3 = button;
			d.Controls.Add(button2);
			d.Controls.Add(button3);
			button3.Enabled = !target.Test;
			label2.Text = "Radino tipas: " + (target.Test ? "EICAR bandymo tekstas (ne virusas). Originalo šalinimas čia išjungtas." : ((target.Kind == "HASH") ? "sumos atitiktis; patikrinkite šaltinį prieš šalindami." : ((!(target.Kind == "ENGINE")) ? "grėsmė šia patikra nepatvirtinta." : "antivirusinio variklio žyma; klaidingas perspėjimas galimas."))) + "\r\n" + label2.Text;
			button2.Click += delegate
			{
				d.Close();
				Run("Šifruotos kopijos kūrimas", () => core.Isolate(target, deleteOriginal: false));
			};
			button3.Click += delegate
			{
				if (MessageBox.Show(d, "PAŠALINTI pasirinktą originalą tik patvirtinus šifruotą kopiją?", target.Path, MessageBoxButtons.YesNo) == DialogResult.Yes)
				{
					d.Close();
					Run("Failo izoliavimas", () => core.IsolateAndConfirm(target));
				}
			};
			ThemeDialog(d);
			d.ShowDialog(this);
			d.Dispose();
		}

		private void Vault()
		{
			List<VaultEntry> rows = core.Vault();
			Form d = new Form
			{
				Text = "Šifruotos kopijos — ne viso PC karantinas",
				Width = 700,
				Height = 450,
				StartPosition = FormStartPosition.CenterParent
			};
			try
			{
				ListBox list = new ListBox
				{
					Left = 16,
					Top = 16,
					Width = 650,
					Height = 290
				};
				foreach (VaultEntry item in rows)
				{
					list.Items.Add(item.Name + ((!item.Deleted) ? " • originalas galėjo likti" : " • šalinimas buvo patvirtintas"));
				}
				d.Controls.Add(list);
				Button button = new Button();
				button.Left = 16;
				button.Top = 325;
				button.Width = 230;
				button.Text = "Atkurti pasirinktą kopiją…";
				button.Height = 40;
				Button button2 = button;
				d.Controls.Add(button2);
				button = new Button();
				button.Left = 270;
				button.Top = 325;
				button.Width = 260;
				button.Text = "Pašalinti šifruotą kopiją…";
				button.Height = 40;
				Button button3 = button;
				d.Controls.Add(button3);
				button3.Click += delegate
				{
					int selectedIndex = list.SelectedIndex;
					if (selectedIndex >= 0)
					{
						VaultEntry entry = rows[selectedIndex];
						if (MessageBox.Show(d, "Negrįžtamai pašalinti šifruotą kopiją? Jei originalas pašalintas, failo nebeatkursite.", entry.Name, MessageBoxButtons.YesNo) == DialogResult.Yes)
						{
							d.Close();
							Run("Šifruotos kopijos pašalinimas", delegate
							{
								core.DeleteCopy(entry);
								return "Pasirinkta šifruota kopija pašalinta jūsų prašymu. Originalo būklė nekeista.";
							});
						}
					}
				};
				button2.Click += delegate
				{
					int selectedIndex = list.SelectedIndex;
					if (selectedIndex >= 0)
					{
						VaultEntry entry = rows[selectedIndex];
						if (MessageBox.Show(d, "Atkurtas failas vėl bus pasiekiamas. Jei jis pavojingas, neatverkite. Esamas failas nebus perrašomas.", "Atkurti?", MessageBoxButtons.YesNo) == DialogResult.Yes)
						{
							using (SaveFileDialog saveFileDialog = new SaveFileDialog
							{
								FileName = Path.GetFileName(entry.Name),
								OverwritePrompt = true
							})
							{
								if (saveFileDialog.ShowDialog(d) == DialogResult.OK)
								{
									string path = saveFileDialog.FileName;
									d.Close();
									Run("Kopijos atkūrimas", delegate
									{
										core.Restore(entry, path);
										return "Kopija patikrinta ir atkurta. Pavojingas failas atkūrus nėra izoliuotas; neatverkite jo neįsitikinę saugumu.";
									});
								}
							}
						}
					}
				};
				ThemeDialog(d);
				d.ShowDialog(this);
			}
			finally
			{
				if (d != null)
				{
					((IDisposable)d).Dispose();
				}
			}
		}

		private void Network()
		{
			Form d = new Form
			{
				Text = "Pasirenkamas HTTPS ryšys",
				Width = 690,
				Height = 520,
				StartPosition = FormStartPosition.CenterParent
			};
			try
			{
				string[] array = new string[3] { "Jūsų ClamAV HTTPS serverio adresas", "Serverio raktas (tuščia — palikti)", "MalwareBazaar Auth-Key (tuščia — palikti)" };
				TextBox[] fields = new TextBox[3];
				for (int i = 0; i < 3; i++)
				{
					d.Controls.Add(new Label
					{
						Left = 18,
						Top = 18 + i * 76,
						Width = 625,
						Text = array[i]
					});
					fields[i] = new TextBox
					{
						Left = 18,
						Top = 43 + i * 76,
						Width = 625,
						UseSystemPasswordChar = (i > 0)
					};
					d.Controls.Add(fields[i]);
				}
				fields[0].Text = core.Config.Endpoint;
				d.Controls.Add(new Label
				{
					Left = 18,
					Top = 255,
					Width = 625,
					Height = 110,
					Text = "MalwareBazaar Auth-Key įrašykite trečiame lauke. ClamAV bazėms jo nereikia. Raktai saugomi Windows DPAPI šifruoti šiai paskyrai. Automatinio siuntimo nėra. Pasirinkto aplanko stebėjimas įjungiamas atskirai. Raktų neklijuokite į ataskaitas. Tiekėjo prieigos teisėmis pasirūpinate patys."
				});
				Button button = new Button();
				button.Left = 18;
				button.Top = 390;
				button.Width = 240;
				button.Height = 40;
				button.Text = "Išsaugoti nustatymus";
				Button button2 = button;
				button = new Button();
				button.Left = 280;
				button.Top = 390;
				button.Width = 260;
				button.Height = 40;
				button.Text = "Pašalinti išsaugotus raktus";
				Button button3 = button;
				d.Controls.Add(button2);
				d.Controls.Add(button3);
				button2.Click += delegate
				{
					string url = fields[0].Text.Trim();
					string api = fields[2].Text;
					string server = fields[1].Text;
					try
					{
						if (url.Length > 0)
						{
							Data.Endpoint(url);
						}
						if (api.Length > 0)
						{
							Data.Token(api);
						}
						if (server.Length > 0)
						{
							Data.Token(server);
						}
					}
					catch (Exception ex)
					{
						MessageBox.Show(d, ex.Message);
						return;
					}
					d.Close();
					Run("Ryšio nustatymai", delegate
					{
						core.SaveKeys(api, server);
						if (core.Config.Endpoint != url)
						{
							core.Config.HealthOk = false;
						}
						core.Config.Endpoint = url;
						core.SaveSettings();
						return "Nustatymai išsaugoti. Vien išsaugojus jokie failai, sumos ar raktai į tinklą nesiųsti. Raktų galiojimą patvirtina tik sėkminga paslaugos užklausa.";
					});
				};
				button3.Click += delegate
				{
					if (MessageBox.Show(d, "Pašalinti abu išsaugotus prieigos raktus?", "Patvirtinimas", MessageBoxButtons.YesNo) == DialogResult.Yes)
					{
						d.Close();
						Run("Raktų šalinimas", delegate
						{
							core.ClearKeys();
							return "Prieigos raktų įrašas pašalintas. Anksčiau eksportuotos ataskaitos ir šifruotos failų kopijos lieka.";
						});
					}
				};
				ThemeDialog(d);
				d.ShowDialog(this);
			}
			finally
			{
				if (d != null)
				{
					((IDisposable)d).Dispose();
				}
			}
		}

		private void ResponseCenter()
		{
			Form d = new Form
			{
				Text = "Grėsmių ir veiksmų centras",
				Width = 820,
				Height = 670,
				StartPosition = FormStartPosition.CenterParent
			};
			try
			{
				Label label = new Label();
				label.Left = 18;
				label.Top = 14;
				label.Width = 760;
				label.Height = 36;
				label.Text = "Spręstini radiniai";
				label.Font = new Font("Segoe UI", 18f, FontStyle.Bold);
				Label value = label;
				d.Controls.Add(value);
				d.Controls.Add(new Label
				{
					Left = 18,
					Top = 55,
					Width = 760,
					Height = 65,
					Text = "Peržiūrėkite pagrindą → patikrinkite papildomai → pasirinkite veiksmą → patikrinkite rezultatą. Peržiūrėta nereiškia neutralizuota. EICAR yra bandymas, ne virusas."
				});
				FlowLayoutPanel flowLayoutPanel = new FlowLayoutPanel();
				flowLayoutPanel.Left = 18;
				flowLayoutPanel.Top = 125;
				flowLayoutPanel.Width = 760;
				flowLayoutPanel.Height = 355;
				flowLayoutPanel.AutoScroll = true;
				flowLayoutPanel.FlowDirection = FlowDirection.TopDown;
				flowLayoutPanel.WrapContents = false;
				FlowLayoutPanel flowLayoutPanel2 = flowLayoutPanel;
				d.Controls.Add(flowLayoutPanel2);
				List<Witness> list = core.SessionFindings.ToList();
				if (list.Count == 0)
				{
					flowLayoutPanel2.Controls.Add(new Label
					{
						Text = "Dabartinės sesijos failų radinių nėra. Tai ne viso PC saugumo patvirtinimas. Istorinius įrašus peržiūrėkite atmintinėje.",
						Width = 710,
						Height = 80
					});
				}
				Button button;
				foreach (Witness item in list)
				{
					Witness w = item;
					Panel panel = new Panel();
					panel.Width = 715;
					panel.Height = 232;
					panel.BackColor = Card;
					panel.Padding = new Padding(12);
					panel.Margin = new Padding(0, 0, 0, 10);
					panel.Tag = w.Kind;
					Panel panel2 = panel;
					label = new Label();
					label.Left = 12;
					label.Top = 10;
					label.Width = 690;
					label.Height = 45;
					label.Text = ((!w.Test) ? ((!(w.Kind == "ENGINE")) ? "SUMOS ATITIKTIS" : "VARIKLIO ŽYMA") : "BANDYMAS") + " • " + Data.Clean(Path.GetFileName(w.Path));
					label.ForeColor = ((!w.Test) ? Color.FromArgb(255, 179, 107) : Accent);
					label.Tag = w.Kind;
					Label value2 = label;
					panel2.Controls.Add(value2);
					panel2.Controls.Add(new Label
					{
						Left = 12,
						Top = 60,
						Width = 690,
						Height = 100,
						Text = "Būsena: reikia peržiūros; pašalinimas nepatvirtintas.\r\nPagrindas: " + ((!w.Test) ? ((!(w.Kind == "ENGINE")) ? "tiksli SHA-256 atitiktis sąrašui" : "jūsų ClamAV serverio žyma") : "vietinis EICAR tekstas") + "\r\nSHA-256:\r\n" + w.Hash
					});
					button = new Button();
					button.Left = 12;
					button.Top = 166;
					button.Width = 680;
					button.Height = 38;
					button.Text = "Peržiūrėti ir pasirinkti veiksmą…";
					Button button2 = button;
					panel2.Controls.Add(button2);
					button2.Click += delegate
					{
						d.Close();
						ResponseActions(w);
					};
					flowLayoutPanel2.Controls.Add(panel2);
				}
				button = new Button();
				button.Left = 18;
				button.Top = 495;
				button.Width = 370;
				button.Height = 42;
				button.Text = "Veiksmų rezultatai / žurnalas";
				Button button3 = button;
				d.Controls.Add(button3);
				button3.Click += delegate
				{
					d.Close();
					ShowJournal();
				};
				button = new Button();
				button.Left = 405;
				button.Top = 495;
				button.Width = 370;
				button.Height = 42;
				button.Text = "Šifruotos kopijos ir atkūrimas";
				Button button4 = button;
				d.Controls.Add(button4);
				button4.Click += delegate
				{
					d.Close();
					Vault();
				};
				button = new Button();
				button.Left = 18;
				button.Top = 550;
				button.Width = 370;
				button.Height = 42;
				button.Text = "Windows apsaugos istorija";
				Button button5 = button;
				d.Controls.Add(button5);
				button5.Click += delegate
				{
					d.Close();
					try
					{
						Process.Start(new ProcessStartInfo("windowsdefender://history")
						{
							UseShellExecute = true
						});
					}
					catch
					{
						MessageBox.Show(this, "Atverkite Windows saugumas → Apsaugos istorija.");
					}
				};
				button = new Button();
				button.Left = 405;
				button.Top = 550;
				button.Width = 370;
				button.Height = 42;
				button.Text = "Grįžti į ataskaitą";
				Button button6 = button;
				d.Controls.Add(button6);
				button6.Click += delegate
				{
					d.Close();
				};
				ThemeDialog(d);
				d.ShowDialog(this);
			}
			finally
			{
				if (d != null)
				{
					((IDisposable)d).Dispose();
				}
			}
		}

		private void ResponseActions(Witness target)
		{
			Form d = new Form
			{
				Text = "Pasirinkto radinio veiksmai",
				Width = 640,
				Height = 300,
				StartPosition = FormStartPosition.CenterParent
			};
			try
			{
				Label label = new Label();
				label.Left = 18;
				label.Top = 18;
				label.Width = 590;
				label.Height = 70;
				label.Text = Path.GetFileName(target.Path) + "\r\nKlaidingas perspėjimas galimas. Tinklo veiksmams reikia jūsų paslaugos rakto.";
				Label value = label;
				d.Controls.Add(value);
				Button button = new Button();
				button.Left = 18;
				button.Top = 235;
				button.Width = 580;
				button.Height = 40;
				button.Text = "Paaiškinti radinį ir veiksmo pasekmes";
				Button button2 = button;
				d.Height = 370;
				d.Controls.Add(button2);
				button2.Click += delegate
				{
					MessageBox.Show(d, Data.FindingAdvice((!target.Test) ? target.Kind : "TEST"), "Radinio pagrindas ir veiksmai");
				};
				Button[] array = new Button[3]
				{
					new Button
					{
						Left = 18,
						Top = 95,
						Width = 580,
						Height = 40,
						Text = "Papildomai tikrinti SHA-256 reputaciją…"
					},
					new Button
					{
						Left = 18,
						Top = 143,
						Width = 580,
						Height = 40,
						Text = "Siųsti failą savo ClamAV serveriui…"
					},
					new Button
					{
						Left = 18,
						Top = 191,
						Width = 580,
						Height = 40,
						Text = "Šifruoti / izoliuoti pasirinktą failą…"
					}
				};
				Button[] array2 = array;
				foreach (Button value2 in array2)
				{
					d.Controls.Add(value2);
				}
				array[0].Click += delegate
				{
					if (Confirm("Siųsti SHA-256?", "MalwareBazaar gaus tik sumą, jūsų Auth-Key ir IP. Failo turinys nesiunčiamas."))
					{
						d.Close();
						Run("Papildoma reputacijos patikra", delegate
						{
							string result = core.Reputation(target);
							selected = target;
							return result;
						});
					}
				};
				array[1].Click += delegate
				{
					if (Confirm("Siųsti failo TURINĮ?", "Iki 32 MiB failas bus siunčiamas jūsų serveriui: " + core.Config.Endpoint + ". Serverio administratorius galės perskaityti turinį."))
					{
						d.Close();
						Run("Papildoma serverio patikra", delegate
						{
							string result = core.Server(target);
							selected = target;
							return result;
						});
					}
				};
				array[2].Click += delegate
				{
					selected = target;
					d.Close();
					Isolation();
				};
				ThemeDialog(d);
				d.ShowDialog(this);
			}
			finally
			{
				if (d != null)
				{
					((IDisposable)d).Dispose();
				}
			}
		}

		private void ThemeDialog(Form d)
		{
			d.BackColor = Bg;
			d.ForeColor = Ink;
			d.Font = Font;
			Action<Control> theme = null;
			theme = delegate(Control c)
			{
				c.ForeColor = ((!(c is Label) || !(c.Tag is string)) ? Ink : (((string)c.Tag == "TEST") ? Accent : ((!core.Config.LightTheme) ? Color.FromArgb(255, 205, 134) : Color.FromArgb(136, 83, 0))));
				if (c is Panel && c.Tag is string)
				{
					c.BackColor = Card;
				}
				if (c is Button)
				{
					Style((Button)c);
				}
				else if (c is ListBox || c is TextBox)
				{
					c.BackColor = Card;
				}
				foreach (Control control in c.Controls)
				{
					theme(control);
				}
			};
			foreach (Control control2 in d.Controls)
			{
				theme(control2);
			}
		}

		private static long Cpu()
		{
			try
			{
				return (long)Process.GetCurrentProcess().TotalProcessorTime.TotalMilliseconds;
			}
			catch
			{
				return -1L;
			}
		}

		private void Wizard()
		{
			Form d = new Form
			{
				Text = "Paruošimo vedlys",
				Width = 720,
				Height = 550,
				StartPosition = FormStartPosition.CenterParent
			};
			try
			{
				Label label = new Label();
				label.Left = 18;
				label.Top = 18;
				label.Width = 660;
				label.Height = 190;
				label.Text = core.Dashboard();
				Label value = label;
				d.Controls.Add(value);
				FlowLayoutPanel panel = new FlowLayoutPanel
				{
					Left = 18,
					Top = 215,
					Width = 660,
					Height = 280,
					FlowDirection = FlowDirection.TopDown,
					WrapContents = false,
					AutoScroll = true
				};
				d.Controls.Add(panel);
				Action<string, Action> action = delegate(string title, Action action2)
				{
					Button button = new Button
					{
						Text = title,
						Width = 620,
						Height = 42
					};
					
					panel.Controls.Add(button);
					button.Click += delegate
					{
						d.Close();
						action2();
					};
				};
				action("1. Paleisti vietinę savitikrą", delegate
				{
					Run("Savitikra", () => core.SelfCheck());
				});
				action("2. Įrašyti savo raktus ir HTTPS adresą", Network);
                action("3. Papildomos MalwareBazaar sumos ir raktas…", FeedDialog);
				action("4. Patikrinti serverio ryšį be failo siuntimo", delegate
				{
					if (Confirm("Patikrinti ryšį?", "Jūsų HTTPS serveris gaus raktą ir IP; failas ir suma nesiunčiami."))
					{
						Run("Serverio ryšys", () => core.CheckServer());
					}
				});
				action("5. Pasirinkti pirmą failą vietinei patikrai", delegate
				{
					string p = Pick();
					if (p != null)
					{
						Run("Vietinė failo patikra", delegate
						{
							Witness w = core.Scan(p, 268435456L);
							string result = core.Describe(w, core.Database());
							selected = w;
							return result;
						});
					}
				});
				ThemeDialog(d);
				d.ShowDialog(this);
			}
			finally
			{
				if (d != null)
				{
					((IDisposable)d).Dispose();
				}
			}
		}

		private void Export()
		{
			SaveFileDialog saveFileDialog = new SaveFileDialog();
			saveFileDialog.FileName = "Sargo_PC_ataskaita.txt";
			saveFileDialog.Filter = "Teksto ataskaita|*.txt";
			saveFileDialog.OverwritePrompt = true;
			using (SaveFileDialog saveFileDialog2 = saveFileDialog)
			{
				if (saveFileDialog2.ShowDialog(this) == DialogResult.OK)
				{
					File.WriteAllText(saveFileDialog2.FileName, lastReport, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
					MessageBox.Show(this, "Ataskaita išsaugota. Joje gali būti pasirinktų failų kelių, sumų ir būsenos duomenų. Automatinio siuntimo nėra.");
				}
			}
		}
	}
}
