using System;
using System.Collections.Concurrent;
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
		if (CommandLine.HasArgument("-testMode") && Enum.TryParse(CommandLine.GetArgumentValue("-testMode"), out testMode) == false)
			Debug.LogWarning($"Couldn't parse testMode from command line. Argument was '{CommandLine.GetArgumentValue("-testMode")}");

		var messageQueue = new ConcurrentQueue<Action>();
		EditorApplication.update = () =>
		{
			if (messageQueue.TryDequeue(out var action))
				action();
		};

		var testRunnerApi = ScriptableObject.CreateInstance<TestRunnerApi>();
		testRunnerApi.RegisterCallbacks(new TestRunnerCallbacks());

		CompilationPipeline.assemblyCompilationFinished += (s, messages) =>
		{
			if (messages.Length == 0)
			{
				testRunnerApi.Execute(new ExecutionSettings(new Filter
				{
					testMode = testMode
				}));
			}
			else
			{
				Log($"Not running tests because of compilation errors:");
				messages.ToList().ForEach(m => Log(m.message));
			}
		};

		var fileSystemWatcher = new FileSystemWatcher(Application.dataPath, "*.cs");
		fileSystemWatcher.Changed += (_, args) =>
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
		};
		fileSystemWatcher.EnableRaisingEvents = true;
		fileSystemWatcher.IncludeSubdirectories = true;

		AssemblyReloadEvents.beforeAssemblyReload += () =>
		{
			fileSystemWatcher?.Dispose();
		};
	}
}