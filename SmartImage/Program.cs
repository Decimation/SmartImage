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
using System.Buffers;
using System.Diagnostics;
using System.Media;
using System.Text;
using Kantan.Cli;
using Kantan.Cli.Controls;
using Kantan.Net;
using Kantan.Text;
using Kantan.Utilities;
using Microsoft.Toolkit.Uwp.Notifications;
using Novus.OS.Win32;
using SmartImage.Lib;
using SmartImage.Lib.Engines;
using SmartImage.Lib.Searching;
using SmartImage.Properties;
using SmartImage.UI;
using static SmartImage.UI.AppInterface;

// ReSharper disable AccessToDisposedClosure
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

public static partial class Program
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
	internal static ConsoleDialog ResultDialog = new()
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

				foreach (ConsoleOption option in buffer.Select(x => x.GetConsoleOption())) {
					ResultDialog.Options.Add(option);
				}

				_isFilteredShown = !_isFilteredShown;

				ResultDialog.Refresh();
			},
			[ConsoleKey.F2] = async () =>
			{
				// F2 : Refine

				_cancellationTokenSource = new();
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

		if (!await Cli.HandleArguments())
			return;

		ResultDialog.Subtitle = $"SE: {Config.SearchEngines} " +
		                        $"| PE: {Config.PriorityEngines} " +
		                        $"| Filtering: {Elements.ToToggleString(Config.Filtering)}";


		_cancellationTokenSource  = new();
		_cancellationTokenSource2 = new();
		_cancellationTokenSource3 = new();

		// Run search

		Client.ResultCompleted += OnResultCompleted;

		Client.SearchCompleted += (obj, eventArgs) =>
		{
			OnSearchCompleted(obj, eventArgs, _cancellationTokenSource);

			if (Config.Notification) {
				AppToast.ShowToast(obj, eventArgs);
			}

			var sp = new SoundPlayer(Resources.hint);
			sp.Play();
			sp.Dispose();
		};


		Client.ResultUpdated += (sender, result) =>
		{
			ResultDialog.Refresh();
		};

		Console.CancelKeyPress += (o, eventArgs) =>
		{
			OnHandler(o, eventArgs);

			SystemSounds.Hand.Play();

			// var x  = cd.ReadInputAsync();
			// x.Wait();
			// Debug.WriteLine($"{ResultDialog.Options.Count}|{Client.AllResults.Count}");
			// ResultDialog = cd;

			var results = Client.Results;
			
			var options = results.Select(x =>
			{
				return x.GetConsoleOption();
			}).ToArray();

			var cd = new ConsoleDialog()
			{
				Description = ResultDialog.Description,
				Header      = ResultDialog.Header,
				Options     = options,
				Subtitle    = ResultDialog.Subtitle,
			};
			cd.ReadInput();

			// await cd.ReadInputAsync();

		};

		ConsoleManager.UI.ProgressIndicator.Instance.Start(_cancellationTokenSource);


		// Show results
		_searchTask    = Client.RunSearchAsync(_cancellationTokenSource, _cancellationTokenSource2);
		_secondaryTask = Client.RunContinueAsync();


		_originalResult = Config.Query.GetConsoleOption();

		// Add original image
		ResultDialog.Options.Add(_originalResult);

		if (!Config.OutputOnly) {
			try {
				await ResultDialog.ReadInputAsync(_cancellationTokenSource3.Token);
			}
			catch (Exception e) {
				Debug.WriteLine($"{e.Message}");
			}

		}

		await _searchTask;

		try {
			await _secondaryTask;

		}
		catch (Exception e) {
			//ignored
		}

		Client.Dispose();
		Client.Reset();

		//todo
		Debug.WriteLine("done");

		if (Config.OutputOnly) {
			ResultDialog.Display(false);
		}


	}

	private static void OnHandler(object sender, ConsoleCancelEventArgs eventArgs)
	{

		/*ResultDialog.Options.Clear();
		ResultDialog.Options.Add(_originalResult);*/


		ResultDialog.Options.Clear();
		ResultDialog.Refresh();
		_cancellationTokenSource.Cancel();
		_cancellationTokenSource2.Cancel();
		_cancellationTokenSource3.Cancel();


		// _cancellationTokenSource = new();

		// ResultDialog.Refresh();
		// await ResultDialog.ReadInputAsync();
	}

	private static CancellationTokenSource _cancellationTokenSource3;

	private static CancellationTokenSource _cancellationTokenSource2;

	private static CancellationTokenSource _cancellationTokenSource;

	private static bool _isFilteredShown;

	private static ConsoleOption _originalResult;

	private static Task _searchTask;
	private static Task _secondaryTask;


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

		ConsoleOption option = result.GetConsoleOption();

		option.Color = Elements.EngineColorMap[result.Engine.EngineOption];
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

		status += $" | Pending: {Client.PendingCount}";

		ResultDialog.Status = status;

		/*GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
		GC.Collect();*/

	}

	#endregion
}