using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;
using Debug = UnityEngine.Debug;
using static SentinelLog;

public class SentinelBootstrap
{
	[InitializeOnLoadMethod]
	public static void OnInitialize()
	{
		var testMode = TestMode.PlayMode;
		if (CommandLine.TryGetArgumentValue("-testMode", out var testModeValue) == false || Enum.TryParse(testModeValue, out testMode) == false)
			Debug.LogWarning($"Couldn't parse testMode from command line. Argument was '{testModeValue}");

		var testCategories = GetTestCategories();
		var testNames = GetTestNames();
		var assemblyNames = GetAssemblyNames();


		var messageQueue = new ConcurrentQueue<Action>();
		EditorApplication.update = () =>
		{
			if (messageQueue.TryDequeue(out var action))
				action();
		};

		var testRunnerApi = ScriptableObject.CreateInstance<TestRunnerApi>();
		testRunnerApi.RegisterCallbacks(new TestRunnerCallbacks());

		CompilationPipeline.assemblyCompilationFinished += (_, messages) =>
		{
			if (messages.Length == 0) 
			{
				Log($"Test Mode: {Colorize(testMode.ToString(), TestRunnerCallbacks.Colors.Yellow)}, " +
				    $"Test Names: {Colorize(string.Join(", ", testNames ?? new []{"Not filtered"}), TestRunnerCallbacks.Colors.Yellow)}, " +
				    $"Test Categories: {Colorize(string.Join(", ", testCategories ?? new []{"Not filtered"}), TestRunnerCallbacks.Colors.Yellow)}, " +
				    $"Assemblies: { Colorize(string.Join(", ", assemblyNames ?? new[] { "Not filtered" }), TestRunnerCallbacks.Colors.Yellow)}"); 

				testRunnerApi.Execute(new ExecutionSettings(new Filter
				{
					testMode = testMode,
					categoryNames = testCategories,
					testNames = testNames,
					assemblyNames = assemblyNames 
				}));
			}
			else
			{
				Log($"Not running tests because of compilation errors:");
				messages.ToList().ForEach(m => Log(Colorize(m.message, TestRunnerCallbacks.Colors.Red)));
			}
		};

		var watchedPaths = new List<string> {Application.dataPath};
		watchedPaths.AddRange(GetAdditionalWatchedPaths());
		var fileSystemWatchers = new List<FileSystemWatcher>();
		var recompileAction = (FileSystemEventHandler) ((_, args) =>
		{
			messageQueue.Enqueue(() =>
			{
				try
				{
					Log($"Compiling...");

					var relativeUri = new Uri(Application.dataPath).MakeRelativeUri(new Uri(args.FullPath));
					var relativePath = Uri.UnescapeDataString(relativeUri.ToString());

					AssetDatabase.ImportAsset(relativePath, ImportAssetOptions.ForceUpdate);
				}
				catch (Exception e)
				{
					Log($"Couldn't parse paths: {e.Message}");
				}
			});
		});

		foreach (var watcherPath in watchedPaths)
		{
			var fileSystemWatcher = new FileSystemWatcher(watcherPath, "*.cs");
			fileSystemWatcher.Changed += recompileAction;
			fileSystemWatcher.Deleted += recompileAction;
			fileSystemWatcher.EnableRaisingEvents = true;
			fileSystemWatcher.IncludeSubdirectories = true;

			fileSystemWatchers.Add(fileSystemWatcher);
		}

		AssemblyReloadEvents.beforeAssemblyReload += () =>
		{
			foreach (var watcher in fileSystemWatchers)
			{
				watcher?.Dispose();
			}
		};
	}

	private static IEnumerable<string> GetAdditionalWatchedPaths()
	{
		return CommandLine.GetSeperatedListValue("-watchPaths") ?? new string[0];
	}

	private static string[] GetAssemblyNames()
	{
		return CommandLine.GetSeperatedListValue("-assemblyNames");
	}

	private static string[] GetTestNames()
	{
		return CommandLine.GetSeperatedListValue("-testNames");
	}
	
	private static string[] GetTestCategories()
	{
		return CommandLine.GetSeperatedListValue("-testCategories");
	}
}