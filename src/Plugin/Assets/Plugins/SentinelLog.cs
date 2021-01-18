using UnityEngine;

public static class SentinelLog
{
	public static void Log(string msg)
	{
		Debug.Log($"SENTINEL >>>>>>>>>> {msg}");
	}
}