// ReSharper disable SuggestVarOrType_BuiltInTypes
// ReSharper disable AssignNullToNotNullAttribute
// ReSharper disable ConvertSwitchStatementToSwitchExpression
// ReSharper disable UnusedParameter.Local
// ReSharper disable RedundantUsingDirective
// ReSharper disable RedundantAssignment

#pragma warning disable IDE0079
#pragma warning disable CS0168
#pragma warning disable IDE0060
#pragma warning disable CA1825
#pragma warning disable IDE0008
#pragma warning restore CA1416
#nullable disable

global using static Kantan.Diagnostics.LogCategories;
using System.Buffers;
using System.Configuration;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Media;
using System.Text;
using Windows.Media.Playback;
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
using Novus.Utilities;
using SmartImage.App;
using SmartImage.Cli;
using SmartImage.Lib;
using SmartImage.Lib.Engines;
using SmartImage.Lib.Searching;
using SmartImage.Lib.Utilities;
using SmartImage.Properties;
using SmartImage.UI;
using SmartImage.Utilities;
using Configuration = System.Configuration.Configuration;
using EH = Kantan.Collections.EnumerableHelper;
using CPI = Kantan.Cli.ConsoleManager.UI.ProgressIndicator;

// ReSharper disable InlineTemporaryVariable

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
	internal static SearchConfig Config { get; } = new()
	{
		Folder = AppInfo.AppFolder,
	};

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
			/*[ConsoleKey.F2] = async () =>
			{
				// F2 : Refine

				tkSearch = new();
				var buf = new List<ConsoleOption>(ResultDialog.Options);

				ResultDialog.Options.Clear();
				ResultDialog.Options.Add(_originalResult);

				try {
					await Client.RefineSearchAsync();
				}
				catch (Exception e) {
					string s = $"Error: {e.Message.AddColor(UI.Elements.ColorError)}";

					Console.WriteLine(
						$"\n{Strings.Constants.CHEVRON} {s}");

					ConsoleManager.WaitForTimeSpan(TimeSpan.FromSeconds(2));

					ResultDialog.Options.Clear();

					foreach (ConsoleOption t in buf) {
						ResultDialog.Options.Add(t);
					}

				}

				ResultDialog.Refresh();
			},*/
			[ConsoleKey.F10] = () =>
			{
				if (!_keepOnTop) {
					Native.KeepWindowOnTop(WindowHandle);
				}
				else {
					Native.RemoveWindowOnTop(WindowHandle);
				}

				_keepOnTop = !_keepOnTop;
				ding.Play();
			}
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
		args = new[]
		{

			"",
			// @"https://litter.catbox.moe/zxvtym.jpg"
			// @"https://i.imgur.com/QtCausw.png"
			
			@"C:\Users\Deci\Downloads\maxresdefault.jpeg"
			// @"C:\Users\Deci\Pictures\Test Images\Test1.jpg"
		};

		Debug.WriteLine($"Configuration: TEST", C_INFO);

		Config.SearchEngines = SearchEngineOptions.TraceMoe;
#endif


		InitConsole();

		WindowHandle = Native.GetConsoleWindow();

		_keepOnTop = false;

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

		Config.Update();
		Reload();

		// Read config and arguments

		if (!await HandleStartup(args))
			return;

		BuildDescription();

		RegisterEvents();

		CPI.Instance.Start(TkProgress);

		// Show results

		// Run search

		_searchTask   = Client.RunSearchAsync(TkSearch.Token);
		_continueTask = Client.RunContinueAsync(TkContinue.Token);

		_originalResult = Config.Query.GetConsoleOption();

		// Add original image
		ResultDialog.Options.Add(_originalResult);

		await ResultDialog.ReadInputAsync(TkReadInput.Token);

		await _searchTask;

		try {
			await _continueTask;
		}
		catch (Exception e) {
			//ignored
		}

		Client.Dispose();
		Client.Reset();

		_ = Console.ReadKey(true);
	}

	private static void InitConsole()
	{
		Console.OutputEncoding = Encoding.Unicode;
		Console.Title          = $"{AppInfo.NAME}";

		ConsoleManager.BufferLimit += 10;
		ConsoleManager.Init();
		Console.Clear();
	}

	private static void RegisterEvents()
	{
		Client.ResultCompleted   += OnResultCompleted;
		Client.SearchCompleted   += OnSearchCompleted;
		Client.ContinueCompleted += OnContinueCompleted;

		ToastNotificationManagerCompat.OnActivated += AppToast.OnToastActivated;

		Console.CancelKeyPress += OnCancel;
	}

	private static void BuildDescription()
	{
		ResultDialog.AddDescription("Press the result number to open in browser", Elements.ColorOther)
		            .AddDescription(EH.ReadCsv(Resources.D_ModifierKeys), Elements.ColorKey)
		            .AddDescription(EH.ReadCsv(Resources.D_FuncKeys), Elements.ColorKey);

		ResultDialog.AddDescription(new Dictionary<string, string>
		{
			[Resources.D_SE]     = Config.SearchEngines.ToString(),
			[Resources.D_PE]     = Config.PriorityEngines.ToString(),
			[Resources.D_Filter] = Elements.GetToggleString(Config.Filtering),
		}, Elements.ColorKey2);

		ResultDialog.AddDescription(new Dictionary<string, string>
		{
			[Resources.D_NI] = Elements.GetToggleString(Config.NotificationImage),
			[Resources.D_N]  = Elements.GetToggleString(Config.Notification),
		}, Elements.ColorKey2);
	}

	/// <summary>
	/// Handles arguments and startup interaction
	/// </summary>
	private static async Task<bool> HandleStartup(string[] args)
	{

#if TEST
#else
		args = Environment.GetCommandLineArgs();

		// first element is executing assembly
		args = args.Skip(1).ToArray();
#endif


		Debug.WriteLine($"Args: {args.QuickJoin()}", C_DEBUG);

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

				CliArguments.ArgumentHandler.Run(args);

				Client.Reload();
			}
			catch (Exception e) {
				Console.WriteLine($"Error: {e.Message}");
				return false;
			}
		}


		return true;
	}

	#region Event handlers

	private static void OnCancel(object sender, ConsoleCancelEventArgs eventArgs)
	{
		TkSearch.Cancel();
		TkContinue.Cancel();
		// _ctsReadInput.Cancel();

		eventArgs.Cancel = true;
		SystemSounds.Hand.Play();
	}

	private static void OnContinueCompleted(object sender, EventArgs result)
	{
		ResultDialog.Refresh();
	}

	private static void OnSearchCompleted(object sender, SearchCompletedEventArgs eventArgs)
	{
		Debug.WriteLine("Search completed");

		Native.FlashWindow(WindowHandle);

		// SystemSounds.Exclamation.Play();
		TkProgress.Cancel();

		ResultDialog.Refresh();

		if (Config.PriorityEngines == SearchEngineOptions.Auto) {
			var m = Client.Results.OrderByDescending(x => x.PrimaryResult.Similarity);

			WebUtilities.OpenUrl(m.First().PrimaryResult.Url.ToString());
		}

		if (Config.Notification) {
			AppToast.ShowToast(sender, eventArgs);
		}

		hint.Play();

		GetStatus();

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

		GetStatus();
	}

	private static void GetStatus()
	{
		var map = new Dictionary<string, string>
		{
			["Results"] = Client.Results.Count.ToString(),
		};

		if (Config.Filtering) {
			map.Add("Filtered", Client.FilteredResults.Count.ToString());
		}

		map.Add("Pending", Client.PendingCount.ToString());

		string status;

		if (TkSearch.IsCancellationRequested) {
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

	internal static void Reload(bool saveCfg = false)
	{
		Client.Reload();

		if (saveCfg) {
			Config.Save();
		}
	}

	#endregion

	#region Resources

	private static readonly SoundPlayer ding = new(Resources.ding);
	private static readonly SoundPlayer hint = new(Resources.hint);

	#endregion

	#region CTS

	private static readonly CancellationTokenSource TkProgress  = new();
	private static readonly CancellationTokenSource TkReadInput = new();
	private static readonly CancellationTokenSource TkContinue  = new();
	private static readonly CancellationTokenSource TkSearch    = new();

	#endregion

	#region State

	private static bool _isFilteredShown;
	private static bool _keepOnTop;

	#endregion

	#region

	private static ConsoleOption _originalResult;

	private static Task _searchTask;
	private static Task _continueTask;

	internal static IntPtr WindowHandle { get; set; }

	#endregion

	private static readonly ConsoleOption[] MainMenuOptions =
	{
		new()
		{
			Name  = ">>> Run <<<",
			Color = Elements.ColorMain,
			Function = () =>
			{
				ImageQuery query = ConsoleManager.ReadLine("Image file or direct URL", x =>
				{
					x = x.CleanString();

					var m = ImageMedia.GetMediaInfo(x);
					return !((bool) m);
				}, "Input must be file or direct image link");

				Config.Query = query;
				return true;
			}
		},


		Controls.CreateOption<SearchEngineOptions>(nameof(Config.SearchEngines), "Engines", Config),

		Controls.CreateOption<SearchEngineOptions>(nameof(Config.PriorityEngines),
		                                           "Priority engines", Config),

		Controls.CreateOption(nameof(Config.Filtering), "Filter", Config),
		Controls.CreateOption(nameof(Config.Notification), "Notification", Config),
		Controls.CreateOption(nameof(Config.NotificationImage), "Notification image", Config),

		Controls.CreateOption(ReflectionOperatorHelpers.propertyof(() => AppIntegration.IsContextMenuAdded),
		                      "Context menu", added => AppIntegration.HandleContextMenu(!added), Config),

		new()
		{
			Name = "Config",
			Function = () =>
			{
				//Console.Clear();

				Console.WriteLine(Config);
				ConsoleManager.WaitForInput();

				return null;
			}
		},
		new()
		{
			Name = "Info",
			Function = () =>
			{
				//Console.Clear();

				var di = new DirectoryInfo(AppInfo.ExeLocation);

				var dependencies = ReflectionHelper.DumpDependencies();

				var ls = new List<string>()
				{
					$"Author: {Resources.Author}",
					$"Current version: {AppInfo.AppVersion} ({UpdateInfo.GetUpdateInfo().Status})",
					$"Latest version: {ReleaseInfo.GetLatestRelease()}",
					$"Executable location: {di.Parent.Name}",
					$"In path: {AppInfo.IsAppFolderInPath}",
					Strings.Constants.Separator
				};

				ls.AddRange(AppIntegration.UtilitiesMap.Select(x => $"{x.Key}: {x.Value}"));
				ls.AddRange(dependencies.Select(x => $"{x.Name} ({x.Version})"));
				ls.Add(Strings.Constants.Separator);

				ls.ForEach(Console.WriteLine);

				ConsoleManager.WaitForInput();

				return null;
			}
		},
		new()
		{
			Name     = "Update",
			Function = null
		},
		new()
		{
			Name = "Help",
			Function = () =>
			{
				WebUtilities.OpenUrl(Resources.U_Wiki);

				return null;
			}
		},
#if DEBUG

		new()
		{
			Name = "Debug",
			Function = () =>
			{
				Config.Query = @"C:\Users\Deci\Pictures\Test Images\Test1.jpg";
				return true;
			}
		},
#endif

	};

	internal static readonly ConsoleDialog MainMenuDialog = new()
	{
		Options   = MainMenuOptions,
		Header    = Elements.NAME_BANNER,
		Functions = new Dictionary<ConsoleKey, Action>(),
		Status    = "You can also drag and drop a file to run a search."
	};

	static Program()
	{
		// NOTE: Static initializer must be AFTER MainMenuDialog

		var current = UpdateInfo.GetUpdateInfo();

		if (current.Status != VersionStatus.Available) {
			return;
		}

		var option = MainMenuDialog["Update"];

		option.Name = option.Name.AddColor(Elements.ColorHighlight);

		var updateStr =
			$"* Update available (latest: {Elements.GetVersionString(current.Latest.Version)};" +
			$" current: {Elements.GetVersionString(current.Current)})";

		updateStr = updateStr.AddColor(Elements.ColorHighlight);

		MainMenuDialog.Description = updateStr;

		option.Function = () =>
		{
			UpdateInfo.Update(current);
			return null;
		};
	}
}