namespace SmartImage.Lib.Engines.Impl.Search.Other;

public sealed class ImgOpsEngine : BaseSearchEngine
{
    public ImgOpsEngine() : base("https://imgops.com/") { }

    public override SearchEngineOptions EngineOption => SearchEngineOptions.ImgOps;

    public override void Dispose() { }
}