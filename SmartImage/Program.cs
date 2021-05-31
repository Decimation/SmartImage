// ReSharper disable RedundantUsingDirective


#nullable enable
using SimpleCore.Cli;
using SmartImage.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Media;
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

namespace SmartImage
{
	public static class Program
	{
		//  ____                       _   ___
		// / ___| _ __ ___   __ _ _ __| |_|_ _|_ __ ___   __ _  __ _  ___
		// \___ \| '_ ` _ \ / _` | '__| __|| || '_ ` _ \ / _` |/ _` |/ _ \
		//  ___) | | | | | | (_| | |  | |_ | || | | | | | (_| | (_| |  __/
		// |____/|_| |_| |_|\__,_|_|   \__|___|_| |_| |_|\__,_|\__, |\___|
		//                                                     |___/


		private static readonly NConsoleDialog ResultDialog = new()
		{
			Options = new List<NConsoleOption>()
		};

		public static readonly SearchConfig Config = new();

		public static readonly SearchClient Client = new(Config);


		/*
		 * Entry point
		 */

		private static async Task Main(string[] args)
		{
			/*
			 * Setup
			 * Check compatibility
			 */


			Console.Title = Info.NAME;

			NConsole.Init();

			Console.CancelKeyPress += (sender, eventArgs) => { };


			NConsole.ReadOptions(MainDialog.MainMenuDialog);


			/*
			 * Run search
			 */

			try {


				// Run search
				ImageQuery query = NConsole.ReadInput("Image file or direct URL", x =>
				{
					(bool url, bool file) = ImageQuery.IsUriOrFile(x);
					return !(url || file);
				});

				Config.Query = query;

				Client.ResultCompleted += ResultCompleted;
				Client.SearchCompleted += SearchCompleted;

				// Show results

				var searchTask = Client.RunSearchAsync();
				NConsole.ReadOptions(ResultDialog);

				await searchTask;
			}
			catch (Exception exception) {
#if !DEBUG
#else
				Console.WriteLine(exception);
#endif
			}
		}

		private static void SearchCompleted(object? sender, EventArgs eventArgs)
		{
			NativeImports.FlashConsoleWindow();
			SystemSounds.Exclamation.Play();
		}

		private static NConsoleOption Convert(SearchResult result)
		{
			var option = new NConsoleOption
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
			};

			return option;
		}

		private static void ResultCompleted(object? sender, SearchResultEventArgs eventArgs)
		{
			var result = eventArgs.Result;

			var option = Convert(result);

			ResultDialog.Options.Add(option);

			if (Config.PriorityEngines.HasFlag(result.Engine.Engine)) {
				option.Function();
			}

			NConsole.Refresh();
		}
	}
}