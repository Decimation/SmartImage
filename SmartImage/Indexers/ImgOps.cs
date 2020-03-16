using SmartImage.Model;

namespace SmartImage.Indexers
{
	public sealed class ImgOps : QuickIndexer
	{
		private ImgOps(string baseUrl) : base(baseUrl) { }

		public static ImgOps Value { get; private set; } = new ImgOps("http://imgops.com/");
	}
}