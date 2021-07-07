// ReSharper disable RedundantUsingDirective

#pragma warning disable CS0168
#nullable enable
using SimpleCore.Cli;
using SmartImage.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Media;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Notifications;
using Microsoft.Toolkit.Uwp.Notifications;
using Novus.Win32;
using Novus.Win32.Structures;
using SimpleCore.Net;
using SimpleCore.Utilities;
using SmartImage.Lib;
using SmartImage.Lib.Engines;
using SmartImage.Lib.Searching;
using SmartImage.Lib.Utilities;
using SmartImage.Utilities;
// ReSharper disable SuggestVarOrType_BuiltInTypes

// ReSharper disable AssignNullToNotNullAttribute

// ReSharper disable ConvertSwitchStatementToSwitchExpression

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


		public static readonly SearchConfig Config = new();

		public static readonly SearchClient Client = new(Config);

		public static readonly NConsoleDialog ResultDialog = new()
		{
			Options     = new List<NConsoleOption>(),
			Description = AppInterface.Description
		};

		/// <summary>
		/// Entry point
		/// </summary>
		private static async Task Main(string[] args)
		{
#if DEBUG
			if (!args.Any()) {
				//args = new[] {"find-direct", "https://danbooru.donmai.us/posts/3987008"};
			}


#endif

			/*
			 * Setup
			 * Check compatibility
			 */

			Native.SetConsoleOutputCP(Native.CP_WIN32_UNITED_STATES);


			Console.Title = $"{AppInfo.NAME}";

			NConsole.Init();
			Console.Clear();

			Console.CancelKeyPress += (sender, eventArgs) => { };


			var process = Process.GetCurrentProcess();
			process.PriorityClass = ProcessPriorityClass.AboveNormal;


			/*
			 * Start
			 */

			// Update

			LocalConfig.ReadConfigFile();

			if (!args.Any()) {
				var options = NConsole.ReadOptions(AppInterface.MainMenuDialog);


				if (!options.Any()) {
					return;
				}
			}
			else {

				// TODO: WIP

				var enumerator = args.GetEnumerator();

				while (enumerator.MoveNext()) {
					object? arg = enumerator.Current;

					switch (arg) {
						case "find-direct":
							enumerator.MoveNext();
							var argValue = (string) enumerator.Current;

							var directImages = ImageHelper.FindDirectImages(argValue, out var images);
							var imageResults = new List<ImageResult>();

							for (int i = 0; i < directImages.Count; i++) {
								string directUrl = directImages[i];

								var ir = new ImageResult
								{
									Image  = images[i],
									Url    = new Uri(directUrl),
									Direct = new Uri(directUrl)
								};

								ir.UpdateImageData();

								imageResults.Add(ir);
							}


							var options = imageResults
							              .Select(r => AppInterface.CreateOption(r, $"Image", AppInterface.ColorOther))
							              .ToArray();


							NConsole.ReadOptions(new NConsoleDialog
							{
								Options     = options,
								Description = AppInterface.Description
							});

							return;
						default:
							Config.Query = args[0];
							break;
					}
				}
			}

			try {

				CancellationTokenSource cts = new();


				// Run search

				Client.ResultCompleted += OnResultCompleted;

				Client.SearchCompleted += (sender, eventArgs) =>
				{
					AppInterface.FlashConsoleWindow();
					//SystemSounds.Exclamation.Play();

					cts.Cancel();
					cts.Dispose();

					if (Config.Notification) {
						AppInterface.ShowToast();
					}
				};

				NConsoleProgress.Queue(cts);

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


		private static void OnResultCompleted(object? sender, SearchResultEventArgs eventArgs)
		{
			var result = eventArgs.Result;

			var option = AppInterface.CreateOption(result);

			bool? isFiltered = eventArgs.IsFiltered;

			if (isFiltered.HasValue && !isFiltered.Value || !isFiltered.HasValue) {
				ResultDialog.Options.Add(option);
			}

			if (eventArgs.IsPriority) {
				option.Function();
			}

			ResultDialog.Status = $"Results: {Client.Results.Count} "            +
			                      $"| Filtered: {Client.FilteredResults.Count} " +
			                      $"| Filtering: {Config.Filtering.ToToggleString()}";

			NConsole.Refresh();
		}
	}
}