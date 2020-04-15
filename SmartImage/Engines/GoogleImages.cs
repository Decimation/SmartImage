#region

using SmartImage.Model;
using SmartImage.Searching;

#endregion

namespace SmartImage.Engines
{
	public sealed class GoogleImages : QuickSearchEngine
	{
		public GoogleImages() : base("http://images.google.com/searchbyimage?image_url=") { }

		public override string Name => "Google Images";

		public override SearchEngines Engine => SearchEngines.GoogleImages;
	}
}