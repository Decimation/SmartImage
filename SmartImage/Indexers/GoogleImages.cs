using SmartImage.Model;

namespace SmartImage.Indexers
{
	public sealed class GoogleImages : QuickIndexer
	{
		public GoogleImages(string baseUrl) : base(baseUrl) { }
		
		public static GoogleImages Value { get; private set; } = new GoogleImages("http://images.google.com/searchbyimage?image_url=");
	}
}