#region Global usings

global using P = SmartImage_3.Program;
global using G = SmartImage_3.Gui;
global using R = SmartImage_3.Resources;
global using GV = SmartImage_3.Gui.Values;
global using GS = SmartImage_3.Gui.Styles;
#endregion
#nullable disable
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using SmartImage.Lib;
using SmartImage.Lib.Searching;
using Terminal.Gui;

#pragma warning disable CS0168

// ReSharper disable InconsistentNaming

namespace SmartImage_3;

public static class Program
{
	#region

	internal static readonly SearchConfig Config = new();
	internal static readonly SearchClient Client = new(Config);

	internal static ImageQuery _query;

	internal static readonly CancellationTokenSource Cts  = new();
	internal static readonly List<SearchResult>      _res = new();

	#endregion

	//todo

	//todo

	[ModuleInitializer]
	public static void Init()
	{
		Trace.WriteLine("Init", R.Name);

		/*var types = new [] { typeof(G), typeof(G.Values), typeof(G.Styles), typeof(Program) };

		foreach (Type type in types) {
			RuntimeHelpers.RunClassConstructor(type.TypeHandle);
		}*/

		// Gui.Init();
	}

	public static async Task Main(string[] args)
	{
		Application.Init();

		Console.OutputEncoding = Encoding.Unicode;

		Gui.Init();

		Application.Run();
		Application.Shutdown();
	}
}