using System.Diagnostics;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using Kantan.Net;
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
			return TryProcess(base.GetResult(query), sr =>
			{
				IDocument doc = GetDocument(sr);
				sr = Process(doc, sr);
				return sr;
			});
		}

		protected virtual IDocument GetDocument(SearchResult sr)
		{
			string response = WebUtilities.GetString(sr.RawUri.ToString()!);

			var parser = new HtmlParser();
			return parser.ParseDocument(response);
		}

		protected abstract SearchResult Process(IDocument doc, SearchResult sr);
	}
}