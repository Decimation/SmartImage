// ReSharper disable RedundantUsingDirective

#pragma warning disable HAA0601,

#nullable enable
using SimpleCore.Cli;
using SmartImage.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Novus.Win32;
using SimpleCore.Net;
using SimpleCore.Utilities;
using SmartImage.Lib;
using SmartImage.Lib.Engines;
using SmartImage.Lib.Searching;
using SmartImage.Lib.Utilities;
using SmartImage.Utilities;

// ReSharper disable UnusedParameter.Local
#pragma warning disable CA1416

namespace SmartImage
{
	public static class Program
	{
		private static readonly List<NConsoleOption> ResultOptions = new();

		//  ____                       _   ___
		// / ___| _ __ ___   __ _ _ __| |_|_ _|_ __ ___   __ _  __ _  ___
		// \___ \| '_ ` _ \ / _` | '__| __|| || '_ ` _ \ / _` |/ _` |/ _ \
		//  ___) | | | | | | (_| | |  | |_ | || | | | | | (_| | (_| |  __/
		// |____/|_| |_| |_|\__,_|_|   \__|___|_| |_| |_|\__,_|\__, |\___|
		//                                                     |___/


		/*
		 * Entry point
		 */

		private static async Task Main(string[] args)
		{
			/*
			 * Setup
			 * Check compatibility
			 */
			Info.Setup();

			Console.Title = Info.NAME;

			NConsole.Init();

			Console.CancelKeyPress += (sender, eventArgs) => { };
			Console.Clear();
			Console.WriteLine(Info.NAME_BANNER);

			var cfg = new SearchConfig();

			var options = NConsoleOption.FromArray(new[] {"run", "exit", "engines"});

			var io = NConsole.ReadOptions(options).First().ToString();

			switch (io) {
				case "exit":
					return;
				case "engines":
				{
					var e   = NConsoleOption.FromEnum<SearchEngineOptions>();
					var ex  = NConsole.ReadOptions(e, true);
					var ex2 = Enums.ReadFromSet<SearchEngineOptions>(ex);

					cfg.SearchEngines = ex2;

					Console.WriteLine(cfg.SearchEngines);
					break;
				}
			}

			/*
			 * Run search
			 */

			try {

				// Setup
				Integration.Setup();

				// Run search
				ImageQuery q = NConsole.ReadInput(null, x =>
				{
					x = x.Trim('\"');
					return !(ImageUtilities.IsDirectImage(x) || File.Exists(x));
				});

				cfg.Query = q;

				var client = new SearchClient(cfg);

				client.ResultCompleted += ResultCompleted;

				// Show results

				var searchTask = client.RunSearchAsync();
				NConsole.ReadOptions(ResultOptions);

				await searchTask;
			}
			catch (Exception exception) {
#if !DEBUG
#else
				Console.WriteLine(exception);
#endif
			}
			finally {
				// Exit
			}
		}

		private static void ResultCompleted(object? sender, SearchResultEventArgs eventArgs)
		{
			var result = eventArgs.Result;

			ResultOptions.Add(new NConsoleOptionBasic
			{
				Function = () =>
				{
					var primaryResult = result.PrimaryResult;

					if (primaryResult is { } && primaryResult.Url != null) {
						Network.OpenUrl(primaryResult.Url.ToString());
					}

					return null;
				},
				//Name = result.Engine.Name,
				Data = result.ToString()
			});

			NConsole.Refresh();
		}
	}
}