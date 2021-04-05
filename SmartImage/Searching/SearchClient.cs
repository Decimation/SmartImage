#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Media;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Newtonsoft.Json.Linq;
using SimpleCore.Cli;
using SimpleCore.Net;
using SimpleCore.Utilities;
using SmartImage.Configuration;
using SmartImage.Engines;
using SmartImage.Engines.Imgur;
using SmartImage.Engines.Other;
using SmartImage.Engines.SauceNao;
using SmartImage.Engines.TraceMoe;
using SmartImage.Utilities;
using static SimpleCore.Cli.NConsoleOption;
using static SmartImage.Core.Info;
using static SmartImage.Core.Interface;

// ReSharper disable UnusedMember.Global

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
			$"Hold down {NC_COMBO_FUNC_MODIFIER} to open raw result.\n" +
			$"{NConsole.NC_GLOBAL_RETURN_KEY}: Refine\n"+
			$"{NConsole.NC_GLOBAL_REFRESH_KEY}: Refresh";


		public SearchClient(SearchConfig config)
		{
			//

			EngineOptions = config.SearchEngines;

			SearchEngines = GetAllEngines()
				.Where(e => EngineOptions.HasFlag(e.Engine))
				.ToArray();


			UploadEngine = config.UseImgur ? new ImgurClient() : new ImgOpsEngine();

			/*
			 *
			 */


			var imageInfo = ResolveUploadUrl(config.ImageInput);


			ImageInfo = imageInfo ?? throw new SmartImageException("Image invalid or upload failed");

			OriginalImageResult = FullSearchResult.GetOriginalImageResult(ImageInfo);


			/*
			 *
			 */

			config.EnsureConfig();

			//

			Results = CreateSearchResults();


			//

			SearchTasks = CreateSearchTasks();

			IsComplete = false;

			//
			Interface = new NConsoleInterface(Results)
			{
				SelectMultiple = false,
				Prompt         = InterfacePrompt
			};

		}


		/// <summary>
		///     <see cref="SearchConfig.SearchEngines" />
		/// </summary>
		private SearchEngineOptions EngineOptions { get; }

		/// <summary>
		///     Image input
		/// </summary>
		private ImageInputInfo ImageInfo { get; }


		/// <summary>
		///     Search tasks (<seealso cref="CreateSearchTasks" />)
		/// </summary>
		private List<Task<FullSearchResult>> SearchTasks { get; }


		/// <summary>
		///     Whether the search is complete
		/// </summary>
		public bool IsComplete { get; private set; }


		/// <summary>
		///     Search results
		/// </summary>
		public List<FullSearchResult> Results { get; }

		/// <summary>
		///     Search client interface
		/// </summary>
		public NConsoleInterface Interface { get; }

		/// <summary>
		///     Upload engine
		/// </summary>
		public IUploadEngine UploadEngine { get; }


		/// <summary>
		///     Original image result
		/// </summary>
		private FullSearchResult OriginalImageResult { get; }

		/// <summary>
		/// Search engines
		/// </summary>
		private IList<BaseSearchEngine> SearchEngines { get; }

		private List<FullSearchResult> CreateSearchResults()
		{
			var results = new List<FullSearchResult>
			{
				OriginalImageResult
			};

			// hack: add stub results
			results.AddRange(SearchEngines.Select(e => new FullSearchResult(e, null) {Name = e.Name}));

			return results;
		}

		

		/// <summary>
		///     Starts search and handles results
		/// </summary>
		public async void Start()
		{
			NConsole.AutoResizeHeight = true;

			int len = SearchTasks.Count;

			while (SearchTasks.Any()) {
				Task<FullSearchResult> finished = await Task.WhenAny(SearchTasks);
				SearchTasks.Remove(finished);

				var result = finished.Result;

				// hack: update stub with full result
				Results.Replace(r => r.Name == result.Name, result);


				// If the engine is priority, open its result in the browser
				if (result.IsPriority) {
					result.HandleResultOpen();
				}

				// Update

				int inProgress = len - SearchTasks.Count;

				Interface.Status = $"Searching: {inProgress}/{len}";


				//var cmp = Collections.ProjectionComparer<FullSearchResult>
				//	.Create(g => g)


				//	 .ThenBy(r => r.Filter)
				//	// .ThenBy(r => r.Similarity)
				//	// .ThenBy(r => r.ExtendedResults.Count)
				//	// .ThenBy(r => r.Metadata.Count)
				//	;

				//Results.Sort(cmp);

				Results.Sort();

				// Reload console UI
				NConsole.Refresh();
			}

			/*
			 * Search is complete
			 */

			IsComplete       = true;
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

				best.HandleResultOpen();
			}

		}

		public static string? ResolveDirectLink(string s)
		{
			//todo
			string d = "";

			try {
				var     uri  = new Uri(s);
				string? host = uri.Host;


				var doc  = new HtmlDocument();
				var html = Network.GetSimpleResponse(s);

				if (host.Contains("danbooru")) {
					Debug.WriteLine("danbooru");


					var jObject = JObject.Parse(html.Content);

					d = (string) jObject["file_url"]!;


					return d;
				}

				doc.LoadHtml(html.Content);

				string? sel = "//img";

				var nodes = doc.DocumentNode.SelectNodes(sel);

				if (nodes == null) {
					return null;
				}

				Debug.WriteLine($"{nodes.Count}");
				Debug.WriteLine($"{nodes[0]}");


			}
			catch (Exception e) {
				Debug.WriteLine($"direct {e.Message}");
				return d;
			}


			return d;
		}

		private List<Task<FullSearchResult>> CreateSearchTasks()
		{
			return SearchEngines.Select(currentEngine => Task.Run(delegate
			{
				var sw = Stopwatch.StartNew();

				var result = currentEngine.GetResult(ImageInfo.ImageUrl);

				sw.Stop();

				result.Elapsed = sw.Elapsed;

				return result;
			})).ToList();
		}


		/// <summary>
		///     Returns all of the supported search engines
		/// </summary>
		private static IEnumerable<BaseSearchEngine> GetAllEngines()
		{
			return new BaseSearchEngine[]
			{
				new SauceNaoEngine(),
				new IqdbEngine(),
				new YandexEngine(),
				new TraceMoeEngine(),
				new ImgOpsEngine(),
				new GoogleImagesEngine(),
				new TinEyeEngine(),
				new BingEngine(),
				new KarmaDecayEngine(),
				new TidderEngine(),
				new Ascii2DEngine()
			};
		}

		/// <summary>
		///     Handles image input (either a URL or path) and returns the corresponding image URL
		/// </summary>
		private ImageInputInfo? ResolveUploadUrl(string imageInput)
		{

			if (!ImageInputInfo.TryCreate(imageInput, out var info)) {
				return null;
			}


			Debug.WriteLine($"{info}");


			string? imgUrl;

			if (!info.IsUrl) {
				/*
				 * Show settings 
				 */
				var sb = new StringBuilder();
				sb.AppendColor(ColorMain1, NAME_BANNER);
				sb.Append(SearchConfig.Config);

				sb.AppendLine();

				/*
				 * Upload
				 */
				sb.AppendLine("Uploading image");

				var sw = Stopwatch.StartNew();

				string imgUrl1 = UploadEngine.Upload(imageInput);

				sw.Stop();

				info.UploadElapsed = sw.Elapsed;

				sb.AppendLine($"Temporary image url: {imgUrl1}");

				NConsole.Write(sb);


				imgUrl = imgUrl1;
			}
			else {
				imgUrl = imageInput;
			}


			Debug.WriteLine($"URL --> {imgUrl}");

			info.ImageUrl = imgUrl;

			return info;
		}
	}
}