using System.IO;
using System.Linq;
using SmartImage.Engines;
using SmartImage.Utilities;

namespace SmartImage
{
	internal static class Images
	{
		private static readonly string[] ImageExtensions =
		{
			".jpg", ".jpeg", ".png", ".gif", ".tga", ".jfif"
		};

		public static bool IsFileValid(string img)
		{
			if (!File.Exists(img)) {
				Cli.WriteError("File does not exist: {0}", img);
				return false;
			}

			bool extOkay = ImageExtensions.Any(img.EndsWith);

			if (!extOkay) {
				return Cli.Confirm("File extension is not recognized as a common image format. Continue?");
			}


			return true;
		}

		public static string Upload(string img, bool useImgur)
		{
			string imgUrl;

			if (useImgur) {
				Cli.WriteInfo("Using Imgur for image upload");
				var imgur = new Imgur();
				imgUrl = imgur.Upload(img);
			}
			else {
				Cli.WriteInfo("Using ImgOps for image upload (2 hour cache)");
				var imgOps = new ImgOps();
				imgUrl = imgOps.UploadTempImage(img, out _);
			}


			return imgUrl;
		}
	}
}