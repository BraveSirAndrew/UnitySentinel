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

	public static string GetArgumentValue(string name)
	{
		return GetArgumentValue(name, Environment.GetCommandLineArgs());
	}

	public static string GetArgumentValue(string name, ICollection<string> args)
	{
		return args
			.SkipWhile(a => a != name)
			.Skip(1)
			.FirstOrDefault();
	}
}