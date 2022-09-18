using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AngleSharp.Dom;
using Flurl.Http;
using Kantan.Net.Utilities;

//todo
namespace SmartImage.Lib.Engines.Search
{
	public sealed class KarmaDecayEngine : WebContentSearchEngine
	{
		public KarmaDecayEngine() : base("http://karmadecay.com/search/?q=") { }

		#region Overrides of BaseSearchEngine

		public override SearchEngineOptions EngineOption => SearchEngineOptions.KarmaDecay;

		public override void Dispose() { }

		#endregion

		#region Overrides of WebContentSearchEngine

		#region Overrides of BaseSearchEngine

		#endregion

		protected override async Task<IDocument> ParseDocumentAsync(Url origin)
		{
			/*var res = await origin.WithHeaders(new { User_Agent = HttpUtilities.UserAgent })
			                      .AllowAnyHttpStatus()
			                      .WithTimeout(TimeSpan.FromSeconds(5))
			                      .WithCookies(out var cj)
			                      .WithAutoRedirect(true)
			                      .GetAsync();*/

			return await base.ParseDocumentAsync(origin);
		}

		protected override async Task<IList<INode>> GetNodesAsync(IDocument doc)
		{
			var results = doc.QuerySelectorAll("tr.result").Cast<INode>().ToList();

			return await Task.FromResult(results);
		}

		protected override async Task<SearchResultItem> ParseResultItemAsync(INode n, SearchResult r)
		{

			return await Task.FromResult<SearchResultItem>(null);
		}

		#endregion
	}
}