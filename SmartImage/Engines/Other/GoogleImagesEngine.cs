using System.Drawing;

namespace SmartImage.Engines.Other
{
	public sealed class GoogleImagesEngine : BasicSearchEngine
	{
		public GoogleImagesEngine() : base("http://images.google.com/searchbyimage?image_url=") { }

		public override string Name => "Google Images";

		public override SearchEngineOptions Engine => SearchEngineOptions.GoogleImages;

		public override Color Color => Color.CornflowerBlue;
	}
}