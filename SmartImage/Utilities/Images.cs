using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
//using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Novus.Win32;
using SimpleCore.Cli;
using SimpleCore.Net;
using SimpleCore.Utilities;
using SmartImage.Core;
using SmartImage.Engines;
using SmartImage.Engines.Imgur;
using SmartImage.Engines.Other;

#nullable enable
namespace SmartImage.Utilities
{
	/// <summary>
	/// Image utilities
	/// </summary>
	public static class Images
	{
		/* Image comparison algorithms:
		 * - ImageHash: too high?
		 * - Shipwreck phash: cross correlation
		 */


		public static (int Width, int Height) GetDimensions(Bitmap bmp)
		{
			return (bmp.Width, bmp.Height);
		}


		public static bool IsInputImageValid(string? imageInput) => IsInputImageValid(imageInput, out _);

		public static bool IsInputImageValid(string? imageInput, out ImageInputInfo info)
		{
			info = new ImageInputInfo();

			if (String.IsNullOrWhiteSpace(imageInput)) {
				return false;
			}

			info.IsFile = File.Exists(imageInput);
			info.IsUrl  = Network.IsUri(imageInput, out _) && !info.IsFile;

			if (info.IsUrl) {
				var isUriFile = MediaTypes.IsDirect(imageInput, MimeType.Image);
				
				if (!isUriFile) {
					return false;
				}
			}

			info.Value = info.IsFile ? new FileInfo(imageInput) : MediaTypes.Identify(imageInput)!;


			return info.IsValid;
		}

		/// <summary>
		/// Handles image input (either a URL or path) and returns the corresponding image URL
		/// </summary>
		public static ImageInputInfo? ResolveUploadUrl(string imageInput)
		{

			if (!IsInputImageValid(imageInput, out var info)) {
				return null;
			}


			Debug.WriteLine($"{info}");


			string? imgUrl = !info.IsUrl ? Upload(imageInput) : imageInput;

			Debug.WriteLine($"URL --> {imgUrl}");

			info.ImageUrl = imgUrl;

			return info;
		}

		/// <summary>
		/// Uploads the image
		/// </summary>
		public static string? Upload(string img)
		{

			IUploadEngine uploadEngine;

			string imgUrl;

			/*
			 * Show settings 
			 */
			var sb = new StringBuilder();
			sb.AppendColor(Interface.ColorPrimary, Info.NAME_BANNER);
			sb.Append(SearchConfig.Config);

			sb.AppendLine();

			/*
			 * Upload
			 */
			sb.AppendLine("Uploading image");


			if (SearchConfig.Config.UseImgur) {
				try {
					sb.AppendLine("Using Imgur for image upload");
					uploadEngine = new ImgurClient();
					imgUrl       = uploadEngine.Upload(img);
				}
				catch (Exception e) {
					sb.AppendLine($"Error uploading with Imgur: {e.Message}");
					sb.AppendLine("Using ImgOps instead");
					UploadImgOps();
				}
			}
			else {
				UploadImgOps();
			}


			void UploadImgOps()
			{
				sb.AppendLine("Using ImgOps for image upload (1 hour cache)");
				uploadEngine = new ImgOpsEngine();
				imgUrl       = uploadEngine.Upload(img);
			}

			sb.AppendLine($"Temporary image url: {imgUrl}");

			NConsole.Write(sb);


			return imgUrl;
		}
	}

	public class ImageInputInfo
	{
		public bool IsFile { get; internal set; }

		public bool IsUrl { get; internal set; }

		public object Value { get; internal set; }

		public string ImageUrl { get; internal set; }

		public bool IsValid => IsFile || IsUrl;

		public override string ToString()
		{
			return Value switch
			{
				FileInfo fi => fi.Name,
				string s    => s,
			};
		}
	}
}