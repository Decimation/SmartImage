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

	internal static SearchConfig Config { get; private set; }

	internal static SearchClient Client { get; private set; }

	internal static SearchQuery _query;

	//todo
	internal static CancellationTokenSource Cts { get; } = new();

	//todo
	internal static List<SearchResult> Results { get; } = new();

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
		Config = new SearchConfig();
		Client = new SearchClient(Config);

		var configuration = new ConfigurationBuilder();
		configuration.AddJsonFile("smartimage.json", optional: true, reloadOnChange: true);
		IConfigurationRoot configurationRoot = configuration.Build();
		configurationRoot.Bind(Config);

		Application.Init();

		Console.OutputEncoding = Encoding.Unicode;

		Gui.Init();

		Application.Run();
		Application.Shutdown();

	}
}