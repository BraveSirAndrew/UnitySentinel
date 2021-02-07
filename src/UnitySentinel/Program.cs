using System;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CommandLine;
using Spectre.Console;

namespace UnitySentinel
{
	static class Program
	{
		private static UnityProcess _unityProcess;

		static async Task Main(string[] args)
		{
			AppDomain.CurrentDomain.ProcessExit += (_, _) =>
			{
				var projectPath = _unityProcess?.ProjectPath;

				if (_unityProcess != null && _unityProcess.IsRunning())
				{
					Console.WriteLine($"Killing Unity process...");
					_unityProcess?.Dispose();
				}

				CleanUp(projectPath);
			};

			Console.CancelKeyPress += (_, _) =>
			{
				Console.WriteLine("Exit signal detected. Quitting...");
				Environment.Exit(0);
			};

			using (var figletStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("UnitySentinel.figletfont.flf"))
			{
				if (figletStream == null)
					AnsiConsole.MarkupLine($"[green]Unity Sentinel[/]");
				else
					AnsiConsole.Render(new FigletText(FigletFont.Load(figletStream), "Unity Sentinel"));
			}

			await Parser.Default.ParseArguments<Options>(args).MapResult(async o =>
			{
				var projectPath = o.ProjectPath ?? Environment.CurrentDirectory;
				var unityExecutablePath = o.UnityPath ?? ParseUnityPathFromProject(projectPath);
				if (File.Exists(unityExecutablePath) == false)
				{
					AnsiConsole.MarkupLine($"[red]Couldn't find Unity editor executable. Use the [bold]--unitypath[/] argument to specify it. Exiting.[/]");
					return;
				}

				_unityProcess = new UnityProcess(unityExecutablePath, projectPath, o.AssemblyNames, o.TestNames, o.TestCategories, o.WatchPaths, o.TestMode);
				
				try
				{
					CopySentinelFiles(projectPath);
					await StartUnity();
					AnsiConsole.MarkupLine($"\n[green]Unity startup completed. Monitoring for changes...Ctrl-C to quit[/]");
					await MonitorChanges();

					await _unityProcess.WaitForExit();
				}
				finally
				{
					CleanUp(projectPath);
				}
			}, _ => Task.FromResult(1));
		}

		private static async Task StartUnity()
		{
			await AnsiConsole
				.Status()
				.AutoRefresh(true)
				.Spinner(Spinner.Known.Dots)
				.StartAsync("[yellow]Starting Unity...Ctrl-C to quit[/]", async ctx =>
				{
					_unityProcess.Start();
					while (_unityProcess.Status == UnityProcessStatus.StartingUp)
					{
						var line = await _unityProcess.Output.ReadAsync();
						if (line != null)
							AnsiConsole.WriteLine(line);
					}
				});
		}

		private static async Task MonitorChanges()
		{
			await Task.Run(async () =>
			{
				while (_unityProcess.Status == UnityProcessStatus.ProjectLoadComplete)
				{
					var line = await _unityProcess.Output.ReadAsync();
					if (line != null)
						AnsiConsole.WriteLine(line);
				}
			});
		}

		private static void CleanUp(string projectPath)
		{
			var sentinelTargetPath = GetSentinelTargetPath(projectPath);
			if (File.Exists(sentinelTargetPath))
			{
				Console.WriteLine("Deleting Sentinel...");
				File.Delete(sentinelTargetPath);

				if (File.Exists(sentinelTargetPath + ".meta"))
					File.Delete(sentinelTargetPath + ".meta");
			}
		}

		private static void CopySentinelFiles(string projectPath)
		{
			var sentinelSrcPath = GetSentinelSrcPath();
			var sentinelTargetPath = GetSentinelTargetPath(projectPath);

			try
			{
				Console.WriteLine($"Copying Sentinel assembly to '{sentinelTargetPath}'");
				File.Copy(sentinelSrcPath, sentinelTargetPath);
			}
			catch (IOException)
			{
				AnsiConsole.MarkupLine($"[red] Couldn't copy sentinel assembly[/]");
			}
		}

		private static string GetSentinelTargetPath(string projectPath)
		{
			if (projectPath == null)
				return string.Empty;

			return Path.Combine(projectPath, "Assets/__Sentinel.dll");
		}

		private static string GetSentinelSrcPath()
		{
			return Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Content", "SentinelPlugin.dll");
		}

		private static string ParseUnityPathFromProject(string projectPath)
		{
			if (string.IsNullOrEmpty(projectPath))
				return string.Empty;

			var projectSettingsPath = Path.Combine(projectPath, "ProjectSettings", "ProjectVersion.txt");
			if (File.Exists(projectSettingsPath) == false)
				return string.Empty;

			var projectVersionText = File.ReadAllText(projectSettingsPath);
			var version = Regex.Match(projectVersionText, @"20\d{2}\.\d\.\w{3,4}|3").Value;

			var unityPath = Path.Combine(@"C:\Program Files\Unity\Hub\Editor", version, "Editor", "Unity.exe");
			if (File.Exists(unityPath) == false)
			{
				AnsiConsole.MarkupLine($"[red]Couldn't find the Unity executable at '{unityPath}'. Please specify the Unity executable path manually " +
									   $"using the [bold]--unitypath[/] switch.[/]");
				return string.Empty;
			}

			return unityPath;
		}
	}
}