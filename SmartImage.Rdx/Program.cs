using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using Flurl.Http;
using Flurl.Http.Configuration;
using Kantan.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Novus.Streams;
using Novus.Win32;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SmartImage.Lib;
using SmartImage.Lib.Images;
using SmartImage.Lib.Images.Uni;
using Spectre.Console;
using Spectre.Console.Cli;
using SmartImage.Rdx.Shell;
using SmartImage.Rdx.Utilities;
using SmartImage.Rdx.Commands;
using SmartImage.Lib.Utilities.Integration;

namespace SmartImage.Rdx;

public static class Program
{

	public static async Task<int> Main(string[] args)
	{
		/*AppDomain.CurrentDomain.UnhandledException += (sender, eventArgs) =>
		{
			Trace.WriteLine($"{sender} -> {eventArgs}");
		};*/

		Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

#if DEBUG

		// Debugger.Launch();
#endif
		HandleArgs(ref args);

		await DisplayHeaderAsync();

		DisplayInfoGrid();

		var app = new CommandApp<SearchCommand>();

		app.Configure(c =>
		{
#if DEBUG
			c.PropagateExceptions();
			c.ValidateExamples();
#endif

			var helpProvider = new CustomHelpProvider(c.Settings);
			c.SetHelpProvider(helpProvider);

			c.AddCommand<IntegrationCommand>("integrate")
				.WithDescription("Configure system integration such as context menu");

			c.AddCommand<ServerCommand>("server")
				.WithDescription("Start listen server");
		});

		int x = BaseOSIntegration.EC_OK;

		try {
			x = await app.RunAsync(args);

		}
		catch (Exception e) {
			AnsiConsole.WriteException(e);
			x = BaseOSIntegration.EC_ERROR;
		}
		finally {

			if (x != BaseOSIntegration.EC_OK) {
				AnsiConsole.Confirm("Press any key to continue");
			}
		}

		return x;
	}

	private static void DisplayInfoGrid()
	{
		Grid grd = ConsoleFormat.MapToGrid(ConsoleFormat.InfoMap);
		AnsiConsole.Write(grd);
	}

	private static async Task DisplayHeaderAsync()
	{
		var ff = ConsoleFormat.LoadFigletFontFromResource(nameof(R2.Fg_larry3d), out var ms);

		var fg = new FigletText(ff, R1.Name)
			.LeftJustified()
			.Color(ConsoleFormat.Clr_Misc1);
		await ms.DisposeAsync();

		AnsiConsole.Write(fg);
	}

	private static void HandleArgs(ref string[] args)
	{
		if (args.Length == 0) {

			// todo

			/*if (Clipboard.Open()) {
				/*var hasBmp = Clipboard.IsFormatAvailable((uint) ClipboardFormat.CF_BITMAP);

				if (hasBmp) {
					var data = (nint) Clipboard.GetData((uint) ClipboardFormat.CF_BITMAP);
					var sz   = Native.GlobalSize(data);
					var buf  = new byte[sz];
					Marshal.Copy(data, buf, 0, (int) sz);
					var mg = await Image.LoadAsync(new MemoryStream(buf));

				}#1#

				var hasFileName = Clipboard.IsFormatAvailable((uint) ClipboardFormat.FileNameW);

			}*/

			// var s = AnsiConsole.Ask<string>("...");

		}

		/*if (args.Length == 0) {
			var prompt = new TextPrompt<string>("Input")
			{
				Converter = s =>
				{
					/*
					var task = SearchQuery.TryCreateAsync(s);
					task.Wait();
					var res = task.Result;
					#1#

					if (UniImage.IsValidSourceType(s)) {
						// var sq = SearchQuery.TryCreateAsync(s).Result;

						return s;
					}

					else {
						return null;
					}
				}
			};
			var sz = AnsiConsole.Prompt(prompt);

			args = [sz];
		}*/
		if (Console.IsInputRedirected) {
			Trace.WriteLine("Input redirected");
			var pipeInput = ConsoleUtil.ParseInputStream();

			var newArgs = new string[args.Length + 1];
			newArgs[0] = pipeInput;
			args.CopyTo(newArgs, 1);

			args = newArgs;

			AnsiConsole.WriteLine($"Received input from stdin");
		}
	}


	private static IConfigurationRoot GetConfig()
	{
		/*var bldr2 = new ConfigurationBuilder();
		var host  = Host.CreateDefaultBuilder();
		var bldr  = host.ConfigureServices((ctx, svc) => { svc.AddSingleton<SearchConfig>(); });

		bldr2.SetBasePath(Directory.GetCurrentDirectory())
			.AddJsonFile("smartimage.json", optional: false, reloadOnChange: true);
			*/

		// TODO

		var currentDirectory = Directory.GetCurrentDirectory();
		var configFileName   = $"{R1.Name}.json";
		var configFilePath   = Path.Combine(currentDirectory, configFileName);

		var cfg = new ConfigurationBuilder()
			.AddJsonFile(configFileName)
			.Build();

		return cfg;
	}

	public static readonly Assembly Assembly = Assembly.GetExecutingAssembly();

	public static readonly Version  Version  = Assembly.GetName().Version;

}