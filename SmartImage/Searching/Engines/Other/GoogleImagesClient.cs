#region

using System.Drawing;
using SmartImage.Searching.Model;

#endregion

namespace SmartImage.Searching.Engines.Other
{
	public sealed class GoogleImagesClient : BasicSearchEngine
	{
		public GoogleImagesClient() : base("http://images.google.com/searchbyimage?image_url=") { }

		public override string Name => "Google Images";

		public override SearchEngines Engine => SearchEngines.GoogleImages;

		public override Color Color => Color.CornflowerBlue;
	}
}