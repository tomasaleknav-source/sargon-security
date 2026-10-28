using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Windows.Forms;

namespace SargasPC
{
	public static class Program
	{
		[STAThread]
		public static void Main(string[] args)
		{
			using (Mutex mutex = new Mutex(initiallyOwned: false, "Local\\SargasPC_v1"))
			{
				bool flag = false;
				try
				{
					flag = mutex.WaitOne(0, exitContext: true);
					if (!flag)
					{
						if (Array.IndexOf(args,"--startup")<0) MessageBox.Show("SARGON Security jau veikia. Atverkite jos skydo piktogramą sistemos dėkle.");
						return;
					}
					ServicePointManagerSetup();
					Application.EnableVisualStyles();
					Application.SetCompatibleTextRenderingDefault(defaultValue: false);
					string text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TelefonoSargasPC");
					Core core = new Core(text);
                    SharedEngine.UseIfAvailable(core);
					core.BundledHashPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "initial.hdb");
					if (args.Length == 1 && args[0] == "--benchmark")
					{
						MessageBox.Show("Spartos bandymas baigtas. Ataskaita: " + BenchmarkRunner.Run(text) + ". Tai nėra virusų aptikimo ar baterijos bandymas.");
					}
					else
					{
						var window = new Window(core);
                        window.ConfigureBackgroundLaunch(Array.IndexOf(args,"--startup")>=0,Array.IndexOf(args,"--configure-background")>=0);
                        Application.Run(window);
					}
				}
				catch (Exception)
				{
					MessageBox.Show("Programos paleisti nepavyko. Patikrinkite Windows .NET Framework 4.8, prieigą prie vietinių duomenų ir ar nustatymų failai nesugadinti. Esami duomenys neperrašyti.", "SARGON Security");
				}
				finally
				{
					if (flag)
					{
						mutex.ReleaseMutex();
					}
				}
			}
		}

		private static void ServicePointManagerSetup()
		{
			ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
		}
	}
}
