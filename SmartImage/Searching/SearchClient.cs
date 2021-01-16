#nullable enable

using Novus.Utilities;
using Novus.Win32;
using SimpleCore.Console.CommandLine;
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
using System.Threading;
using System.Threading.Tasks;
using static SimpleCore.Console.CommandLine.NConsoleOption;

// ReSharper disable ConvertIfStatementToReturnStatement

namespace SmartImage.Searching
{
	/// <summary>
	///     Searching client
	/// </summary>
	public sealed class SearchClient
	{
		private const string ORIGINAL_IMAGE_NAME = "(Original image)";

		private static readonly string InterfacePrompt =
			$"Enter the option number to open or {NConsole.NC_GLOBAL_EXIT_KEY} to exit.\n" +
			$"Hold down {NC_ALT_FUNC_MODIFIER} to show more info.\n"                       +
			$"Hold down {NC_CTRL_FUNC_MODIFIER} to download.\n"                            +
			$"Hold down {NC_COMBO_FUNC_MODIFIER} to open raw result.\n";

		/// <summary>
		/// <see cref="SearchConfig.SearchEngines"/>
		/// </summary>
		private SearchEngineOptions Engines { get; }

		/// <summary>
		/// Image
		/// </summary>
		private FileInfo ImageFile { get; }

		/// <summary>
		/// Url of <seealso cref="ImageFile"/>
		/// </summary>
		private string ImageUrl { get; }

		/// <summary>
		/// Thread that monitors the search (<see cref="RunSearchMonitor"/>)
		/// </summary>
		private Thread SearchMonitor { get; }

		/// <summary>
		/// Search tasks (<seealso cref="CreateSearchTasks"/>)
		/// </summary>
		private Task[] SearchTasks { get; }

		/// <summary>
		/// Search results
		/// </summary>
		private List<FullSearchResult> m_results;

		/// <summary>
		/// Whether the search is complete
		/// </summary>
		public bool Complete { get; private set; }

		/// <summary>
		///     Searching client
		/// </summary>
		public static SearchClient Client { get; } = new(SearchConfig.Config.Image);

		/// <summary>
		///     Search results
		/// </summary>
		public ref List<FullSearchResult> Results => ref m_results;

		/// <summary>
		/// Search client interface
		/// </summary>
		public NConsoleInterface Interface { get; }

		private SearchClient(string img)
		{
			if (!Images.IsFileValid(img)) {
				throw new SmartImageException("Invalid image");
			}

			string auth     = SearchConfig.Config.ImgurAuth;
			bool   useImgur = !String.IsNullOrWhiteSpace(auth);

			SearchConfig.Config.EnsureConfig();


			m_results = null!;
			Engines   = SearchConfig.Config.SearchEngines;
			ImageFile = new FileInfo(img);

			//

			string? imgUrl = Upload(img, useImgur);

			ImageUrl = imgUrl ?? throw new SmartImageException("Image upload failed");

			//

			SearchTasks = CreateSearchTasks();

			// Joining each thread isn't necessary as this object is disposed upon program exit
			// Background threads won't prevent program termination

			SearchMonitor = new Thread(RunSearchMonitor)
			{
				Priority     = ThreadPriority.Highest,
				IsBackground = true
			};

			Complete = false;

			Interface = new NConsoleInterface(Results)
			{
				SelectMultiple = false,
				Prompt         = InterfacePrompt
			};
		}

		/// <summary>
		/// Monitors the search process
		/// </summary>
		private void RunSearchMonitor()
		{
			while (SearchTasks.Any(t => !t.IsCompleted)) {
				int inProgress = SearchTasks.Count(t => t.IsCompleted);
				int len        = SearchTasks.Length;

				Interface.Status = $"Searching: {inProgress}/{len}";
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

				var best = m_results[1];

				HandleResultOpen(best);

			}
		}

		/// <summary>
		///     Starts search
		/// </summary>
		public void Start()
		{
			SearchMonitor.Start();

			foreach (var thread in SearchTasks) {
				thread.Start();
			}
		}

		/// <summary>
		/// Creates a <see cref="FullSearchResult"/> for the original image
		/// </summary>
		private FullSearchResult GetOriginalImageResult()
		{
			var result = new FullSearchResult(Color.White, ORIGINAL_IMAGE_NAME, ImageUrl)
			{
				Similarity = 100.0f,
			};

			var fileFormat = FileSystem.ResolveFileType(ImageFile.FullName);

			double fileSizeMegabytes =
				MathHelper.ConvertToUnit(FileSystem.GetFileSize(ImageFile.FullName), MetricUnit.Mega);

			(int width, int height) = Images.GetDimensions(ImageFile.FullName);

			result.Width  = width;
			result.Height = height;

			double mpx = MathHelper.ConvertToUnit(width * height, MetricUnit.Mega);

			string infoStr = $"Info: {ImageFile.Name} ({fileSizeMegabytes:F} MB) ({mpx:F} MP) ({fileFormat.Name})";

			result.ExtendedInfo.Add(infoStr);

			return result;
		}

		private Task[] CreateSearchTasks()
		{
			// todo: improve, hacky :(

			var availableEngines = GetAllEngines()
				.Where(e => Engines.HasFlag(e.Engine))
				.ToArray();

			m_results = new List<FullSearchResult>(availableEngines.Length + 1)
			{
				GetOriginalImageResult()
			};

			return availableEngines.Select(currentEngine => new Task(() => RunSearchTask(currentEngine))).ToArray();
		}

		/// <summary>
		/// Handles result opening from priority engines and filtering
		/// </summary>
		private static void HandleResultOpen(FullSearchResult result)
		{
			/*
			 * Filtering is disabled
			 * Open it anyway
			 */

			if (!SearchConfig.Config.FilterResults) {
				result.Function();
				return;
			}

			/*
			 * Filtering is enabled
			 * Determine if it passes the threshold
			 */

			if (!result.Filter) {
				// Open result
				result.Function();
			}
			else {
				Debug.WriteLine($"Filtering result {result.Name}");
			}


		}

		/// <summary>
		/// Individual task search operation
		/// </summary>
		private void RunSearchTask(ISearchEngine currentEngine)
		{
			var result = currentEngine.GetResult(ImageUrl);

			m_results.Add(result);

			// If the engine is priority, open its result in the browser
			if (SearchConfig.Config.PriorityEngines.HasFlag(currentEngine.Engine)) {

				HandleResultOpen(result);
			}

			// Sort results
			m_results.Sort(FullSearchResult.CompareResults);

			// Reload console UI
			NConsole.Refresh();
		}


		/// <summary>
		/// Returns all of the supported search engines
		/// </summary>
		private static IEnumerable<ISearchEngine> GetAllEngines()
		{
			return new ISearchEngine[]
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
				new KarmaDecayEngine()
			};
		}


		/// <summary>
		/// Uploads the image
		/// </summary>
		private static string? Upload(string img, bool useImgur)
		{

			IUploadEngine uploadEngine;

			string imgUrl;

			/*
			 * Show settings 
			 */

			Console.WriteLine(Info.NAME_BANNER);

			NConsole.OverrideForegroundColor = Core.Interface.ColorConfig;
			NConsole.WriteInfo(SearchConfig.Config);
			NConsole.ResetOverrideColors();

			/*
			 * Upload
			 */

			NConsole.WriteInfo("Uploading image");


			if (useImgur) {
				try {
					NConsole.WriteInfo("Using Imgur for image upload");
					uploadEngine = new ImgurClient();
					imgUrl       = uploadEngine.Upload(img);
				}
				catch (Exception e) {
					NConsole.WriteError("Error uploading with Imgur: {0}", e.Message);
					NConsole.WriteInfo("Using ImgOps instead");
					UploadImgOps();
				}
			}
			else {
				UploadImgOps();
			}


			void UploadImgOps()
			{
				NConsole.WriteInfo("Using ImgOps for image upload (1 hour cache)");
				uploadEngine = new ImgOpsEngine();
				imgUrl       = uploadEngine.Upload(img);
			}

			NConsole.WriteInfo($"Temporary image url: {imgUrl}");

			return imgUrl;
		}
	}
}