using System.Linq;
using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.Execution;
using Nuke.Common.Git;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.GitVersion;
using Nuke.Common.Utilities.Collections;
using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.Tools.DotNet.DotNetTasks;
using static Nuke.Common.IO.PathConstruction;

[CheckBuildProjectConfigurations]
[ShutdownDotNetAfterServerBuild]
class Build : NukeBuild
{
	public static int Main() => Execute<Build>(x => x.Push);

	[Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
	readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

	[Solution] readonly Solution Solution;
	[GitRepository] readonly GitRepository GitRepository;
	[GitVersion(Framework = "netcoreapp3.1")] readonly GitVersion GitVersion;

	[Parameter] string NugetApiUrl = "https://api.nuget.org/v3/index.json";
	[Parameter] string NugetApiKey;

	AbsolutePath OutputDirectory => RootDirectory / "output";
	AbsolutePath NugetDirectory => OutputDirectory / "nuget";

	Target Clean => _ => _
		.Before(Restore)
		.Executes(() =>
		{
			EnsureCleanDirectory(OutputDirectory);
		});

	Target Restore => _ => _
		.Executes(() =>
		{
			DotNetRestore(s => s
				.SetProjectFile(Solution));
		});

	Target Compile => _ => _
		.DependsOn(Restore)
		.Executes(() =>
		{
			DotNetBuild(s => s
				.SetProjectFile(Solution)
				.SetConfiguration(Configuration)
				.SetAssemblyVersion(GitVersion.AssemblySemVer)
				.SetFileVersion(GitVersion.AssemblySemFileVer)
				.SetInformationalVersion(GitVersion.InformationalVersion)
				.EnableNoRestore());
		});

	Target Pack => _ => _
		.DependsOn(Compile)
		.Executes(() =>
		{
			DotNetPack(s => s
				.SetProject(Solution)
				.SetConfiguration(Configuration)
				.EnableNoBuild()
				.EnableNoRestore()
				.SetVersion(GitVersion.NuGetVersionV2)
				.SetDescription("A continuous testing tool for Unity")
				.SetPackageTags("unity testing")
				.SetAuthors("Andrew O'Connor")
				.SetPackageIconUrl("https://github.com/BraveSirAndrew/Sentinel")
				.SetNoDependencies(true)
				.SetOutputDirectory(OutputDirectory / "nuget"));
		});

	Target Push => _ => _
		.DependsOn(Pack)
		.Requires(() => NugetApiUrl)
		.Requires(() => NugetApiKey)
		.Requires(() => Configuration.Equals(Configuration.Release))
		.Executes(() =>
		{
			GlobFiles(NugetDirectory, "*.nupkg")
				.NotEmpty()
				.Where(x => !x.EndsWith("symbols.nupkg"))
				.ForEach(x =>
				{
					DotNetNuGetPush(s => s
						.SetTargetPath(x)
						.SetSource(NugetApiUrl)
						.SetApiKey(NugetApiKey)
					);
				});
		});
}
