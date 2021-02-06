using UnityEngine;

public static class SentinelLog
{
	public static void Log(string msg)
	{
		Debug.Log($"SENTINEL >>>>>>>>>> {msg}");
	}

	public static string Colorize(string s, string color) => $"{color}{s}{TestRunnerCallbacks.Colors.Reset}";
}