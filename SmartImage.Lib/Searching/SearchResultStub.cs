using System;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using RestSharp;

namespace SmartImage.Lib.Searching
{
	public class SearchResultStub
	{
		public IRestResponse InitialResponse { get; init; }

		public TimeSpan Retrieval { get; init; }

		public bool InitialSuccess { get; init; }

		public Uri RawUri { get; init; }

		public IDocument GetDocument()
		{
			var parser = new HtmlParser();

			var document = parser.ParseDocument(InitialResponse.Content);

			return document;
		}
	}
}