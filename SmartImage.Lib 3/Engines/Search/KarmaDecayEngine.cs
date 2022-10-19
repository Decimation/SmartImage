using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AngleSharp.Dom;
using Flurl.Http;
using Kantan.Net.Utilities;

//todo
namespace SmartImage.Lib.Engines.Search;

public sealed class KarmaDecayEngine : WebContentSearchEngine
{
	public KarmaDecayEngine() : base("http://karmadecay.com/search/?q=") { }

	public override SearchEngineOptions EngineOption => SearchEngineOptions.KarmaDecay;

	public override void Dispose() { }

	protected override string NodesSelector => "tr.result";

	protected override async Task<SearchResultItem> ParseResultItemAsync(INode n, SearchResult r)
	{
		throw new NotImplementedException();
	}

	/*protected override async Task<List<INode>> GetNodesAsync(IDocument doc)
	{
		var results = doc.QuerySelectorAll(NodesSelector).Cast<INode>().ToList();

		return await Task.FromResult(results);
	}*/
}