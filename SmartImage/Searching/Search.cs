using System;
using System.Collections.Generic;
using Neocmd;
using SmartImage.Engines;
using SmartImage.Engines.SauceNao;
using SmartImage.Engines.TraceMoe;
using SmartImage.Model;
using SmartImage.Utilities;

namespace SmartImage.Searching
{
	public static class Search
	{
		public static ISearchEngine[] GetAvailableEngines()
		{
			var engines = new List<ISearchEngine>()
			{
				new ImgOps(),
				new GoogleImages(),
				new TinEye(),
				new Iqdb(),
				new TraceMoe(),
				new KarmaDecay(),
			};

			var sauceNaoConfigured = !Config.SauceNaoAuth.IsNull;

			if (sauceNaoConfigured) {
				engines.Add(new SauceNao());
			}
			else {
				engines.Add(new BasicSauceNao());
			}

			return engines.ToArray();
		}


		public static SearchResult[] RunSearches(string imgUrl, SearchEngines engines)
		{
			var list = new List<SearchResult>
			{
				new SearchResult(imgUrl, "(Original image)")
			};


			var available = GetAvailableEngines();
			
			foreach (var idx in available) {
				if (engines.HasFlag(idx.Engine)) {
					string wait = string.Format("{0}: ...", idx.Engine);

					CliOutput.WithColor(ConsoleColor.Blue, () =>
					{
						//
						Console.Write(wait);
					});


					// Run search
					var result = idx.GetResult(imgUrl);

					if (result != null) {
						
						string clear = new string('\b', wait.Length);
						Console.Write(clear);

						var url = result.Url;

						if (url != null) {
							CliOutput.WithColor(ConsoleColor.Green, () =>
							{
								//
								Console.Write("{0}: Done\n", result.Name);
							});

							if (Config.PriorityEngines.HasFlag(idx.Engine)) {
								Http.OpenUrl(result.Url);
							}
						}
						else {
							CliOutput.WithColor(ConsoleColor.Yellow, () =>
							{
								//
								Console.Write("{0}: Done (url is null!)\n", result.Name);
							});
						}

						list.Add(result);
					}
					else { }
				}
			}


			return list.ToArray();
		}
	}
}