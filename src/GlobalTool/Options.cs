using CommandLine;

namespace Sentinel
{
	public class Options
	{
		[Option('u', "unitypath", Required = true, HelpText = "The path to the Unity executable you want to run")]
		public string UnityPath { get; set; }

		[Option('p', "projectpath", Required = true, HelpText = "The path to the Unity project")]
		public string ProjectPath { get; set; }
	}
}