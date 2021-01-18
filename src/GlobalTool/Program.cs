using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using CommandLine;

namespace Sentinel
{
	class Program
	{
		private static Process _unityProcess;
		private static Task<ParserResult<Options>> _sentinelTask;

		static async Task Main(string[] args)
		{
			AppDomain.CurrentDomain.ProcessExit += CloseUnity;
			Console.CancelKeyPress += (sender, eventArgs) =>
			{
				Console.WriteLine("Exit signal detected. Quitting...");
				Environment.Exit(0);
			};

			_sentinelTask = Parser.Default.ParseArguments<Options>(args).WithParsedAsync(Run);
			await _sentinelTask;
		}

		static async Task Run(Options options)
		{
			var sentinelSrcPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "SentinelPlugin.dll");
			var sentinelTargetPath = Path.Combine(options.ProjectPath, "Assets/__Sentinel.dll");

			try
			{
				var processStartInfo = new ProcessStartInfo
				{
					FileName = options.UnityPath,
					Arguments = $"-projectpath \"{options.ProjectPath}\" -nographics -batchmode -stackTraceLogType None -silent-crashes -logFile ",
					RedirectStandardOutput = true,
					UseShellExecute = false
				};
				
				Console.WriteLine($"Copying Sentinel assembly to '{sentinelTargetPath}'");
				File.Copy(sentinelSrcPath, sentinelTargetPath);
				
				Console.WriteLine("Starting Unity...");

				_unityProcess = new Process {StartInfo = processStartInfo};
				_unityProcess.OutputDataReceived += (sender, args) =>
				{
					if (args.Data?.StartsWith("SENTINEL >>>>>>>>>> ") == false)
						return;

					Console.WriteLine($"{args.Data?.Replace("SENTINEL >>>>>>>>>> ", "")}");
				};
				_unityProcess.Start();
				_unityProcess.BeginOutputReadLine();

				await _unityProcess.WaitForExitAsync();

				Console.WriteLine("Unity exited");
			}
			finally
			{
				// delete the sentinel
				Console.WriteLine("Deleting Sentinel...");
				if (File.Exists(sentinelTargetPath))
					File.Delete(sentinelTargetPath);

				if(File.Exists(sentinelTargetPath + ".meta"))
					File.Delete(sentinelTargetPath + ".meta");
			}
		}

		private static void CloseUnity(object? sender, EventArgs eventArgs)
		{
			Console.WriteLine($"Killing Unity process...");
			_unityProcess?.Kill(true);
			Task.WaitAll(_sentinelTask);
		}
	}
}