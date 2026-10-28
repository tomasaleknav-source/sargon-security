namespace SargasPC
{
	public sealed class CommandOutput
	{
		public int ExitCode;

		public string Output;

		public string Error;

		public bool OutputTruncated;

		public bool ErrorTruncated;
	}
}
