// ReSharper disable SuggestVarOrType_BuiltInTypes
// ReSharper disable AssignNullToNotNullAttribute
// ReSharper disable ConvertSwitchStatementToSwitchExpression
// ReSharper disable UnusedParameter.Local
// ReSharper disable RedundantUsingDirective

#pragma warning disable IDE0079
#pragma warning disable CS0168
#pragma warning disable IDE0060
#pragma warning disable CA1825
#pragma warning disable IDE0008
#pragma warning restore CA1416
#nullable disable

using SmartImage.Core;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Media;
using System.Net.NetworkInformation;
using System.Resources;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using System.Text.Unicode;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.Networking.Connectivity;
using Windows.UI.Notifications;
using Kantan.Cli;
using Kantan.Cli.Controls;
using Kantan.Diagnostics;
using Kantan.Model;
using Kantan.Net;
using Kantan.Text;
using Kantan.Utilities;
using Microsoft.Toolkit.Uwp.Notifications;
using Novus;
using Novus.Win32;
using Novus.Win32.Structures;
using SmartImage.Lib;
using SmartImage.Lib.Engines;
using SmartImage.Lib.Searching;
using SmartImage.Lib.Utilities;
using SmartImage.UI;
using SmartImage.Utilities;
using static SmartImage.UI.AppInterface;

// ReSharper disable SuggestVarOrType_Elsewhere
// ReSharper disable PossibleNullReferenceException
// ReSharper disable AsyncVoidLambda
// ReSharper disable ConditionIsAlwaysTrueOrFalse
// ReSharper disable ArrangeObjectCreationWhenTypeNotEvident
// ReSharper disable CognitiveComplexity

namespace SmartImage;
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
	internal static readonly ConsoleDialog ResultDialog = new()
	{
		Options = new List<ConsoleOption>(),
		Description = "Press the result number to open in browser\n".AddColor(Elements.ColorOther) +
		              $"{"Ctrl:".AddColor(Elements.ColorKey)} Load direct | " +
		              $"{"Alt:".AddColor(Elements.ColorKey)} Show other | " +
		              $"{"Shift:".AddColor(Elements.ColorKey)} Open raw | " +
		              $"{"Alt+Ctrl:".AddColor(Elements.ColorKey)} Download\n" +
		              $"{"F1:".AddColor(Elements.ColorKey)} Show filtered results | " +
		              $"{"F2:".AddColor(Elements.ColorKey)} Refine | " +
		              $"{"F5:".AddColor(Elements.ColorKey)} Refresh",

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

				ResultDialog.Options.Add(_originalResult);

				foreach (ConsoleOption option in buffer.Select(ConsoleUIFactory.CreateResultOption)) {
					ResultDialog.Options.Add(option);
				}

				_isFilteredShown = !_isFilteredShown;

				ResultDialog.Refresh();
			},
			[ConsoleKey.F2] = async () =>
			{
				// F2 : Refine

				_cancellationToken = new();
				var buf = new List<ConsoleOption>(ResultDialog.Options);

				ResultDialog.Options.Clear();
				ResultDialog.Options.Add(_originalResult);

				try {
					await Client.RefineSearchAsync();
				}
				catch (Exception e) {
					Console.WriteLine(
						$"\n{Strings.Constants.CHEVRON} Error: {e.Message.AddColor(Elements.ColorError)}");

					ConsoleManager.WaitForTimeSpan(TimeSpan.FromSeconds(2));

					ResultDialog.Options.Clear();
					// ResultDialog.Options.Add(_originalResult);

					foreach (ConsoleOption t in buf) {
						ResultDialog.Options.Add(t);
					}

				}

				ResultDialog.Refresh();
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


		ToastNotificationManagerCompat.OnActivated += AppToast.OnToastActivated;
		Console.OutputEncoding                     =  Encoding.Unicode;

		Console.Title = $"{AppInfo.NAME}";

		ConsoleManager.Init();
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
		                        $"| Filtering: {Elements.ToToggleString(Config.Filtering)}";


		_cancellationToken = new();

		// Run search

		Client.ResultCompleted += OnResultCompleted;

		Client.SearchCompleted += (obj, eventArgs) =>
		{
			OnSearchCompleted(obj, eventArgs, _cancellationToken);

			if (Config.Notification) {
				AppToast.ShowToast(obj, eventArgs);
			}
		};

		ThreadPool.QueueUserWorkItem(_ =>
		{
			//todo: this is stupid
			while (!(Client.Pending2 <= 0&&Client.IsComplete)) { }

			Client.Dispose();
			Client.Reset();

			GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
			GC.Collect(2, GCCollectionMode.Forced);
			Debug.WriteLine("done");
		});

		Client.ResultUpdated += (sender, result) =>
		{
			ResultDialog.Refresh();
		};


		CPI.Start(_cancellationToken);


		// Show results
		var searchTask = Client.RunSearchAsync();

		_originalResult = ConsoleUIFactory.CreateResultOption(Config.Query.GetImageResult(), "(Original image)",
		                                                      Elements.ColorMain, -0.1f);

		// Add original image
		ResultDialog.Options.Add(_originalResult);

		if (!Config.OutputOnly) {
			await ResultDialog.ReadInputAsync();
		}

		await searchTask;

		if (Config.OutputOnly) {
			ResultDialog.Display(false);
		}
	}

	private static CancellationTokenSource _cancellationToken;

	private static bool _isFilteredShown;

	private static ConsoleOption _originalResult;

	#region CLI

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
				Function = delegate
				{
					Config.Filtering = true;
					return null;
				}
			},
			new()
			{
				ArgumentCount = 0,
				ParameterId   = "-output_only",
				Function = delegate
				{
					Config.OutputOnly = true;
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

	private static async Task<bool> HandleArguments()
	{
		var args = Environment.GetCommandLineArgs();

		// first element is executing assembly
		args = args.Skip(1).ToArray();

		if (!args.Any()) {
			var options = await MainMenuDialog.ReadInputAsync();

			var file = options.DragAndDrop;

			if (file != null) {
				Debug.WriteLine($"Drag and drop: {file}");
				Console.WriteLine($">> {file}".AddColor(Elements.ColorMain));
				Config.Query = file;
				return true;
			}

			if (!options.Output.Any()) {
				return false;
			}
		}
		else {

			/*
			 * Handle CLI args
			 */

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

	#endregion


	#region Event handlers
	


	private static void OnSearchCompleted(object sender, SearchCompletedEventArgs eventArgs,
	                                      CancellationTokenSource cts)
	{


		Debug.WriteLine("Search completed");

		// GC.Collect();

		Native.FlashConsoleWindow();

		cts.Cancel();
		cts.Dispose();

		// SystemSounds.Exclamation.Play();

		ResultDialog.Refresh();

		if (Config.PriorityEngines == SearchEngineOptions.Auto) {
			var m = Client.Results.OrderByDescending(x => x.PrimaryResult.Similarity);

			WebUtilities.OpenUrl(m.First().PrimaryResult.Url.ToString());
		}


	}

	private static void OnResultCompleted(object sender, ResultCompletedEventArgs eventArgs)
	{
		SearchResult result = eventArgs.Result;

		ConsoleOption option = ConsoleUIFactory.CreateResultOption(result);

		bool? isFiltered = eventArgs.IsFiltered;

		if (isFiltered.HasValue && !isFiltered.Value || !isFiltered.HasValue) {
			ResultDialog.Options.Add(option);
		}

		if (eventArgs.IsPriority) {
			option.Function();
		}

		var status = $"Results: {Client.Results.Count}";

		if (Config.Filtering) {
			status += $" | Filtered: {Client.FilteredResults.Count}";
		}

		status += $" | Pending: {Client.Pending}";

		ResultDialog.Status = status;

		GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
		GC.Collect();

	}

	#endregion
}