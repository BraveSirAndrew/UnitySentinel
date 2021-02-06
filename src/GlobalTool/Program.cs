using System;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CommandLine;
using Spectre.Console;

namespace Sentinel
{
	static class Program
	{
		private static UnityProcess _unityProcess;

		static async Task Main(string[] args)
		{
			AppDomain.CurrentDomain.ProcessExit += (_, _) =>
			{
				Console.WriteLine($"Killing Unity process...");

				var projectPath = _unityProcess?.ProjectPath;
				_unityProcess?.Dispose();

				CleanUp(projectPath);
			};

			Console.CancelKeyPress += (_, _) =>
			{
				Console.WriteLine("Exit signal detected. Quitting...");
				Environment.Exit(0);
			};

			await Parser.Default.ParseArguments<Options>(args).MapResult(async o =>
			{
				_unityProcess = new UnityProcess(ParseUnityPathFromProject(o.ProjectPath) ?? o.UnityPath,
					o.ProjectPath ?? Environment.CurrentDirectory, o.AssemblyNames, o.TestNames, o.TestCategories, o.WatchPaths, o.TestMode);

				AnsiConsole.Render(new FigletText(FigletFont.Load("doom.flf"), "Sentinel"));

				try
				{
					CopySentinelFiles(o.ProjectPath);
					await StartUnity();
					AnsiConsole.MarkupLine($"\n[green]Unity startup completed. Monitoring for changes...Ctrl-C to quit[/]");
					await MonitorChanges();

					await _unityProcess.WaitForExit();
				}
				finally
				{
					CleanUp(o.ProjectPath);
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
			Console.WriteLine("Deleting Sentinel...");
			var sentinelTargetPath = GetSentinelTargetPath(projectPath);
			if (File.Exists(sentinelTargetPath))
				File.Delete(sentinelTargetPath);

			if (File.Exists(sentinelTargetPath + ".meta"))
				File.Delete(sentinelTargetPath + ".meta");
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
			return Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "SentinelPlugin.dll");
		}

		private static string ParseUnityPathFromProject(string projectPath)
		{
			if (string.IsNullOrEmpty(projectPath))
				return null;

			var projectSettingsPath = Path.Combine(projectPath, "ProjectSettings", "ProjectVersion.txt");
			if (File.Exists(projectSettingsPath) == false)
			{
				AnsiConsole.MarkupLine($"[red]Couldn't find ProjectVersion.txt at '{projectSettingsPath}'. Without this file, you will need to " +
				                       $"use the [bold]--unitypath[/] switch to specify the path to the Unity editor executable manually.[/]");
				return null;
			}

			var projectVersionText = File.ReadAllText(projectSettingsPath);
			var version = Regex.Match(projectVersionText, @"20\d{2}\.\d\.\w{3,4}|3").Value;
			
			var unityPath = Path.Combine(@"C:\Program Files\Unity\Hub\Editor", version, "Editor", "Unity.exe");
			if(File.Exists(unityPath) == false)
			{
				AnsiConsole.MarkupLine($"[red]Couldn't find the Unity executable at '{unityPath}'. Please specify the Unity path manually " +
				                       $"using the [bold]--unitypath[/] switch.[/]");
				return null;
			}

			return unityPath;
		}
	}
}