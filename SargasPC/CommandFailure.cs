using System.IO;

namespace SargasPC
{
	public sealed class CommandFailure : IOException
	{
		public readonly CommandProblem Problem;

		public readonly int Code;

		public CommandFailure(CommandProblem problem, int code = 0)
			: base(string.Concat("Sistemos komanda: ", problem, "; kodas ", code))
		{
			Problem = problem;
			Code = code;
		}
	}
}
