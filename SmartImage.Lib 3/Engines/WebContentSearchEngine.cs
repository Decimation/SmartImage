using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;

namespace SmartImage_3.Lib.Engines
{
	public abstract class WebContentSearchEngine : BaseSearchEngine
	{
		protected virtual async Task<IDocument> ParseContent(SearchQuery origin)
		{
			var parser  = new HtmlParser();
			var readStr = await origin.Response.Content.ReadAsStringAsync();
			
			var content  = readStr.Result;
			var document = parser.ParseDocument(content);

			return document;
		}

		protected WebContentSearchEngine(string baseUrl) : base(baseUrl) { }
	}
}
