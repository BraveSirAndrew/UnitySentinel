using CommandLine;

namespace Sentinel
{
	public class Options
	{
		[Option("unitypath", Required = false, HelpText = "The path to the Unity executable you want to run")]
		public string UnityPath { get; set; }

		[Option("projectpath", Required = false, HelpText = "The path to the Unity project")]
		public string ProjectPath { get; set; }

		[Option("testmode", Required = false, Default = "PlayMode", HelpText = "Run play-mode or edit-mode tests. Specify as PlayMode or EditMode. Default if not specified is PlayMode.")]
		public string TestMode { get; set; }

		[Option("watchpaths", Required = false, HelpText = "Specify additional paths outside of the project Asset folder to watch for changes, separated by a semi-colon.")]
		public string WatchPaths { get; set; }

		[Option("assemblynames", Required = false, HelpText = "A semicolon-separated list of test assemblies to test")]
		public string AssemblyNames { get; set; }

		[Option("testnames", Required = false, HelpText = "A semicolon-separated list of test names to run, or a regular expression pattern to match tests by their full name.")]
		public string TestNames { get; set; }

		[Option("testcategories", Required = false, HelpText = "A semicolon-separated list of test categories to include in the run.")]
		public string TestCategories { get; set; }
	}
}