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

		/// <summary>
		/// Image file if an image file was given
		/// </summary>
		private FileInfo? ImageFile { get; }

		/// <summary>
		/// MIME type of URI if an image URI was given
		/// </summary>
		private string? ImageMimeType { get; }

		/// <summary>
		/// Url of <seealso cref="ImageFile"/>
		/// </summary>
		private string ImageUrl { get; }


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
		public static SearchClient Client { get; } = new(SearchConfig.Config.Image);

		/// <summary>
		///     Search results
		/// </summary>
		public List<FullSearchResult> Results { get; }

		/// <summary>
		/// Search client interface
		/// </summary>
		public NConsoleInterface Interface { get; }


		private SearchClient(string img)
		{
			if (!IsInputImageValid(img)) {
				throw new SmartImageException("Invalid image!");
			}

			string auth     = SearchConfig.Config.ImgurAuth;
			bool   useImgur = !String.IsNullOrWhiteSpace(auth);

			SearchConfig.Config.EnsureConfig();


			Results = new List<FullSearchResult>();
			Engines = SearchConfig.Config.SearchEngines;


			//

			string? imgUrl = ResolveUploadUrl(img, useImgur, out bool isFile);

			if (isFile) {
				ImageFile = new FileInfo(img);
			}
			else {
				ImageMimeType = MediaTypes.Identify(img);
			}

			ImageUrl = imgUrl ?? throw new SmartImageException("Image upload failed");


			//

			SearchTasks = CreateSearchTasks();

			Complete = false;

			Interface = new NConsoleInterface(Results)
			{
				SelectMultiple = false,
				Prompt         = InterfacePrompt
			};
		}

		public static bool IsInputImageValid(string? img) => IsInputImageValid(img, out _, out _);

		public static bool IsInputImageValid(string? img, out bool isUri, out bool isFile)
		{
			if (String.IsNullOrWhiteSpace(img)) {
				isFile = false;
				isUri  = false;
				return false;
			}

			isFile = File.Exists(img);

			isUri = Network.IsUri(img, out _) && !isFile;

			if (isUri) {

				// var type = MediaTypes.IdentifyType(img);
				//
				// if (type == null) {
				// 	return false;
				// }
				//
				// var isUriFile = MediaTypes.IsImage(type);
				//
				// Debug.WriteLine($"MIME type: {type} | Is URI file: {isUriFile}");

				var isUriFile = MediaTypes.IsDirect(img, MimeType.Image);
				
				Debug.WriteLine($"Is URI file: {isUriFile}");

				if (!isUriFile) {

					return false;
				}
			}

			return isFile || isUri;
		}

		/// <summary>
		/// Handles image input (either a URL or path) and returns the corresponding image URL
		/// </summary>
		private static string? ResolveUploadUrl(string img, bool useImgur, out bool isFile)
		{

			if (!IsInputImageValid(img, out var isUri, out isFile)) {
				return null;
			}


			Debug.WriteLine($"Input is URI: {isUri} | Input is file: {isFile}");


			string? imgUrl = !isUri ? Upload(img, useImgur) : img;

			Debug.WriteLine($"URL --> {imgUrl}");

			return imgUrl;
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
					HandleResultOpen(result);
				}

				int inProgress = len - SearchTasks.Count;

				Interface.Status = $"Searching: {inProgress}/{len}";

				Results.Sort(FullSearchResult.CompareResults);

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

				HandleResultOpen(best);
			}

			/*
			 *
			 */

			Debug.WriteLine($"Analyzing");


			while (!Results.All(r => r.IsAnalyzed)) { }

			Debug.WriteLine($"Analysis complete");

			Results.Sort(FullSearchResult.CompareResults);
			NConsole.Refresh();

		}


		private List<Task<FullSearchResult>> CreateSearchTasks()
		{
			var availableEngines = GetAllEngines()
				.Where(e => Engines.HasFlag(e.Engine))
				.ToArray();

			// Add original image to results
			Results.Add(FullSearchResult.GetOriginalImageResult(ImageUrl, ImageFile, ImageMimeType));

			return availableEngines.Select(currentEngine => Task.Run(() => currentEngine.GetResult(ImageUrl))).ToList();
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
			var sb = new StringBuilder();
			sb.AppendColor(ColorPrimary, NAME_BANNER);
			sb.Append(SearchConfig.Config);

			sb.AppendLine();

			/*
			 * Upload
			 */
			sb.AppendLine("Uploading image");


			if (useImgur) {
				try {
					sb.AppendLine("Using Imgur for image upload");
					uploadEngine = new ImgurClient();
					imgUrl       = uploadEngine.Upload(img);
				}
				catch (Exception e) {
					sb.AppendLine($"Error uploading with Imgur: {e.Message}");
					sb.AppendLine("Using ImgOps instead");
					UploadImgOps();
				}
			}
			else {
				UploadImgOps();
			}


			void UploadImgOps()
			{
				sb.AppendLine("Using ImgOps for image upload (1 hour cache)");
				uploadEngine = new ImgOpsEngine();
				imgUrl       = uploadEngine.Upload(img);
			}

			sb.AppendLine($"Temporary image url: {imgUrl}");
			
			NConsole.Write(sb);


			return imgUrl;
		}
	}
}