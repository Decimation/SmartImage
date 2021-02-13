using System;
using System.IO;
using JetBrains.Annotations;
using SimpleCore.Net;

namespace SmartImage.Searching
{
	/// <summary>
	/// Contains image input info
	/// </summary>
	public class ImageInputInfo
	{
		public bool IsFile { get; internal set; }

		public bool IsUrl { get; internal set; }

		public object Value { get; internal set; }

		public string ImageUrl { get; internal set; }

		public bool IsValid => IsFile || IsUrl;

		public Stream Stream { get; internal set; }

		public ImageInputInfo()
		{
			Value    = null;
			ImageUrl = null; //todo
		}

		public override string ToString()
		{
			return Value switch
			{
				FileInfo fi => fi.Name,
				string s    => s,
				_           => throw new ArgumentOutOfRangeException()
			};
		}


		/// <summary>
		/// Attempts to create an <see cref="ImageInputInfo"/>
		/// </summary>
		/// <returns><c>true</c> if creation was successful and <paramref name="imageInput"/> is valid input; <c>false</c> otherwise</returns>
		public static bool TryCreate([CanBeNull] string imageInput, out ImageInputInfo info)
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

			info.Value  = info.IsFile ? new FileInfo(imageInput) : MediaTypes.Identify(imageInput)!;
			info.Stream = info.IsFile ? File.OpenRead(imageInput) : Network.GetStreamFromUrl(imageInput);

			return info.IsValid;
		}
	}
}