using JetBrains.Annotations;

namespace SmartImage.Utilities
{
	[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
	internal sealed class ImgurImage
	{
		public string Id { get; set; }

		public string Title { get; set; }

		public string Description { get; set; }

		public int Datetime { get; set; }


		public string Type { get; set; }

		public bool Animated { get; set; }

		public int Width { get; set; }

		public int Height { get; set; }

		public int Size { get; set; }

		public long Views { get; set; }

		public long Bandwidth { get; set; }

		public string Deletehash { get; set; }

		public object Section { get; set; }

		public string Link { get; set; }
	}
}