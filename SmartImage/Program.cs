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
using System.Configuration;
using System.Diagnostics;
using System.Drawing;
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
using Configuration = System.Configuration.Configuration;

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
	internal static SearchConfig Config { get; set; } = new();

	/// <summary>
	/// Search client
	/// </summary>
	internal static SearchClient Client { get; private set; } = new(Config);

	private static Dictionary<string, string> desc = new()
	{
		["Ctrl"]     = "Load direct",
		["Alt"]      = "Show other",
		["Shift"]    = "Open raw",
		["Alt+Ctrl"] = "Download",

	};

	private static Dictionary<string, string> desc2 = new()
	{

		["F1"] = "Show filtered results",
		["F2"] = "Refine",
		["F5"] = "Refresh"
	};

	/// <summary>
	/// Console UI for search results
	/// </summary>
	internal static ConsoleDialog ResultDialog { get; private set; } = new()
	{
		Options = new List<ConsoleOption>(),

		Description = "Press the result number to open in browser\n".AddColor(Elements.ColorOther)
		              + Strings.GetMapString(desc, Elements.ColorKey) + '\n'
		              + Strings.GetMapString(desc2, Elements.ColorKey),

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

				_ctsSearch = new();
				var buf = new List<ConsoleOption>(ResultDialog.Options);

				ResultDialog.Options.Clear();
				ResultDialog.Options.Add(_originalResult);

				try {
					await Client.RefineSearchAsync();
				}
				catch (Exception e) {
					string s = $"Error: {e.Message.AddColor(Elements.ColorError)}";

					Console.WriteLine(
						$"\n{Strings.Constants.CHEVRON} {s}");

					ConsoleManager.WaitForTimeSpan(TimeSpan.FromSeconds(2));

					ResultDialog.Options.Clear();

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

		Console.OutputEncoding = Encoding.Unicode;

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

		var map = new Dictionary<string, string>()
		{
			["SE"]        = Config.SearchEngines.ToString(),
			["PE"]        = Config.PriorityEngines.ToString(),
			["Filtering"] = Elements.GetToggleString(Config.Filtering)
		};

		ResultDialog.Subtitle = Strings.GetMapString(map);


		_ctsSearch    = new();
		_ctsContinue  = new();
		_ctsReadInput = new();
		_ctsProgress  = new();

		// Run search

		Client.ResultCompleted += OnResultCompleted;
		Client.SearchCompleted += OnSearchCompleted;
		Client.DirectResultCompleted   += OnDirectResultCompleted;

		Console.CancelKeyPress += OnCancel;

		ConsoleManager.UI.ProgressIndicator.Instance.Start(_ctsProgress);

		// Show results
		_searchTask   = Client.RunSearchAsync(_ctsSearch.Token);
		_continueTask = Client.RunContinueAsync(_ctsContinue.Token);

		_originalResult = Config.Query.GetConsoleOption();

		// Add original image
		ResultDialog.Options.Add(_originalResult);

		await ResultDialog.ReadInputAsync(_ctsReadInput.Token);


		await _searchTask;

		try {
			await _continueTask;
		}
		catch (Exception e) {
			//ignored
		}

		/*Client.Dispose();
		Client.Reset();
		*/
	}


	#region Event handlers

	private static void OnCancel(object sender, ConsoleCancelEventArgs eventArgs)
	{
		_ctsSearch.Cancel();
		_ctsContinue.Cancel();
		// _ctsReadInput.Cancel();

		eventArgs.Cancel = true;
		SystemSounds.Hand.Play();
	}

	private static void OnDirectResultCompleted(object sender, EventArgs result)
	{
		ResultDialog.Refresh();
	}

	private static void OnSearchCompleted(object sender, SearchCompletedEventArgs eventArgs)
	{
		Debug.WriteLine("Search completed");

		Native.FlashConsoleWindow();

		// SystemSounds.Exclamation.Play();
		_ctsProgress.Cancel();

		ResultDialog.Refresh();

		if (Config.PriorityEngines == SearchEngineOptions.Auto) {
			var m = Client.Results.OrderByDescending(x => x.PrimaryResult.Similarity);

			WebUtilities.OpenUrl(m.First().PrimaryResult.Url.ToString());
		}

		if (Config.Notification) {
			AppToast.ShowToast(sender, eventArgs);
		}

		var sp = new SoundPlayer(Resources.hint);
		sp.Play();
		sp.Dispose();
	}

	private static void OnResultCompleted(object sender, ResultCompletedEventArgs eventArgs)
	{
		SearchResult result = eventArgs.Result;

		ConsoleOption option = result.GetConsoleOption();

		var color = Elements.EngineColorMap[result.Engine.EngineOption];
		option.Color    = color;
		option.ColorAlt = color.ChangeBrightness(-.4f);

		bool? isFiltered = eventArgs.IsFiltered;

		if (isFiltered.HasValue && !isFiltered.Value || !isFiltered.HasValue) {
			ResultDialog.Options.Add(option);
		}

		if (eventArgs.IsPriority) {
			option.Function();
		}

		var map = new Dictionary<string, string>()
		{
			["Results"] = Client.Results.Count.ToString(),
		};

		if (Config.Filtering) {
			map.Add("Filtered", Client.FilteredResults.Count.ToString());
		}

		map.Add("Pending", Client.PendingCount.ToString());

		string s;

		if (_ctsSearch.IsCancellationRequested) {
			s = "Cancelled";
		}
		else if (Client.IsComplete) {
			s = "Complete";
		}
		else {
			s = "Searching";
		}

		map.Add("Status", s);

		var status = Strings.GetMapString(map);

		ResultDialog.Status = status;
	}

	#endregion

	private static CancellationTokenSource _ctsProgress;
	private static CancellationTokenSource _ctsReadInput;
	private static CancellationTokenSource _ctsContinue;
	private static CancellationTokenSource _ctsSearch;

	private static bool _isFilteredShown;

	private static ConsoleOption _originalResult;

	private static Task _searchTask;
	private static Task _continueTask;
}