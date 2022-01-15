﻿// ReSharper disable SuggestVarOrType_BuiltInTypes
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
using Kantan.Collections;
using Kantan.Diagnostics;
using Kantan.Net;
using Kantan.Text;
using Kantan.Utilities;
using Microsoft.Toolkit.Uwp.Notifications;
using Microsoft.VisualBasic.FileIO;
using Novus.OS.Win32;
using SmartImage.Lib;
using SmartImage.Lib.Engines;
using SmartImage.Lib.Searching;
using SmartImage.Properties;
using SmartImage.UI;
using static SmartImage.UI.AppInterface;
using Configuration = System.Configuration.Configuration;
using EH = Kantan.Collections.EnumerableHelper;
using E = SmartImage.UI.AppInterface.Elements;

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

public static class Program
{
	#region Core fields

	/// <summary>
	/// User search config
	/// </summary>
	internal static SearchConfig Config { get; } = new();

	/// <summary>
	/// Search client
	/// </summary>
	internal static SearchClient Client { get; } = new(Config);


	/// <summary>
	/// Console UI for search results
	/// </summary>
	private static ConsoleDialog ResultDialog { get; } = new()
	{
		Options = new List<ConsoleOption>(),


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
					string s = $"Error: {e.Message.AddColor(E.ColorError)}";

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

#if TEST
		args = new[] { "", @"C:\Users\Deci\Pictures\Test Images\Test1.jpg" };
#endif




		ResultDialog.AddDescription("Press the result number to open in browser", E.ColorOther)
		            .AddDescription(EH.ReadCsv(Resources.D_ModifierKeys), E.ColorKey)
		            .AddDescription(EH.ReadCsv(Resources.D_FuncKeys), E.ColorKey);

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


		if (!await HandleStartup(args))
			return;

		ConsoleManager.BufferLimit += 10;

		ResultDialog.AddDescription(new Dictionary<string, string>()
		{
			[Resources.D_SE]     = Config.SearchEngines.ToString(),
			[Resources.D_PE]     = Config.PriorityEngines.ToString(),
			[Resources.D_Filter] = E.GetToggleString(Config.Filtering),
		}, E.ColorKey2);

		ResultDialog.AddDescription(new Dictionary<string, string>()
		{
			[Resources.D_NI] = E.GetToggleString(Config.NotificationImage),
			[Resources.D_N]  = E.GetToggleString(Config.Notification),
		}, E.ColorKey2);


		_ctsSearch    = new();
		_ctsContinue  = new();
		_ctsReadInput = new();
		_ctsProgress  = new();

		// Run search

		Client.ResultCompleted       += OnResultCompleted;
		Client.SearchCompleted       += OnSearchCompleted;
		Client.DirectResultCompleted += OnDirectResultCompleted;

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

		Client.Dispose();
		Client.Reset();

		Console.ReadKey(true);
	}

	private static async Task<bool> HandleStartup(string[] args)
	{

#if !TEST
		args = Environment.GetCommandLineArgs();

		// first element is executing assembly
		args = args.Skip(1).ToArray();
#endif

		
		if (!args.Any())
		{
			var options = await AppInterface.MainMenuDialog.ReadInputAsync();

			var file = options.DragAndDrop;

			if (file != null)
			{
				Debug.WriteLine($"Drag and drop: {file}");
				Console.WriteLine($">> {file}".AddColor(AppInterface.Elements.ColorMain));
				Config.Query = file;
				return true;
			}

			if (!options.Output.Any())
			{
				return false;
			}
		}
		else
		{

			/*
	 * Handle CLI args
	 */

			try
			{

				Cli.ArgumentHandler.Run(args);

				Client.Reload();
			}
			catch (Exception e)
			{
				Console.WriteLine($"Error: {e.Message}");
				return false;
			}
		}


		return true;
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

		var color = E.EngineColorMap[result.Engine.EngineOption];
		option.Color    = color;
		option.ColorAlt = color.ChangeBrightness(-.4f);

		bool? isFiltered = eventArgs.IsFiltered;

		if (isFiltered.HasValue && !isFiltered.Value || !isFiltered.HasValue) {
			ResultDialog.Options.Add(option);
		}

		if (eventArgs.IsPriority) {
			option.Function();
		}

		var map = new Dictionary<string, string>
		{
			["Results"] = Client.Results.Count.ToString(),
		};

		if (Config.Filtering) {
			map.Add("Filtered", Client.FilteredResults.Count.ToString());
		}

		map.Add("Pending", Client.PendingCount.ToString());

		string status;

		if (_ctsSearch.IsCancellationRequested) {
			status = "Cancelled";
		}
		else if (Client.IsComplete) {
			status = "Complete";
		}
		else {
			status = "Searching";
		}

		map.Add("Status", status);

		ResultDialog.Status = Strings.GetMapString(map);
	}

	#endregion

	#region Other fields

	private static CancellationTokenSource _ctsProgress;
	private static CancellationTokenSource _ctsReadInput;
	private static CancellationTokenSource _ctsContinue;
	private static CancellationTokenSource _ctsSearch;

	private static bool _isFilteredShown;

	private static ConsoleOption _originalResult;

	private static Task _searchTask;
	private static Task _continueTask;

	#endregion

	internal static void Reload() => Client.Reload();

	private static class Cli
	{
		/// <summary>
		/// Command line argument handler
		/// </summary>
		internal static readonly CliHandler ArgumentHandler = new()
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
			Default = new()
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