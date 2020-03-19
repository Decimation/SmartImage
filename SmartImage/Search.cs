using System.Collections.Generic;
using SmartImage.Engines;
using SmartImage.Engines.SauceNao;
using SmartImage.Engines.TraceMoe;
using SmartImage.Model;
using SmartImage.Utilities;

namespace SmartImage
{
	public static class Search
	{
		public static readonly ISearchEngine[] AllEngines =
		{
			new SauceNao(),
			new ImgOps(),
			new GoogleImages(),
			new TinEye(),
			new Iqdb(),
			new TraceMoe(),
			new KarmaDecay(),
		};


		public static SearchResult[] RunSearches(string imgUrl, SearchEngines engines)
		{
			var list = new List<SearchResult>
			{
				new SearchResult(imgUrl, "(Original image)")
			};


			foreach (var idx in AllEngines) {
				if (engines.HasFlag(idx.Engine)) {
					var result = idx.GetResult(imgUrl);

					if (result != null) {
						list.Add(result);

						if (Config.PriorityEngines.HasFlag(idx.Engine)) {
							Common.OpenUrl(result.Url);
						}
					}
					else { }
				}
			}

			return list.ToArray();
		}
	}
}