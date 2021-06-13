// ReSharper disable RedundantUsingDirective


#nullable enable
using SimpleCore.Cli;
using SmartImage.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Media;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Novus.Win32;
using SimpleCore.Net;
using SimpleCore.Utilities;
using SmartImage.Lib;
using SmartImage.Lib.Engines;
using SmartImage.Lib.Searching;
using SmartImage.Lib.Utilities;
using SmartImage.Utilities;

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


		/// <summary>
		/// Entry point
		/// </summary>
		private static async Task Main(string[] args)
		{
			/*
			 * Setup
			 * Check compatibility
			 */

			Native.SetConsoleOutputCP(Native.CP_WIN32_UNITED_STATES);

			Console.Title = $"{Info.NAME}";

			NConsole.Init();
			Console.Clear();

			Console.CancelKeyPress += (sender, eventArgs) => { };

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
					SystemSounds.Exclamation.Play();

					cts.Cancel();
					cts.Dispose();
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

			var option = DialogBridge.CreateOption(result);

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

		private const string K_ENGINES          = "engines";
		private const string K_PRIORITY_ENGINES = "priority-engines";
		private const string K_FILTER           = "filter";

		public static void ReadConfigFile()
		{
			var map = Collections.ReadDictionary(ConfigFile);

			if (map.Count == 0) {
				SaveConfigFile();
				map = Collections.ReadDictionary(ConfigFile);
			}


			Config.SearchEngines   = Enum.Parse<SearchEngineOptions>(map[K_ENGINES]);
			Config.PriorityEngines = Enum.Parse<SearchEngineOptions>(map[K_PRIORITY_ENGINES]);
			Config.Filtering       = Boolean.Parse(map[K_FILTER]);

			Client.Reload();

			Debug.WriteLine($"Updated config from {ConfigFile}");
		}

		public static void SaveConfigFile()
		{
			var map = new Dictionary<string, string>()
			{
				{K_ENGINES, Config.SearchEngines.ToString()},
				{K_PRIORITY_ENGINES, Config.PriorityEngines.ToString()},
				{K_FILTER, Config.Filtering.ToString()}
			};

			Collections.WriteDictionary(map, ConfigFile);

			Debug.WriteLine($"Saved to {ConfigFile}");
		}
	}
}