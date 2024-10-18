using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Channels;
using Flurl.Http;
using Flurl.Http.Configuration;
using Kantan.Text;
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

namespace SmartImage.Rdx;

public static class Program
{

	public static async Task<int> Main(string[] args)
	{
		Debug.WriteLine(AConsole.Profile.Height);
		Debug.WriteLine(Console.BufferHeight);

		Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

#if DEBUG
		Debugger.Launch();
#endif
		if (args.Length == 0) {


			if (Clipboard.Open()) {
				var hasBmp = Clipboard.IsFormatAvailable((uint) ClipboardFormat.CF_BITMAP);

				if (hasBmp) {
					
				}
				var hasFileName = Clipboard.IsFormatAvailable((uint) ClipboardFormat.FileNameW);

			}
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
			var sz = AConsole.Prompt(prompt);

			args = [sz];
		}*/

		if (Console.IsInputRedirected) {
			Trace.WriteLine("Input redirected");
			var pipeInput = ConsoleUtil.ParseInputStream();

			var newArgs = new string[args.Length + 1];
			newArgs[0] = pipeInput;
			args.CopyTo(newArgs, 1);

			args = newArgs;

			AConsole.WriteLine($"Received input from stdin");
		}

		var ff = ConsoleFormat.LoadFigletFontFromResource(nameof(R2.Fg_larry3d), out var ms);

		// ms?.Dispose();

		var fg = new FigletText(ff, R1.Name)
			.LeftJustified()
			.Color(ConsoleFormat.Clr_Misc1);

		AConsole.Write(fg);

#if DEBUG
		Trace.WriteLine(args.QuickJoin());
#endif

		Grid grd = ConsoleFormat.CreateInfoGrid();

		AConsole.Write(grd);

		// var env = Environment.GetEnvironmentVariables(EnvironmentVariableTarget.Process);

		var app = new CommandApp<SearchCommand>();

		app.Configure(c =>
		{
			c.PropagateExceptions();
			var helpProvider = new CustomHelpProvider(c.Settings);
			c.SetHelpProvider(helpProvider);

			c.AddCommand<IntegrationCommand>("integrate")
				.WithDescription("Configure system integration such as context menu");

		});
		int x = SearchCommand.EC_OK;

		try {
			x = await app.RunAsync(args);

		}
		catch (Exception e) {
			AConsole.WriteException(e);
			x = SearchCommand.EC_ERROR;
		}
		finally {

			if (x != SearchCommand.EC_OK) {
				AConsole.Confirm("Press any key to continue");
			}
		}

		return x;
	}

}