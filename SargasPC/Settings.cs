namespace SargasPC
{
	public sealed class Settings
	{
		public string Endpoint = "";

		public long SignedDbVersion;

		public long LookupAt;

		public long FeedAt;

		public long ImportAt;

		public long SelfAt;

		public long HealthAt;

		public bool SelfRunning;

		public bool SelfPassed;

		public bool HealthOk;

		public int SelfVersion;

		public long LocalEngineCheckAt;
        public long EngineDetectionAt;
        public bool AutoDbUpdate,AutoDetectionControl,RestoreWatch,SavedWatchEnabled,SavedAutoQuarantine;
        public string SavedWatchDirectory="";
        public long AutoDbAttemptAt,AutoDbSuccessAt;
        public int AutoDbFailures;

		public string HealthEndpoint = "";

		public bool Motion = true;

		public bool MonitorOnAc = true;

		public bool LightTheme;

		public int FontPercent = 100;
	}
}
