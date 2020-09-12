#region

#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SimpleCore.Utilities;
using SmartImage.Engines;
using SmartImage.Engines.Imgur;
using SmartImage.Engines.SauceNao;
using SmartImage.Engines.Simple;
using SmartImage.Engines.TraceMoe;
using SmartImage.Utilities;

// ReSharper disable ReturnTypeCanBeEnumerable.Local

#endregion

// ReSharper disable ReturnTypeCanBeEnumerable.Global

namespace SmartImage.Searching
{

	/// <summary>
	/// Runs image searches
	/// </summary>
	public static class Search
	{
		private static readonly string[] ImageExtensions =
		{
			".jpg", ".jpeg", ".png", ".gif", ".tga", ".jfif"
		};


		private static ISearchEngine[] GetAvailableEngines()
		{
			var engines = new List<ISearchEngine>();

			var others = new ISearchEngine[]
			{
				new SauceNao(),

				new ImgOps(),
				new GoogleImages(),

				new TinEye(),
				new Iqdb(),

				new Bing(),
				new Yandex(),


				new KarmaDecay(),
				new TraceMoe()
			};

			engines.AddRange(others);

			return engines.ToArray();
		}


		public static bool RunSearch(string img, ref SearchResult[] res)
		{
			/*
			 * Run 
             */

			string auth = SearchConfig.Config.ImgurAuth;
			bool useImgur = !String.IsNullOrWhiteSpace(auth);

			var engines = SearchConfig.Config.SearchEngines;
			//var priority = SearchConfig.Config.PriorityEngines;

			if (engines == SearchEngines.None) {
				//todo
				//CliOutput.WriteError("Please configure search engine preferences!");
				engines = SearchConfig.ENGINES_DEFAULT;
			}


			// Exit
			if (!IsFileValid(img)) {
				SearchConfig.Cleanup();

				return false;
			}

			// Display config
			CliOutput.WriteInfo(SearchConfig.Config);

			string imgUrl = Upload(img, useImgur);

			CliOutput.WriteInfo("Temporary image url: {0}", imgUrl);

			Console.WriteLine();

			//Console.ReadLine();

			//
			// Search
			//


			// Where the actual searching occurs

			StartSearches(imgUrl, engines, ref res);


			return true;
		}

		private static void StartSearches(string imgUrl, SearchEngines engines, ref SearchResult[] res)
		{
			// todo: improve
			// todo: use tasks

			var availableEngines = GetAvailableEngines()
				.Where(e => engines.HasFlag(e.Engine))
				.ToArray();

			int i = 0;

			res = new SearchResult[availableEngines.Length + 1];
			res[i] = new SearchResult(imgUrl, "(Original image)");

			i++;


			foreach (var currentEngine in availableEngines) {
				string wait = String.Format("{0}: ...", currentEngine.Engine);

				CliOutput.WithColor(ConsoleColor.Blue, () =>
				{
					//
					Console.Write(wait);
				});

				var sw = Stopwatch.StartNew();

				// Run search
				var result = currentEngine.GetResult(imgUrl);

				sw.Stop();

				if (result != null) {
					string? url = result.Url;

					var sb = new StringBuilder();
					double t = sw.Elapsed.TotalSeconds;
					sb.AppendFormat("{0}: Done ({1:F3} sec)\n", result.Name, t);


					//todo

					bool ok = url != null;

					string sz = sb.ToString();


					if (ok) {
						CliOutput.OnCurrentLine(ConsoleColor.Green, sz);

						if (SearchConfig.Config.PriorityEngines.HasFlag(currentEngine.Engine)) {
							NetworkUtilities.OpenUrl(result.Url);
						}
					}
					else {
						CliOutput.OnCurrentLine(ConsoleColor.Yellow, sz);
					}

					res[i] = result;
				}

				// todo

				i++;
			}


		}

		private static bool IsFileValid(string img)
		{
			if (!File.Exists(img)) {
				CliOutput.WriteError("File does not exist: {0}", img);
				return false;
			}

			bool extOkay = ImageExtensions.Any(img.ToLower().EndsWith);

			if (!extOkay) {
				return CliOutput.ReadConfirm("File extension is not recognized as a common image format. Continue?");
			}


			return true;
		}

		private static string Upload(string img, bool useImgur)
		{
			string imgUrl;

			if (useImgur) {
				CliOutput.WriteInfo("Using Imgur for image upload");
				var imgur = new Imgur();
				imgUrl = imgur.Upload(img);
			}
			else {
				CliOutput.WriteInfo("Using ImgOps for image upload (2 hour cache)");
				var imgOps = new ImgOps();
				imgUrl = imgOps.UploadTempImage(img, out _);
			}


			return imgUrl;
		}
	}
}