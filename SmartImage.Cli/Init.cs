using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using SmartImage.Lib;

namespace SmartImage.Cli;

public static class Init
{
	private static readonly AssemblyName Assembly;

	static Init()
	{
		Assembly = typeof(SearchClient).Assembly.GetName();

	}

	[ModuleInitializer]
	public static void Setup()
	{
		Trace.WriteLine($"{Resources.Name}:: {nameof(Setup)} | {Assembly.Version}");
	}
}