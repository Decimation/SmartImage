#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Media;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Newtonsoft.Json.Linq;
using RestSharp;
using RestSharp.Serialization.Json;
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


		private SearchClient(string imgInput)
		{
			//

			UploadEngine = SearchConfig.Config.UseImgur ? new ImgurClient() : new ImgOpsEngine();


			var imageInfo = ResolveUploadUrl(imgInput);

			ImageInfo = imageInfo ?? throw new SmartImageException("Image invalid or upload failed");

			Original = FullSearchResult.GetOriginalImageResult(ImageInfo);

			SearchConfig.Config.EnsureConfig();

			Results = new List<FullSearchResult>
			{
				Original
			};


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

		/// <summary>
		///     <see cref="SearchConfig.SearchEngines" />
		/// </summary>
		private SearchEngineOptions Engines { get; }

		private ImageInputInfo ImageInfo { get; }


		/// <summary>
		///     Search tasks (<seealso cref="CreateSearchTasks" />)
		/// </summary>
		private List<Task<FullSearchResult>> SearchTasks { get; }


		/// <summary>
		///     Whether the search is complete
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
		///     Search client interface
		/// </summary>
		public NConsoleInterface Interface { get; }

		public IUploadEngine UploadEngine { get; }


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


			Results.Sort();
			NConsole.Refresh();

		}

		/// <summary>
		/// Original image result
		/// </summary>
		public FullSearchResult Original { get; }


		public static ulong hash(string s, int size = 256)
		{
			//widthAndLength := uint(math.Ceil(math.Sqrt(float64(hashLength)/2.0)) + 1)
			var wl = (int) (Math.Ceiling(Math.Sqrt(((float) size) / 2.0)) + 1);

			Debug.WriteLine($"{wl}");

			Image im = Image.FromFile(s);
			//new Bitmap(9, 8, PixelFormat.Format16bppGrayScale);

			Bitmap c = new Bitmap(im, new Size(wl + 1, wl));


			ulong h = 0;

			// Loop through the images pixels to reset color.
			for (int i = 0; i < c.Width; i++) {
				for (int x = 0; x < c.Height; x++) {
					Color oc        = c.GetPixel(i, x);
					int   grayScale = (int) ((oc.R * 0.3) + (oc.G * 0.59) + (oc.B * 0.11));
					Color nc        = Color.FromArgb(oc.A, grayScale, grayScale, grayScale);
					c.SetPixel(i, x, nc);
				}
			}
			//c = MakeGrayscale3(c);

			// int x, y;
			//
			// for (x = 0; x < c.Width; x++)
			// {
			// 	for (y = 0; y < c.Height; y++)
			// 	{
			// 		Color pixelColor = c.GetPixel(x, y);
			// 		Color newColor   = Color.FromArgb(pixelColor.R, 0, 0);
			// 		c.SetPixel(x, y, newColor); // Now greyscale
			// 	}
			// }

			for (int j = 0; j < wl; j++) {
				for (int k = 0; k < wl; k++) {
					var b   = (c.GetPixel(j, k).R > c.GetPixel(j + 1, k).R);
					var bit = Convert.ToUInt64(b) << (j + k * 8);
					h |= bit;
				}
			}

			return h;
		}

		public static string ResolveDirectLink(string s)
		{
			//todo
			string d = "";

			try {
				var uri  = new Uri(s);
				var host = uri.Host;


				var doc  = new HtmlDocument();
				var html = Network.GetSimpleResponse(s);

				if (host.Contains("danbooru")) {
					Debug.WriteLine("danbooru");


					var jobj = JObject.Parse(html.Content);

					d = (string) jobj["file_url"];


					return d;
				}

				doc.LoadHtml(html.Content);

				var sel = "//img";

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
			var availableEngines = GetAllEngines()
				.Where(e => Engines.HasFlag(e.Engine))
				.ToArray();

			return availableEngines.Select(currentEngine => Task.Run(() => currentEngine.GetResult(ImageInfo.ImageUrl)))
				.ToList();
		}


		/// <summary>
		///     Returns all of the supported search engines
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
		///     Handles image input (either a URL or path) and returns the corresponding image URL
		/// </summary>
		public ImageInputInfo? ResolveUploadUrl(string imageInput)
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
				sb.AppendColor(ColorPrimary, NAME_BANNER);
				sb.Append(SearchConfig.Config);

				sb.AppendLine();

				/*
				 * Upload
				 */
				sb.AppendLine("Uploading image");

				string imgUrl1 = UploadEngine.Upload(imageInput);


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