using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using Kantan.Utilities;
using RestSharp;
using SmartImage.Lib.Searching;

namespace SmartImage.Lib.Engines.Model
{
	/// <summary>
	///     Represents a search engine whose results are from HTML.
	/// </summary>
	public abstract class WebSearchEngine : ProcessedSearchEngine
	{
		protected WebSearchEngine(string baseUrl) : base(baseUrl) { }

		public abstract override SearchEngineOptions EngineOption { get; }

		public abstract override string Name { get; }

		protected internal virtual IDocument GetContent(IRestResponse response)
		{
			var parser = new HtmlParser();
			

			/*var s=AsyncHelpers.RunSync<string>(async () =>
			{
				var r = response.Content.ReadAsStringAsync();
				await r;
				return r.Result;
			});*/

			var s = response.Content;
			
			return parser.ParseDocument(s);
		}

	}
}