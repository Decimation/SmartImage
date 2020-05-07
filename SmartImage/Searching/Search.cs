#region

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

		public static ISearchEngine[] GetAvailableEngines()
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
				new TraceMoe(),
				new KarmaDecay(),
				new Yandex(),
				new Bing()
			};

			engines.AddRange(others);

			return engines.ToArray();
		}


		public static SearchResult[] RunSearches(string imgUrl, SearchEngines engines)
		{
			var list = new List<SearchResult>
			{
				new SearchResult(imgUrl, "(Original image)")
			};


			ISearchEngine[] available = GetAvailableEngines();

			foreach (var idx in available) {
				if (engines.HasFlag(idx.Engine)) {
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

						list.Add(result);
					}
				}
			}


			return list.ToArray();
		}

		internal static bool IsFileValid(string img)
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

		public static string Upload(string img, bool useImgur)
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