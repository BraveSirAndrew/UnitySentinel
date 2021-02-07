# Unity Sentinel

## What is it?

Unity Sentinel is a continuous testing tool for Unity developers. It helps to ease the experience of testing in Unity by removing the need to constantly switch between your IDE and Unity.

## Installation

Unity Sentinel is a .Net global tool, so installing it is easy. Make sure you have [.Net 5](https://dotnet.microsoft.com/download/dotnet/5.0) installed, then at the command prompt, type:

```
dotnet tool install -g UnitySentinel
```

To uninstall type:

```
dotnet tool uninstall -g UnitySentinel
```

and for updates:

```
dotnet tool update -g UnitySentinel
```

Pretty easy.


## Usage

The simplest way to run the tool is to navigate to your Unity project directory in your terminal and type

```
UnitySentinel
```

but you can also run it from any location and specify the project using the --projectpath command line argument.

Type 

```
UnitySentinel --help
```

for a list of other command line arguments.

## How does it work?

It's pretty dumb really. On startup, Unity Sentinel copies a small plugin called __Sentinel.dll to your Unity project's Assets folder. It then loads your project into Unity using batch mode.

:exclamation::exclamation::exclamation: **You can't have the same project open in the Unity editor at the same time as running this tool.** You'll get the standard error from Unity about multiple instances of a project being open at the same time.

The plugin uses .Net file system watchers to watch for changes to .cs files in your Assets folder, as well as any additional folders you pass using the --watchpaths argument. When a change is detected, it recompiles the code and uses the Unity test framework API to programatically run your tests.

The test run results are reported in the console window and make it clear when a test has failed.

The __Sentinel.dll file is deleted on exit.
