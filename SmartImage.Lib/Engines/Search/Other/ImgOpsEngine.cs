namespace SmartImage.Lib.Engines.Search.Other;

public sealed class ImgOpsEngine : BaseSearchEngine
{

	public ImgOpsEngine() : base("https://imgops.com/") { }

	public override SearchEngineOptions Option => SearchEngineOptions.ImgOps;
	
	public override void Dispose() { }
	
}