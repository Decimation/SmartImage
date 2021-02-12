#nullable enable

using SimpleCore.Cli;
using SmartImage.Core;
using SmartImage.Engines;
using SmartImage.Engines.Imgur;
using SmartImage.Engines.Other;
using SmartImage.Engines.SauceNao;
using SmartImage.Engines.TraceMoe;
using SmartImage.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Media;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SimpleCore.Net;
using SimpleCore.Utilities;
using static SimpleCore.Cli.NConsoleOption;
using static SmartImage.Core.Info;
using static SmartImage.Core.Interface;

// ReSharper disable ConvertIfStatementToReturnStatement

namespace SmartImage.Searching
{
	/// <summary>
	///     Searching client
	/// </summary>
	public sealed class SearchClient
	{
		private static readonly string InterfacePrompt =
			$"Enter the option number to open or {NConsole.NC_GLOBAL_EXIT_KEY} to exit.\n" +
			$"Hold down {NC_ALT_FUNC_MODIFIER} to show more info.\n"                       +
			$"Hold down {NC_CTRL_FUNC_MODIFIER} to download.\n"                            +
			$"Hold down {NC_COMBO_FUNC_MODIFIER} to open raw result.\n";

		/// <summary>
		/// <see cref="SearchConfig.SearchEngines"/>
		/// </summary>
		private SearchEngineOptions Engines { get; }

		private ImageInputInfo ImageInfo { get; }


		/// <summary>
		/// Search tasks (<seealso cref="CreateSearchTasks"/>)
		/// </summary>
		private List<Task<FullSearchResult>> SearchTasks { get; }


		/// <summary>
		/// Whether the search is complete
		/// </summary>
		public bool Complete { get; private set; }

		/// <summary>
		///     Searching client
		/// </summary>
		public static SearchClient Client { get; } = new(SearchConfig.Config.ImageInput);

		/// <summary>
		///     Search results
		/// </summary>
		public List<FullSearchResult> Results { get; }

		/// <summary>
		/// Search client interface
		/// </summary>
		public NConsoleInterface Interface { get; }


		private SearchClient(string imgInput)
		{
			//

			var imageInfo = Images.ResolveUploadUrl(imgInput);

			ImageInfo = imageInfo ?? throw new SmartImageException("Image invalid or upload failed");
			

			SearchConfig.Config.EnsureConfig();

			Results = new List<FullSearchResult>();
			Engines = SearchConfig.Config.SearchEngines;

			//

			SearchTasks = CreateSearchTasks();

			Complete = false;

			Interface = new NConsoleInterface(Results)
			{
				SelectMultiple = false,
				Prompt         = InterfacePrompt
			};
		}


		private static async void RunAnalysis(FullSearchResult best)
		{

			var task = Task.Run(() =>
			{
				if (!best.IsAnalyzed) {
					//todo
				}

				best.IsAnalyzed = true;


			});

			await task;
		}


		/// <summary>
		///     Starts search and handles results
		/// </summary>
		public async void Start()
		{
			int len = SearchTasks.Count;

			while (SearchTasks.Any()) {
				Task<FullSearchResult> finished = await Task.WhenAny(SearchTasks);
				SearchTasks.Remove(finished);

				var result = finished.Result;

				Results.Add(result);


				// If the engine is priority, open its result in the browser
				if (result.IsPriority) {
					result.HandlePriorityResult();
				}

				int inProgress = len - SearchTasks.Count;

				Interface.Status = $"Searching: {inProgress}/{len}";

				Results.Sort();

				// Reload console UI
				NConsole.Refresh();


				/*
				 *
				 */

				RunAnalysis(result);

				if (result.ExtendedResults.Any()) {
					foreach (var resultExtendedResult in result.ExtendedResults) {
						RunAnalysis(resultExtendedResult);
					}
				}
			}

			/*
			 * Search is complete
			 */

			Complete         = true;
			Interface.Status = "Search complete";
			NConsole.Refresh();

			/*
			 * Alert user
			 */

			// Play sound
			SystemSounds.Exclamation.Play();

			// Flash taskbar icon
			NativeImports.FlashConsoleWindow();

			// Bring to front
			//NativeImports.BringConsoleToFront();


			if (SearchConfig.Config.PriorityEngines == SearchEngineOptions.Auto) {
				// Results will already be sorted
				// Open best result

				var best = Results[1];

				best.HandlePriorityResult();
			}

			/*
			 *
			 */

			Debug.WriteLine($"Analyzing");


			SpinWait.SpinUntil(() => !Results.All(r => r.IsAnalyzed));
			
			Debug.WriteLine($"Analysis complete");

			Results.Sort();
			NConsole.Refresh();

		}


		private List<Task<FullSearchResult>> CreateSearchTasks()
		{
			var availableEngines = GetAllEngines()
				.Where(e => Engines.HasFlag(e.Engine))
				.ToArray();

			// Add original image to results
			Results.Add(FullSearchResult.GetOriginalImageResult(ImageInfo));

			return availableEngines.Select(currentEngine => Task.Run(() => currentEngine.GetResult(ImageInfo.ImageUrl))).ToList();
		}


		/// <summary>
		/// Returns all of the supported search engines
		/// </summary>
		private static IEnumerable<BaseSearchEngine> GetAllEngines()
		{
			return new BaseSearchEngine[]
			{
				//
				new SauceNaoEngine(),
				new IqdbEngine(),
				new YandexEngine(),
				new TraceMoeEngine(),

				//
				new ImgOpsEngine(),
				new GoogleImagesEngine(),
				new TinEyeEngine(),
				new BingEngine(),
				new KarmaDecayEngine(),
				new TidderEngine()
			};
		}
	}
}