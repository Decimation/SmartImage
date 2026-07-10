//todo

using SmartImage.Lib.Engines.Search.Base;

namespace SmartImage.Lib.Engines.Search.Other;

[Obsolete]
public sealed class KarmaDecayEngine : BaseSearchEngine
{

	public KarmaDecayEngine() : base("http://karmadecay.com/search/?q=") { }

	public override SearchEngineOptions Option => SearchEngineOptions.KarmaDecay;


	

	public override void Dispose() { }

	/*protected override async Task<List<INode>> GetNodesAsync(IDocument doc)
	{
		var results = doc.QuerySelectorAll(NodesSelector).Cast<INode>().ToList();

		return await Task.FromResult(results);
	}*/

}