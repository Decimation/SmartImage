namespace SmartImage.Lib.Engines.Impl.Other
{
	public sealed class GoogleImagesEngine : SearchEngine
	{
		public GoogleImagesEngine() : base("http://images.google.com/searchbyimage?image_url=") { }

		public override string Name => "Google Images";

		public override SearchEngineOptions Engine => SearchEngineOptions.GoogleImages;

		


		// https://html-agility-pack.net/knowledge-base/2113924/how-can-i-use-html-agility-pack-to-retrieve-all-the-images-from-a-website-
	}
}