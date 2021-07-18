// ReSharper disable SuggestVarOrType_BuiltInTypes
// ReSharper disable AssignNullToNotNullAttribute
// ReSharper disable ConvertSwitchStatementToSwitchExpression
// ReSharper disable UnusedParameter.Local
// ReSharper disable RedundantUsingDirective

#pragma warning disable IDE0079
#pragma warning disable CS0168
#pragma warning disable IDE0060
#pragma warning disable CA1825
#nullable enable

using SmartImage.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Media;
using System.Net.NetworkInformation;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Unicode;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.UI.Notifications;
using Kantan.Cli;
using Microsoft.Toolkit.Uwp.Notifications;
using Novus.Win32;
using Novus.Win32.Structures;
using SmartImage.Lib;
using SmartImage.Lib.Engines;
using SmartImage.Lib.Searching;
using SmartImage.Lib.Utilities;
using SmartImage.UI;
using SmartImage.Utilities;

// ReSharper disable CognitiveComplexity

namespace SmartImage
{
	//  ____                       _   ___
	// / ___| _ __ ___   __ _ _ __| |_|_ _|_ __ ___   __ _  __ _  ___
	// \___ \| '_ ` _ \ / _` | '__| __|| || '_ ` _ \ / _` |/ _` |/ _ \
	//  ___) | | | | | | (_| | |  | |_ | || | | | | | (_| | (_| |  __/
	// |____/|_| |_| |_|\__,_|_|   \__|___|_| |_| |_|\__,_|\__, |\___|
	//                                                     |___/

	public static class Program
	{
		#region Core fields

		public static readonly SearchConfig Config = new();

		public static readonly SearchClient Client = new(Config);

		public static readonly NConsoleDialog ResultDialog = new()
		{
			Options     = new List<NConsoleOption>(),
			Description = Elements.Description
		};

		#endregion

		/// <summary>
		/// Entry point
		/// </summary>
		private static async Task Main(string[] args)
		{
#if DEBUG
			if (!args.Any()) {
				//args = new string[] {CMD_SEARCH, "https://i.imgur.com/QtCausw.png"};
				//args = new[] {CMD_FIND_DIRECT, "https://twitter.com/sciamano240/status/1186775807655587841"};
			}


#endif

			/*
			 * Setup
			 * Check compatibility
			 * Register events
			 */

			ToastNotificationManagerCompat.OnActivated += AppInterface.OnToastActivated;

			Native.SetConsoleOutputCP(Native.CP_IBM437);

			Console.Title = $"{AppInfo.NAME}";

			NConsole.Init();
			Console.Clear();

			Console.CancelKeyPress += (sender, eventArgs) => { };

			var process = Process.GetCurrentProcess();
			process.PriorityClass = ProcessPriorityClass.AboveNormal;


			/*
			 * Start
			 */


			AppConfig.ReadConfigFile();


			if (HandleArguments(args))
				return;


			try {

				CancellationTokenSource cts = new();


				// Run search

				Client.ResultCompleted += OnResultCompleted;

				Client.SearchCompleted += (obj, eventArgs) => OnSearchCompleted(obj, eventArgs, cts);

				Client.ExtraResults += AppInterface.ShowToast;

				NConsoleProgress.Queue(cts);

				// Show results
				var searchTask = Client.RunSearchAsync();
				
				// Add original image
				ResultDialog.Options.Add(NConsoleFactory.CreateResultOption(
					                         Config.Query.GetImageResult(), "(Original image)",
					                         Elements.ColorMain, -0.1f));


				NConsole.ReadOptions(ResultDialog);

				await searchTask;
			}
			catch (Exception exception) {
#if !DEBUG
				// ...
#else
				Console.WriteLine(exception);
#endif
			}
		}

		private static bool HandleArguments(string[] args)
		{
			if (!args.Any()) {
				HashSet<object> options = NConsole.ReadOptions(AppInterface.MainMenuDialog);

				if (!options.Any()) {
					return true;
				}
			}
			else {

				/*
				 * Handle CLI args
				 */

				try {

					var argEnumerator = args.GetEnumerator();

					while (argEnumerator.MoveNext()) {
						object? arg = argEnumerator.Current;

						switch (arg) {
							case CMD_FIND_DIRECT:
								argEnumerator.MoveNext();

								var directImages = ImageHelper.FindDirectImages((string) argEnumerator.Current);

								var imageResults = directImages.Select(ImageResult.FromDirectImage);

								var directOptions = NConsoleFactory.CreateResultOptions(imageResults, "Image");


								NConsole.ReadOptions(new NConsoleDialog
								{
									Options     = directOptions,
									Description = Elements.Description
								});

								return true;
							case CMD_SEARCH:
								argEnumerator.MoveNext();
								Config.Query = (string) argEnumerator.Current;
								break;
							default:
								Config.Query = args.First();
								break;
						}
					}
				}
				catch (Exception e) {
					Console.WriteLine(e);
					Console.ReadLine();
				}
			}

			return false;
		}


		private static void OnSearchCompleted(object? sender, List<SearchResult> eventArgs, CancellationTokenSource cts)
		{
			Native.FlashConsoleWindow();

			cts.Cancel();
			cts.Dispose();

			SystemSounds.Exclamation.Play();

		}


		private static void OnResultCompleted(object? sender, SearchResultEventArgs eventArgs)
		{
			var result = eventArgs.Result;

			var option = NConsoleFactory.CreateResultOption(result);

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

		#region Commands

		private const string CMD_FIND_DIRECT = "find-direct";

		private const string CMD_SEARCH = "search";

		#endregion
	}
}