using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using Flurl.Http;

namespace SmartImage.Lib.Engines
{
	public abstract class WebContentSearchEngine : BaseSearchEngine
	{
		protected virtual async Task<IDocument> ParseContent(Url origin)
		{
			var parser  = new HtmlParser();
			var readStr = await origin.GetStringAsync();

			var document = parser.ParseDocument(readStr);

			return document;
		}

		protected WebContentSearchEngine(string baseUrl) : base(baseUrl) { }

	}
}