namespace SmartImage.Lib.Engines.Search.Other;

public sealed class ImgOpsEngine : BaseSearchEngine
{

	public ImgOpsEngine() : base("https://imgops.com/") { }

	public override SearchEngineOptions EngineOption => SearchEngineOptions.ImgOps;

	public override void Dispose() { }
	public override ValueTask<bool> ApplyConfigAsync(SearchConfig cfg, CancellationToken ct = default)
	{
		return ValueTask.FromResult(true);

	}
}