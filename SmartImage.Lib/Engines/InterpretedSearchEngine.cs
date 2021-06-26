using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using RestSharp;
using SimpleCore.Net;
using SmartImage.Lib.Searching;
using SmartImage.Lib.Utilities;
using static SimpleCore.Diagnostics.LogCategories;

namespace SmartImage.Lib.Engines
{
	/// <summary>
	/// Represents a search engine whose results are parsed.
	/// </summary>
	public abstract class InterpretedSearchEngine : BaseSearchEngine
	{
		public abstract override SearchEngineOptions EngineOption { get; }

		public abstract override string Name { get; }

		protected InterpretedSearchEngine(string baseUrl) : base(baseUrl) { }


		[DebuggerHidden]
		public override SearchResult GetResult(ImageQuery query)
		{
			var sr = base.GetResult(query);

			if (!sr.IsSuccessful) {
				return sr;
			}

			try {
				var doc = GetDocument(sr);

				sr = Process(doc, sr);
			}
			catch (Exception e) {
				sr.Status = ResultStatus.Failure;
				Trace.WriteLine($"{Name}: {e.Message}", C_ERROR);
			}

			return sr;
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