#region

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SimpleCore.Utilities;
using SmartImage.Engines;
using SmartImage.Engines.Imgur;
using SmartImage.Engines.SauceNao;
using SmartImage.Engines.TraceMoe;
using SmartImage.Model;

#endregion

// ReSharper disable ReturnTypeCanBeEnumerable.Global

namespace SmartImage.Searching
{
	public static class Search
	{
		private static readonly string[] ImageExtensions =
		{
			".jpg", ".jpeg", ".png", ".gif", ".tga", ".jfif"
		};


		private static ISearchEngine[] GetAvailableEngines()
		{
			var engines = new List<ISearchEngine>();


			bool sauceNaoConfigured = !string.IsNullOrWhiteSpace(RuntimeInfo.Config.SauceNaoAuth);

			if (sauceNaoConfigured) {
				engines.Add(new SauceNao());
			}
			else {
				engines.Add(new BasicSauceNao());
			}

			var others = new ISearchEngine[]
			{
				new ImgOps(),
				new GoogleImages(),
				
				new TinEye(),
				new Iqdb(),
				
				new Bing(),
				new Yandex(),
				
				
				new KarmaDecay(),
				new TraceMoe(),
			};

			engines.AddRange(others);

			return engines.ToArray();
		}

		public static bool RunSearch(string img, ref SearchResults res)
		{
			/*
			 * Run 
             */

			var  auth     = RuntimeInfo.Config.ImgurAuth;
			bool useImgur = !string.IsNullOrWhiteSpace(auth);

			var engines  = RuntimeInfo.Config.Engines;
			var priority = RuntimeInfo.Config.PriorityEngines;

			if (engines == SearchEngines.None) {
				//todo
				//CliOutput.WriteError("Please configure search engine preferences!");
				engines = SearchEngines.All;
			}


			// Exit
			if (!Search.IsFileValid(img)) {
				SearchConfig.Cleanup();

				return false;
			}

			CliOutput.WriteInfo(RuntimeInfo.Config);

			string imgUrl = Search.Upload(img, useImgur);

			CliOutput.WriteInfo("Temporary image url: {0}", imgUrl);

			Console.WriteLine();

			//Console.ReadLine();

			//
			// Search
			//


			// Where the actual searching occurs

			Search.StartSearches(imgUrl, engines, res);


			return true;
		}


		private static bool StartSearches(string imgUrl, SearchEngines engines, SearchResults res)
		{
			ISearchEngine[] available = GetAvailableEngines()
			                           .Where(e => engines.HasFlag(e.Engine))
			                           .ToArray();

			int i = 0;
			res.Results    = new SearchResult[available.Length + 1];
			res.Results[i] = new SearchResult(imgUrl, "(Original image)");

			i++;

			foreach (var idx in available) {
				string wait = String.Format("{0}: ...", idx.Engine);

				CliOutput.WithColor(ConsoleColor.Blue, () =>
				{
					//
					Console.Write(wait);
				});


				// Run search
				var result = idx.GetResult(imgUrl);

				if (result != null) {
					string url = result.Url;


					if (url != null) {
						CliOutput.OnCurrentLine(ConsoleColor.Green, "{0}: Done\n", result.Name);

						if (RuntimeInfo.Config.PriorityEngines.HasFlag(idx.Engine)) {
							WebAgent.OpenUrl(result.Url);
						}
					}
					else {
						CliOutput.OnCurrentLine(ConsoleColor.Yellow, "{0}: Done (url is null!)\n", result.Name);
					}

					res.Results[i] = result;
				}

				// todo

				i++;
			}


			return true;
		}

		private static bool IsFileValid(string img)
		{
			if (!File.Exists(img)) {
				CliOutput.WriteError("File does not exist: {0}", img);
				return false;
			}

			bool extOkay = ImageExtensions.Any(img.EndsWith);

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