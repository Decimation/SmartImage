using System.Diagnostics;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using RestSharp;
using SmartImage.Lib.Searching;

namespace SmartImage.Lib.Engines
{
	/// <summary>
	///     Represents a search engine whose results are parsed.
	/// </summary>
	public abstract class InterpretedSearchEngine : BaseSearchEngine
	{
		protected InterpretedSearchEngine(string baseUrl) : base(baseUrl) { }

		public abstract override SearchEngineOptions EngineOption { get; }

		public abstract override string Name { get; }

		

		[DebuggerHidden]
		public override SearchResult GetResult(ImageQuery query)
		{
			return TryProcess(base.GetPreliminaryResult(query, out var response), sr =>
			{
				//IDocument doc = GetDocument(sr);
				var doc = GetDocument(response);
				sr = Process(doc, sr);
				return sr;
			});
		}

		protected virtual IDocument GetDocument(IRestResponse response)
		{
			
			var parser = new HtmlParser();
			return parser.ParseDocument(response.Content);
		}

		protected abstract SearchResult Process(IDocument doc, SearchResult sr);
	}
}