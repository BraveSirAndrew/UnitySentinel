using System;
using System.Diagnostics;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace UnitySentinel
{
	public class UnityProcess : IDisposable
	{
		private const string SentinelLogMarker = "SENTINEL >>>>>>>>>> ";
		private const string ProjectLoadedLogMarker = "[Project] Loading completed";

		private Process _process;
		private Channel<string> _consoleOutputChannel;

		public UnityProcess(string unityExecutablePath, string projectPath, string assemblyNames,
			string testNames, string testCategories, string watchPaths, string testMode)
		{
			ProjectPath = projectPath;
			Status = UnityProcessStatus.StartingUp;

			var customArguments = $"{GetCustomArg("testMode", testMode)} " +
								  $"{GetCustomArg("testNames", testNames)} " +
								  $"{GetCustomArg("testCategories", testCategories)} " +
								  $"{GetCustomArg("assemblyNames", assemblyNames)} " +
								  $"{GetCustomArg("watchPaths", watchPaths)}";

			var processStartInfo = new ProcessStartInfo
			{
				FileName = unityExecutablePath,
				Arguments = $"-projectpath \"{projectPath}\" -nographics -batchmode -stackTraceLogType None -silent-crashes -logFile {customArguments}",
				RedirectStandardOutput = true,
				UseShellExecute = false
			};

			_consoleOutputChannel = Channel.CreateUnbounded<string>(new UnboundedChannelOptions { SingleReader = true, SingleWriter = true });

			_process = new Process { StartInfo = processStartInfo };
			_process.OutputDataReceived += (_, args) =>
			{
				if (args.Data == null) return;

				if (args.Data.StartsWith(ProjectLoadedLogMarker))
					Status = UnityProcessStatus.ProjectLoadComplete;

				switch (Status)
				{
					case UnityProcessStatus.StartingUp:
						_consoleOutputChannel.Writer.WriteAsync(args.Data);
						break;

					case UnityProcessStatus.ProjectLoadComplete:
						if (args.Data?.StartsWith(SentinelLogMarker) == false)
							return;

						_consoleOutputChannel.Writer.WriteAsync($"{args.Data?.Replace(SentinelLogMarker, "")}");
						break;

					default:
						throw new ArgumentOutOfRangeException();
				}
			};
		}

		public UnityProcessStatus Status { get; private set; }
		public string ProjectPath { get; }
		public ChannelReader<string> Output => _consoleOutputChannel.Reader;

		public void Start()
		{
			_process.Start();
			_process.BeginOutputReadLine();
		}

		public async Task WaitForExit()
		{
			await _process.WaitForExitAsync();
		}

		public void Dispose()
		{
			_process?.Kill(true);
			_process?.Dispose();
		}

		private string GetCustomArg(string arg, string value)
		{
			return string.IsNullOrEmpty(value) ? "" : $"-{arg} {value}";
		}
	}
}