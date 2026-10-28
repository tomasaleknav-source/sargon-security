using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SargasPC
{
	public static class BoundedCommand
	{
		private sealed class Capture
		{
			public string Text;

			public bool Truncated;
		}

		[StructLayout(LayoutKind.Auto)]
		[CompilerGenerated]
		private struct _003CRead_003Ec__async0 : IAsyncStateMachine
		{
			internal StringBuilder _003Ctext_003E__0;

			internal char[] _003Cbuffer_003E__0;

			internal long _003Ctotal_003E__0;

			internal bool _003Ctruncated_003E__0;

			internal TextReader reader;

			internal int _003Cn_003E__1;

			internal bool tail;

			internal AsyncTaskMethodBuilder<Capture> _0024builder;

			internal int _0024PC;

			private ConfiguredTaskAwaitable<int>.ConfiguredTaskAwaiter _0024awaiter0;

			public void MoveNext()
			{
				uint num = (uint)_0024PC;
				_0024PC = -1;
				Capture result;
				try
				{
					switch (num)
					{
					default:
						return;
					case 0u:
						_003Ctext_003E__0 = new StringBuilder();
						_003Cbuffer_003E__0 = new char[1024];
						_003Ctotal_003E__0 = 0L;
						_003Ctruncated_003E__0 = false;
						goto IL_004b;
					case 1u:
						{
							_003Cn_003E__1 = _0024awaiter0.GetResult();
							if (_003Cn_003E__1 == 0)
							{
								result = new Capture
								{
									Text = _003Ctext_003E__0.ToString(),
									Truncated = _003Ctruncated_003E__0
								};
								break;
							}
							_003Ctotal_003E__0 += _003Cn_003E__1;
							if (_003Ctotal_003E__0 > ((!tail) ? 16384 : 67108864))
							{
								throw new CommandFailure(CommandProblem.OutputLimit);
							}
							_003Ctext_003E__0.Append(_003Cbuffer_003E__0, 0, _003Cn_003E__1);
							if (_003Ctext_003E__0.Length > 16384)
							{
								if (!tail)
								{
									throw new CommandFailure(CommandProblem.OutputLimit);
								}
								_003Ctext_003E__0.Remove(0, _003Ctext_003E__0.Length - 16384);
								_003Ctruncated_003E__0 = true;
							}
							goto IL_004b;
						}
						IL_004b:
						_0024awaiter0 = reader.ReadAsync(_003Cbuffer_003E__0, 0, _003Cbuffer_003E__0.Length).ConfigureAwait(continueOnCapturedContext: false).GetAwaiter();
						if (!_0024awaiter0.IsCompleted)
						{
							_0024PC = 1;
							_0024builder.AwaitUnsafeOnCompleted(ref _0024awaiter0, ref this);
							return;
						}
						goto case 1u;
					}
				}
				catch (Exception exception)
				{
					_0024PC = -1;
					_0024builder.SetException(exception);
					return;
				}
				_0024PC = -1;
				_0024builder.SetResult(result);
			}

			[DebuggerHidden]
			public void SetStateMachine(IAsyncStateMachine stateMachine)
			{
				_0024builder.SetStateMachine(stateMachine);
			}
		}

		private const int Limit = 16384;

		[DebuggerStepThrough]
		[AsyncStateMachine(typeof(_003CRead_003Ec__async0))]
		private static Task<Capture> Read(TextReader reader, bool tail)
		{
			_003CRead_003Ec__async0 stateMachine = default(_003CRead_003Ec__async0);
			stateMachine.reader = reader;
			stateMachine.tail = tail;
			stateMachine._0024builder = AsyncTaskMethodBuilder<Capture>.Create();
			ref AsyncTaskMethodBuilder<Capture> reference = ref stateMachine._0024builder;
			reference.Start(ref stateMachine);
			return reference.Task;
		}

		private static void Observe(Task<Capture> task)
		{
			task.ContinueWith(delegate(Task<Capture> t)
			{
				AggregateException exception = t.Exception;
			}, TaskContinuationOptions.OnlyOnFaulted);
		}

		private static void CheckRead(Task<Capture> task)
		{
			if (task.IsFaulted)
			{
				ReadOnlyCollection<Exception> innerExceptions = task.Exception.Flatten().InnerExceptions;
				foreach (Exception item in innerExceptions)
				{
					if (item is CommandFailure)
					{
						throw (CommandFailure)item;
					}
				}
				throw new CommandFailure(CommandProblem.ReadFailure);
			}
			if (task.IsCanceled)
			{
				throw new CommandFailure(CommandProblem.ReadFailure);
			}
		}

		public static string Run(ProcessStartInfo info, int timeout, Func<bool> cancelled)
		{
			return RunResult(info, timeout, cancelled, acceptFound: false).Output;
		}

		public static CommandOutput RunResult(ProcessStartInfo info, int timeout, Func<bool> cancelled, bool acceptFound, bool tail = false)
		{
			if (timeout <= 0)
			{
				throw new ArgumentOutOfRangeException("timeout");
			}
			if (cancelled())
			{
				throw new CommandFailure(CommandProblem.Cancelled);
			}
			Stopwatch stopwatch = Stopwatch.StartNew();
			Process process = new Process();
			process.StartInfo = info;
			using (Process process2 = process)
			{
				try
				{
					if (!process2.Start())
					{
						throw new CommandFailure(CommandProblem.Start);
					}
					try
					{
						process2.PriorityClass = ProcessPriorityClass.BelowNormal;
					}
					catch
					{
					}
				}
				catch (Win32Exception ex)
				{
					throw new CommandFailure(CommandProblem.Start, ex.NativeErrorCode);
				}
				Task<Capture> task = Read(process2.StandardOutput, tail);
				Task<Capture> task2 = Read(process2.StandardError, tail);
				Observe(task);
				Observe(task2);
				bool flag = false;
				try
				{
					while (true)
					{
						if (cancelled())
						{
							throw new CommandFailure(CommandProblem.Cancelled);
						}
						if (stopwatch.ElapsedMilliseconds >= timeout)
						{
							throw new CommandFailure(CommandProblem.Timeout);
						}
						CheckRead(task);
						CheckRead(task2);
						if (process2.HasExited && task.IsCompleted && task2.IsCompleted)
						{
							break;
						}
						Thread.Sleep(25);
					}
					if (process2.ExitCode != 0 && (!acceptFound || process2.ExitCode != 1))
					{
						throw new CommandFailure(CommandProblem.ExitCode, process2.ExitCode);
					}
					flag = true;
					CommandOutput commandOutput = new CommandOutput();
					commandOutput.ExitCode = process2.ExitCode;
					commandOutput.Output = task.Result.Text;
					commandOutput.Error = task2.Result.Text;
					commandOutput.OutputTruncated = task.Result.Truncated;
					commandOutput.ErrorTruncated = task2.Result.Truncated;
					return commandOutput;
				}
				finally
				{
					if (!flag)
					{
						try
						{
							if (!process2.HasExited)
							{
								process2.Kill();
							}
						}
						catch
						{
						}
						try
						{
							process2.StandardOutput.Close();
						}
						catch
						{
						}
						try
						{
							process2.StandardError.Close();
						}
						catch
						{
						}
					}
				}
			}
		}
	}
}
