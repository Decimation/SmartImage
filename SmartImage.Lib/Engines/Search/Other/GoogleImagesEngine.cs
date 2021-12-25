using SmartImage.Lib.Engines.Search.Base;
using SmartImage.Lib.Searching;

namespace SmartImage.Lib.Engines.Search.Other;

public sealed class GoogleImagesEngine : BaseSearchEngine
{
	public GoogleImagesEngine() : base("http://images.google.com/searchbyimage?image_url=") { }

	public override string Name => "Google Images";

	public override SearchEngineOptions EngineOption => SearchEngineOptions.GoogleImages;

	public override EngineSearchType SearchType => EngineSearchType.Image;



	// https://html-agility-pack.net/knowledge-base/2113924/how-can-i-use-html-agility-pack-to-retrieve-all-the-images-from-a-website-
}