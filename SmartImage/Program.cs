﻿// ReSharper disable SuggestVarOrType_BuiltInTypes
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
using Windows.Networking.Connectivity;
using Windows.UI.Notifications;
using Kantan.Cli;
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
			/*
			 * Setup
			 * Check compatibility
			 * Register events
			 */

			ToastNotificationManagerCompat.OnActivated += AppInterface.OnToastActivated;

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

			AppConfig.ReadConfigFile();

			if (!HandleArguments())
				return;

			ResultDialog.Subtitle = $"SE: {Config.SearchEngines} "     +
			                        $"| PE: {Config.PriorityEngines} " +
			                        $"| Filtering: {Config.Filtering.ToToggleString()}";

			try {

				CancellationTokenSource cts = new();


				// Run search

				Client.ResultCompleted += OnResultCompleted;
				Client.SearchCompleted += (obj, eventArgs) => OnSearchCompleted(obj, eventArgs, cts);
				Client.ExtraResults    += AppInterface.ShowToast;

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

		private static bool HandleArguments()
		{
			var args = Environment.GetCommandLineArgs();

			if (!args.Any()) {
				HashSet<object> options = NConsole.ReadOptions(AppInterface.MainMenuDialog);

				if (!options.Any()) {
					return false;
				}
			}
			else {

				/*
				 * Handle CLI args
				 */

				//-pe SauceNao,Iqdb -se All -f "C:\Users\Deci\Pictures\Test Images\Test6.jpg"

				try {
					// todo: WIP

					var argEnumerator = args.GetEnumerator();

					while (argEnumerator.MoveNext()) {
						object? paramName = argEnumerator.Current;

						switch (paramName) {

							case P_SE:
								argEnumerator.MoveNext();

								Config.SearchEngines = Enum.Parse<SearchEngineOptions>((string) argEnumerator.Current);
								break;
							case P_PE:
								argEnumerator.MoveNext();

								Config.PriorityEngines =
									Enum.Parse<SearchEngineOptions>((string) argEnumerator.Current);
								break;
							case P_F:
								Config.Filtering = true;
								break;
							default:
								Config.Query = (string) argEnumerator.Current;
								break;
						}
					}

					Client.Reload();
				}
				catch (Exception e) {
					Console.WriteLine(e);
					Console.ReadLine();
				}
			}

			return true;
		}


		#region Event handlers

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

			var s = $"Results: {Client.Results.Count}";

			if (Config.Filtering) {
				s += $" | Filtered: {Client.FilteredResults.Count}";
			}

			s += $" | Pending: {Client.Pending}";

			ResultDialog.Status = s;

			NConsole.Refresh();
		}

		#endregion

		#region Parameters

		/// <summary>
		/// <see cref="SearchConfig.SearchEngines"/>
		/// </summary>
		/// <remarks>Parameter</remarks>
		private const string P_SE = "-se";

		/// <summary>
		/// <see cref="SearchConfig.PriorityEngines"/>
		/// </summary>
		/// <remarks>Parameter</remarks>
		private const string P_PE = "-pe";

		/// <summary>
		/// <see cref="SearchConfig.Filtering"/>
		/// </summary>
		/// <remarks>Switch</remarks>
		private const string P_F = "-f";

		#endregion
	}
}