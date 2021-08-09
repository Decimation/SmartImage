using System;
using System.Diagnostics;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
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
			return parser.ParseDocument(response.Content);
		}

		
	}
}