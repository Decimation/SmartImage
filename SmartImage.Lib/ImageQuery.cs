using System;
using System.Diagnostics;
using System.IO;
using JetBrains.Annotations;
using SimpleCore.Net;

namespace SmartImage.Lib
{
	public class ImageQuery
	{
		public string Value { get; init; }

		public bool IsFile { get; }

		public bool IsUrl { get; }

		public string Url { get; }
		public ImageQuery([NotNull] string value)
		{
			if (string.IsNullOrWhiteSpace(value)) {
				throw new ArgumentNullException(nameof(value));
			}

			Value = value;

			// todo: direct image support

			IsFile = File.Exists(value);
			IsUrl  = Network.IsUri(value, out _) && !IsFile;

			if (IsUrl) {
				var isUriFile = MediaTypes.IsDirect(value, MimeType.Image);

				Debug.WriteLine($"{value}: {isUriFile} {MediaTypes.Identify(value)}");

				if (!isUriFile) {
					throw new ArgumentException();
				}
			}

			//info.Value = info.IsFile ? new FileInfo(imageInput) : MediaTypes.Identify(imageInput)!;

			Url = IsUrl ? Value : ImgOpsEngine.QuickUpload(Value);
		}

		public static implicit operator ImageQuery(string value) => new ImageQuery(value);


		public override string ToString()
		{
			return $"{Value} | {Url}";
		}
	}
}