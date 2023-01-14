
//todo
namespace SmartImage.Lib.Engines.Impl.Search.Other;

public sealed class KarmaDecayEngine : BaseSearchEngine
{
    public KarmaDecayEngine() : base("http://karmadecay.com/search/?q=") { }

    public override SearchEngineOptions EngineOption => SearchEngineOptions.KarmaDecay;

    public override void Dispose() { }

    /*protected override async Task<List<INode>> GetNodesAsync(IDocument doc)
	{
		var results = doc.QuerySelectorAll(NodesSelector).Cast<INode>().ToList();

		return await Task.FromResult(results);
	}*/
}