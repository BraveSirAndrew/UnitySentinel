using System.Collections.Generic;
using System.Linq;
using UnityEditor.TestTools.TestRunner.Api;
using static SentinelLog;

public class TestRunnerCallbacks : ICallbacks
{
	public class Colors
	{
		public static string Black = "\u001b[30m";
		public static string Red = "\u001b[31m";
		public static string Green = "\u001b[32m";
		public static string Yellow = "\u001b[33m";
		public static string Blue = "\u001b[34m";
		public static string Magenta = "\u001b[35m";
		public static string Cyan = "\u001b[36m";
		public static string White = "\u001b[37m";
		public static string Reset = "\u001b[0m";
	}

	private string Colorize(string s, string color) => $"{color}{s}{Colors.Reset}";
	private string Red(string s) => Colorize(s, Colors.Red);
	private string Green(string s) => Colorize(s, Colors.Green);
	private string Yellow(string s) => Colorize(s, Colors.Yellow);

	private static bool IsFailed(ITestResultAdaptor r) => r.ResultState.StartsWith("Failed");
	
	private readonly List<ITestResultAdaptor> _results = new List<ITestResultAdaptor>();
	private static Dictionary<string, string> _resultStateColors = new Dictionary<string, string>() {
		{"Failed", Colors.Red},
		{"Failed:Error", Colors.Red},
		{"Passed", Colors.Green},
		{"Skipped", Colors.Yellow},
		{"Skipped:Ignored", Colors.Yellow}
	};


	public void RunStarted(ITestAdaptor testsToRun)
	{
		Log($"Running tests...");
	}

	public void RunFinished(ITestResultAdaptor result)
	{
		var resultState = result.ResultState.Replace("(Child)", "");
		Log($"");
		Log($"{Colorize("Test Run Summary", Colors.Yellow)}");
		Log($"{Colorize("================================", Colors.Yellow)}");
		Log("");

		// failed items next so they are more visible
		_results.Where(IsFailed).ToList().ForEach(r =>
		{
			OutputResult(r);
			Log("");
		});

		Log(Colorize(new string('=', 60), GetResultColor(resultState)));
		Log($"Test Run Finished " +
		    $"{Colorize(result.FailCount.ToString(), Colors.Yellow)} Failed, " +
		    $"{Colorize(result.SkipCount.ToString(), Colors.Yellow)} Skipped, " +
		    $"{Colorize(result.PassCount.ToString(), Colors.Yellow)} Passed");
		Log(Colorize(new string('=', 60), GetResultColor(resultState)));
	}

	public void TestFinished(ITestResultAdaptor result)
	{
		_results.Add(result);
		OutputResult(result);
	}

	public void TestStarted(ITestAdaptor test)
	{
	}

	private void OutputResult(ITestResultAdaptor result)
	{
		if (result.HasChildren)
			return;

		var resultColour = GetResultColor(result.ResultState);
		
		if (IsFailed(result) || !string.IsNullOrWhiteSpace(result.Output))
		{
			Log($"{Colorize(result.ResultState, resultColour)} - Test {Colorize(result.Test.FullName, Colors.Cyan)}");
			Log(new string('-', 80));
		}

		if (IsFailed(result))
		{
			Log("");
			Log($"{result.Message}");

			Log("");
			Log("StackTrace:");
			Log($"{result.StackTrace}");
			Log("");
		}

		if (!string.IsNullOrWhiteSpace(result.Output))
		{
			Log($"{result.Output}");
			Log("");
		}
	}
	
	private string GetResultColor(string resultState)
	{
		return _resultStateColors.TryGetValue(resultState, out var value) ? value : Colors.Reset;
	}
}