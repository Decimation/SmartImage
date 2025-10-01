namespace SmartImage.Lib.Engines.Search.Other;

public sealed class GoogleImagesEngine : BaseSearchEngine
{

	public GoogleImagesEngine() : base("https://lens.google.com/uploadbyurl?url=") { }

	public override string Name => "Google Images";

	public override SearchEngineOptions EngineOption => SearchEngineOptions.GoogleImages;

	public override ValueTask<bool> ApplyConfigAsync(SearchConfig cfg, CancellationToken ct = default)
	{
		return ValueTask.FromResult(true);

	}
	#region Overrides of BaseSearchEngine

	public override void Dispose() { }

	#endregion

	// https://html-agility-pack.net/knowledge-base/2113924/how-can-i-use-html-agility-pack-to-retrieve-all-the-images-from-a-website-

}