using System;
using System.Collections.Generic;
using System.Linq;

public static class CommandLine
{
	public static bool HasArgument(string name)
	{
		return HasArgument(name, Environment.GetCommandLineArgs());
	}

	public static bool HasArgument(string name, ICollection<string> args)
	{
		return args.Contains(name);
	}

	public static bool TryGetArgumentValue(string name, out string value)
	{
		value = GetArgumentValue(name, Environment.GetCommandLineArgs());
		return value != null;
	}

	public static string GetArgumentValue(string name)
	{
		return GetArgumentValue(name, Environment.GetCommandLineArgs());
	}

	public static string[] GetSeperatedListValue(string name, string seperator = ";")
	{
		if (TryGetArgumentValue(name, out var values) == false)
			return null;

		return values.Split(new[] {seperator}, StringSplitOptions.RemoveEmptyEntries);
	}

	public static string GetArgumentValue(string name, ICollection<string> args)
	{
		return args
			.SkipWhile(a => a != name)
			.Skip(1)
			.FirstOrDefault();
	}
}