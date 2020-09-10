#region

using SmartImage.Searching;

#endregion

namespace SmartImage.Engines.Simple
{
	public sealed class GoogleImages : SimpleSearchEngine
	{
		public GoogleImages() : base("http://images.google.com/searchbyimage?image_url=") { }

		public override string Name => "Google Images";

		public override SearchEngines Engine => SearchEngines.GoogleImages;
	}
}