// ReSharper disable SuggestVarOrType_BuiltInTypes
// ReSharper disable AssignNullToNotNullAttribute
// ReSharper disable ConvertSwitchStatementToSwitchExpression
// ReSharper disable UnusedParameter.Local
// ReSharper disable RedundantUsingDirective

#pragma warning disable IDE0079
#pragma warning disable CS0168
#pragma warning disable IDE0060
#pragma warning disable CA1825
#nullable disable

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
using Windows.Networking.Connectivity;
using Windows.UI.Notifications;
using Kantan.Cli;
using Kantan.Diagnostics;
using Kantan.Net;
using Kantan.Utilities;
using Microsoft.Toolkit.Uwp.Notifications;
using Novus.Win32;
using Novus.Win32.Structures;
using SmartImage.Lib;
using SmartImage.Lib.Engines;
using SmartImage.Lib.Searching;
using SmartImage.Lib.Utilities;
using SmartImage.UI;
using SmartImage.Utilities;

// ReSharper disable AsyncVoidLambda

// ReSharper disable ConditionIsAlwaysTrueOrFalse

// ReSharper disable ArrangeObjectCreationWhenTypeNotEvident

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

		/// <summary>
		/// User search config
		/// </summary>
		internal static readonly SearchConfig Config = new();

		/// <summary>
		/// Search client
		/// </summary>
		internal static readonly SearchClient Client = new(Config);

		/// <summary>
		/// Console UI for search results
		/// </summary>
		private static readonly NConsoleDialog ResultDialog = new()
		{
			Options = new List<NConsoleOption>(),

			Description = "Press the result number to open in browser\n" +
			              "Ctrl: Load direct | Alt: Show other | Shift: Open raw | Alt+Ctrl: Download\n" +
			              "F1: Show filtered results | F2: Refine | F5: Refresh",

			Functions = new()
			{
				[ConsoleKey.F1] = () =>
				{
					// F1 : Show filtered

					ResultDialog.Options.Clear();

					var buffer = new List<SearchResult>();
					buffer.AddRange(Client.Results);

					if (!_isFilteredShown) {
						buffer.AddRange(Client.FilteredResults);
					}

					ResultDialog.Options.Add(_orig);

					foreach (NConsoleOption option in buffer.Select(NConsoleFactory.CreateResultOption)) {
						ResultDialog.Options.Add(option);
					}

					_isFilteredShown = !_isFilteredShown;

					NConsole.Refresh();
				},
				[ConsoleKey.F2] = async () =>
				{
					// F2 : Refine

					_cancellationToken = new();
					ResultDialog.Options.Clear();
					ResultDialog.Options.Add(_orig);

					try {
						await Client.RefineSearchAsync();
					}
					catch (Exception e) {
						Console.WriteLine("Error: {0}",e.Message);
						NConsole.WaitForSecond();
					}

					NConsole.Refresh();
				},
			}
		};

		#endregion

		/// <summary>
		/// Entry point
		/// </summary>
		private static async Task Main(string[] args)
		{
			/*
			 * Setup
			 * Check compatibility
			 * Register events
			 */

			ToastNotificationManagerCompat.OnActivated += AppInterface.OnToastActivated;

			//...
			Native.SetConsoleOutputCP(Native.CP_IBM437);

			Console.Title = $"{AppInfo.NAME}";

			//120,30
			//Console.WindowHeight = 60;

			NConsole.Init();
			Console.Clear();

			Console.CancelKeyPress += (sender, eventArgs) => { };

			var process = Process.GetCurrentProcess();
			process.PriorityClass = ProcessPriorityClass.AboveNormal;

			/*
			 * Start
			 */

			/*
			 * Configuration precedence
			 *
			 * 1. Config file
			 * 2. Cli arguments
			 *
			 * Cli arguments override config file
			 */

			AppConfig.ReadConfigFile();

			if (!await HandleArguments())
				return;

			ResultDialog.Subtitle = $"SE: {Config.SearchEngines} " +
			                        $"| PE: {Config.PriorityEngines} " +
			                        $"| Filtering: {AppInterface.Elements.ToToggleString(Config.Filtering)}";

			_cancellationToken = new();

			// Run search

			Client.ResultCompleted += OnResultCompleted;

			Client.SearchCompleted += (obj, eventArgs) =>
			{
				OnSearchCompleted(obj, eventArgs, _cancellationToken);

				if (Config.Notification) {
					AppInterface.ShowToast(obj, eventArgs);
				}
			};

			NConsoleProgress.Queue(_cancellationToken);

			// Show results
			var searchTask = Client.RunSearchAsync();

			_orig = NConsoleFactory.CreateResultOption(Config.Query.GetImageResult(), "(Original image)",
			                                           AppInterface.Elements.ColorMain, -0.1f);

			// Add original image
			ResultDialog.Options.Add(_orig);


			await ResultDialog.ReadAsync();

			await searchTask;
		}

		private static async Task<bool> HandleArguments()
		{
			var args = Environment.GetCommandLineArgs();

			// first element is executing assembly
			args = args.Skip(1).ToArray();

			if (!args.Any()) {
				var options = await AppInterface.MainMenuDialog.ReadAsync();

				if (!options.Any()) {
					return false;
				}

				if (options.First() is string file) {
					Debug.WriteLine($"Drag and drop: {file}");
					Console.WriteLine($">> {file}".AddColor(AppInterface.Elements.ColorMain));
					Config.Query = file;
				}
			}
			else {

				/*
				 * Handle CLI args
				 */

				//-pe SauceNao,Iqdb -se All -f "C:\Users\Deci\Pictures\Test Images\Test6.jpg"

				try {

					CliHandler.Run(args);

					Client.Reload();
				}
				catch (Exception e) {
					Console.WriteLine($"Error: {e.Message}");
					return false;
				}
			}

			return true;
		}

		private static CancellationTokenSource _cancellationToken;

		private static bool _isFilteredShown;

		private static NConsoleOption _orig;

		#region Event handlers

		private static void OnSearchCompleted(object sender, SearchCompletedEventArgs eventArgs,
		                                      CancellationTokenSource cts)
		{
			Native.FlashConsoleWindow();

			cts.Cancel();
			cts.Dispose();

			SystemSounds.Exclamation.Play();
			NConsole.Refresh();

			if (Config.PriorityEngines == SearchEngineOptions.Auto) {
				var m = Client.Results.OrderByDescending(x => x.PrimaryResult.Similarity);

				WebUtilities.OpenUrl(m.First().PrimaryResult.Url.ToString());
			}
			//ResultDialog.Status += $" | {Client.ShouldRefine}";

		}

		private static void OnResultCompleted(object sender, ResultCompletedEventArgs eventArgs)
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

			var s = $"Results: {Client.Results.Count}";

			if (Config.Filtering) {
				s += $" | Filtered: {Client.FilteredResults.Count}";
			}

			s += $" | Pending: {Client.Pending}";

			ResultDialog.Status = s;

		}

		#endregion

		/// <summary>
		/// Command line argument handler
		/// </summary>
		private static readonly CliHandler CliHandler = new()
		{
			Parameters =
			{
				new()
				{
					ArgumentCount = 1,
					ParameterId   = "-se",
					Function = strings =>
					{
						Config.SearchEngines = Enum.Parse<SearchEngineOptions>(strings[0]);
						return null;
					}
				},
				new()
				{
					ArgumentCount = 1,
					ParameterId   = "-pe",
					Function = strings =>
					{
						Config.PriorityEngines = Enum.Parse<SearchEngineOptions>(strings[0]);
						return null;
					}
				},
				new()
				{
					ArgumentCount = 0,
					ParameterId   = "-f",
					Function = strings =>
					{
						Config.Filtering = true;
						return null;
					}
				}
			},
			Default = new CliParameter
			{
				ArgumentCount = 1,
				ParameterId   = null,
				Function = strings =>
				{
					Config.Query = strings[0];
					return null;
				}
			}
		};
	}
}