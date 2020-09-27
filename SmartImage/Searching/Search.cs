#region

#nullable enable
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SimpleCore.Utilities;
using SimpleCore.Win32.Cli;
using SmartImage.Searching.Engines.Imgur;
using SmartImage.Searching.Engines.SauceNao;
using SmartImage.Searching.Engines.Simple;
using SmartImage.Searching.Engines.TraceMoe;
using SmartImage.Searching.Model;
using SmartImage.Shell;
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
		/// <summary>
		/// Common image extensions
		/// </summary>
		private static readonly string[] ImageExtensions =
		{
			".jpg", ".jpeg", ".png", ".gif", ".tga", ".jfif", ".bmp"
		};


		private static ISearchEngine[] GetAllEngines()
		{
			var engines = new ISearchEngine[]
			{
				new SauceNaoClient(),
				new ImgOpsClient(),
				new GoogleImagesClient(),
				new TinEyeClient(),
				new IqdbClient(),
				new BingClient(),
				new YandexClient(),
				new KarmaDecayClient(),
				new TraceMoeClient()
			};

			return engines;
		}


		public static bool RunSearch(string img, ref ConsoleOption[] res)
		{
			/*
			 * Run
             */

			// Run checks
			if (!IsFileValid(img)) {
				SearchConfig.UpdateFile();

				return false;
			}

			string auth = SearchConfig.Config.ImgurAuth;
			bool useImgur = !String.IsNullOrWhiteSpace(auth);

			var engines = SearchConfig.Config.SearchEngines;
			//var priority = SearchConfig.Config.PriorityEngines;

			if (engines == SearchEngines.None) {
				//todo
				//CliOutput.WriteError("Please configure search engine preferences!");
				engines = SearchConfig.ENGINES_DEFAULT;
			}


			// Display config
			CliOutput.WriteInfo(SearchConfig.Config);


			string imgUrl = Upload(img, useImgur);

			CliOutput.WriteInfo("Temporary image url: {0}", imgUrl);

			Console.WriteLine();


			// Where the actual searching occurs

			//StartSearches(imgUrl, engines, ref res);
			var threads = StartSearchesMultithread(imgUrl, engines, ref res);

			foreach (var thread in threads) {
				thread.Start();
			}

			return true;
		}

		

		private static Thread[] StartSearchesMultithread(string imgUrl, SearchEngines engines, ref ConsoleOption[] res)
		{
			// todo: improve
			// todo: use tasks

			var availableEngines = GetAllEngines()
				.Where(e => engines.HasFlag(e.Engine))
				.ToArray();

			int i = 0;

			res = new SearchResult[availableEngines.Length + 1];
			res[i] = new SearchResult(ConsoleColor.Gray, "(Original image)", imgUrl, null);

			i++;


			var threads = new List<Thread>();

			foreach (var currentEngine in availableEngines)
			{
				var options = res;

				int i1 = i;

				ThreadStart ts = () =>
				{
					var result = currentEngine.GetResult(imgUrl);
					options[i1] = result;


					// If the engine is priority, open its result in the browser
					if (SearchConfig.Config.PriorityEngines.HasFlag(currentEngine.Engine))
					{
						Network.OpenUrl(result.Url);
					}

					ConsoleIO.Status = ConsoleIO.STATUS_REFRESH;

					
				};
				var t = new Thread(ts);
				t.Priority = ThreadPriority.Highest;
				t.Name = string.Format("thread - {0}", currentEngine.Name);
				threads.Add(t);

				i++;
			}

			return threads.ToArray();


		}

		private static bool IsFileValid(string img)
		{
			if (string.IsNullOrWhiteSpace(img)) {
				return false;
			}

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
				var imgur = new ImgurClient();
				imgUrl = imgur.Upload(img);
			}
			else {
				CliOutput.WriteInfo("Using ImgOps for image upload (2 hour cache)");
				var imgOps = new ImgOpsClient();
				imgUrl = imgOps.UploadTempImage(img, out _);
			}


			return imgUrl;
		}
	}
}