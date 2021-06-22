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

		[DllImport("user32.dll")] static extern IntPtr GetKeyboardLayout(uint thread);
		public static CultureInfo GetCurrentKeyboardLayout()
		{
			try
			{
				IntPtr foregroundWindow  = Native.GetForegroundWindow();
				int    foregroundProcess = Native.GetWindowThreadProcessId(foregroundWindow, out _);
				int    keyboardLayout    = GetKeyboardLayout((uint) foregroundProcess).ToInt32() & 0xFFFF;
				return new CultureInfo(keyboardLayout);
			}
			catch (Exception _)
			{
				return new CultureInfo(1033); // Assume English if something went wrong.
			}
		}
		/// <summary>
		/// Entry point
		/// </summary>
		private static async Task Main(string[] args)
		{
#if DEBUG
			if (!args.Any()) {
				//args = new[] {""};
			}
#endif

			/*
			 * Setup
			 * Check compatibility
			 */

			Native.SetConsoleOutputCP(Native.CP_WIN32_UNITED_STATES);



			Console.Title = $"{Info.NAME}";

			NConsole.Init();
			Console.Clear();

			Console.CancelKeyPress += (sender, eventArgs) => { };


			var process = Process.GetCurrentProcess();
			process.PriorityClass = ProcessPriorityClass.AboveNormal;


			/*
			 * Start
			 */

			// Update

			ReadConfigFile();

			if (!args.Any()) {
				var options = NConsole.ReadOptions(MainDialog.MainMenuDialog);


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


						default:
							Config.Query = args[0];
							break;
					}
				}
			}

			try {

				CancellationTokenSource cts = new();


				// Run search

				Client.ResultCompleted += ResultCompleted;

				Client.SearchCompleted += (sender, eventArgs) =>
				{
					NativeUI.FlashConsoleWindow();
					//SystemSounds.Exclamation.Play();

					cts.Cancel();
					cts.Dispose();

					if (Config.Notification) {
						ShowToast();
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


		private static void ResultCompleted(object? sender, SearchResultEventArgs eventArgs)
		{
			var result = eventArgs.Result;

			var option = NConsoleFactory.Create(result);

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

		public static readonly NConsoleDialog ResultDialog = new()
		{
			Options = new List<NConsoleOption>()
		};

		public static readonly SearchConfig Config = new();

		public static readonly SearchClient Client = new(Config);

		public static string ConfigFile
		{
			get
			{
				string file = Path.Combine(Info.AppFolder, Info.NAME_CFG);

				if (!File.Exists(file)) {
					var f = File.Create(file);
					f.Close();
				}

				return file;
			}
		}

		private const string K_ENGINES            = "engines";
		private const string K_PRIORITY_ENGINES   = "priority-engines";
		private const string K_FILTER             = "filter";
		private const string K_NOTIFICATION       = "notification";
		private const string K_NOTIFICATION_IMAGE = "notification-image";

		public static void ReadConfigFile()
		{
			var map = Collections.ReadDictionary(ConfigFile);


			foreach (var (key, value) in ConfigMap) {
				if (!map.ContainsKey(key)) {
					map.Add(key, value);
				}
			}

			Config.SearchEngines     = Enum.Parse<SearchEngineOptions>(map[K_ENGINES]);
			Config.PriorityEngines   = Enum.Parse<SearchEngineOptions>(map[K_PRIORITY_ENGINES]);
			Config.Filtering         = Boolean.Parse(map[K_FILTER]);
			Config.Notification      = Boolean.Parse(map[K_NOTIFICATION]);
			Config.NotificationImage = Boolean.Parse(map[K_NOTIFICATION_IMAGE]);

			SaveConfigFile();

			Client.Reload();

			Debug.WriteLine($"Updated config from {ConfigFile}");
		}

		public static Dictionary<string, string> ConfigMap
		{
			get
			{
				var map = new Dictionary<string, string>()
				{
					{K_ENGINES, Config.SearchEngines.ToString()},
					{K_PRIORITY_ENGINES, Config.PriorityEngines.ToString()},
					{K_FILTER, Config.Filtering.ToString()},
					{K_NOTIFICATION, Config.Notification.ToString()},
					{K_NOTIFICATION_IMAGE, Config.NotificationImage.ToString()},
				};
				return map;
			}
		}

		public static void SaveConfigFile()
		{
			var map = ConfigMap;

			Collections.WriteDictionary(map, ConfigFile);

			Debug.WriteLine($"Saved to {ConfigFile}");
		}

		public static void ShowToast()
		{
			var button = new ToastButton();

			button.SetContent("Open")
			      .AddArgument("action", "open");

			var button2 = new ToastButton();

			button2.SetContent("Dismiss")
			       .AddArgument("action", "dismiss");

			var builder = new ToastContentBuilder();

			builder.AddButton(button)
			       .AddButton(button2)
			       .AddText("Search complete")
			       .AddText($"Results: {Client.Results.Count}");

			var bestResult = Client.FindBestResult();

			if (Config.NotificationImage) {
				Debug.WriteLine("Finding direct");
				var direct = Client.FindDirectResult();
				Debug.WriteLine(direct);

				//if (direct is { PrimaryResult: { Direct: { } } })
				//{
				//	builder.AddInlineImage(direct.PrimaryResult.Direct);
				//	Debug.WriteLine(direct.PrimaryResult.Direct);
				//}


				if (direct is {Direct: { }}) {
					Debug.WriteLine($"Downloading {direct}");
					string file = WebUtilities.Download(direct.Direct.ToString(), Path.GetTempPath());
					Debug.WriteLine($"{file}");
					builder.AddHeroImage(new Uri(file));
					//File.Delete(s);

					AppDomain.CurrentDomain.ProcessExit += (sender, args) =>
					{
						File.Delete(file);
					};
				}
			}


			ToastNotificationManagerCompat.OnActivated += compat =>
			{
				// Obtain the arguments from the notification
				ToastArguments args = ToastArguments.Parse(compat.Argument);

				foreach (var argument in args) {
					Debug.WriteLine($">>> {argument}");

					if (argument.Key == "action" && argument.Value == "open") {
						//Client.Results.Sort();


						if (bestResult is {Url: { }}) {
							WebUtilities.OpenUrl(bestResult.Url.ToString());
						}
					}
				}
			};

			builder.Show();

			//ToastNotificationManager.CreateToastNotifier();
		}
	}
}