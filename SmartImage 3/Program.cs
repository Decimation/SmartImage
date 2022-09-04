#nullable disable
using System.Reflection;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.Extensions.Hosting;
using SmartImage.Lib;
using Terminal.Gui;
using static SmartImage.Gui;
using Rune = System.Text.Rune;

#pragma warning disable CS0168

// ReSharper disable InconsistentNaming

namespace SmartImage;

using Microsoft.Extensions.Configuration;

public static class Program
{
	#region

	internal static readonly SearchConfig Config = new();
	internal static readonly SearchClient Client = new(Config);

	internal static SearchQuery _query;

	internal static readonly CancellationTokenSource Cts  = new();
	internal static readonly List<SearchResult>      _res = new();

	#endregion

	//todo

	//todo

	[ModuleInitializer]
	public static void Init()
	{
		Trace.WriteLine("Init", Resources.Name);

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