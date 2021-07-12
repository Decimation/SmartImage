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

// ReSharper disable CognitiveComplexity

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

		#region Core fields

		public static readonly SearchConfig Config = new();

		public static readonly SearchClient Client = new(Config);

		public static readonly NConsoleDialog ResultDialog = new()
		{
			Options     = new List<NConsoleOption>(),
			Description = AppInterface.Description
		};

		#endregion

		/// <summary>
		/// Entry point
		/// </summary>
		private static async Task Main(string[] args)
		{
#if TEST_DEBUG
			if (!args.Any()) {
				//args = new string[] {CMD_SEARCH, "https://i.imgur.com/QtCausw.png"};
				args = new[] {CMD_FIND_DIRECT, "https://twitter.com/sciamano240/status/1186775807655587841"};
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


			AppConfig.ReadConfigFile();

			if (!args.Any()) {
				var options = NConsole.ReadOptions(AppInterface.MainMenuDialog);

				if (!options.Any()) {
					return;
				}
			}
			else {

				/*
				 * Handle CLI args
				 */

				var argEnumerator = args.GetEnumerator();

				while (argEnumerator.MoveNext()) {
					object? arg = argEnumerator.Current;

					switch (arg) {
						case CMD_FIND_DIRECT:
							argEnumerator.MoveNext();

							var directImages  = ImageHelper.FindDirectImages((string) argEnumerator.Current);
							var imageResults  = directImages.Select(ImageResult.FromDirectImage);
							var directOptions = AppInterface.CreateResultOptions(imageResults, "Image");


							NConsole.ReadOptions(new NConsoleDialog
							{
								Options     = directOptions,
								Description = AppInterface.Description
							});

							return;
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

			try {

				CancellationTokenSource cts = new();


				// Run search

				Client.ResultCompleted += OnResultCompleted;

				Client.SearchCompleted += (obj, eventArgs) =>
				{
					OnSearchCompleted(obj, eventArgs, cts);
				};

				NConsoleProgress.Queue(cts);

				// Show results
				var searchTask = Client.RunSearchAsync();

				// Add original image
				ResultDialog.Options.Add(AppInterface.CreateResultOption(
					                         Config.Query.GetImageResult(), "(Original image)",
					                         AppInterface.ColorMain, -0.1f));


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

		private static void OnSearchCompleted(object? sender, EventArgs eventArgs, CancellationTokenSource cts)
		{
			AppInterface.FlashConsoleWindow();

			cts.Cancel();
			cts.Dispose();

			if (Config.Notification) {
				AppInterface.ShowToast();
			}
			else {
				SystemSounds.Exclamation.Play();
			}
		}


		private static void OnResultCompleted(object? sender, SearchResultEventArgs eventArgs)
		{
			var result = eventArgs.Result;

			var option = AppInterface.CreateResultOption(result);

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