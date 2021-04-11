using System;
using System.Diagnostics;
using System.IO;
using JetBrains.Annotations;
using SimpleCore.Net;
using SmartImage.Lib.Engines.Impl;
using SmartImage.Lib.Utilities;

namespace SmartImage.Lib.Searching
{
	public class ImageQuery
	{
		public string Value { get; init; }

		public bool IsFile { get; }

		public bool IsUrl { get; }

		public Uri Uri { get; }

		public ImageQuery([NotNull] string value)
		{
			if (String.IsNullOrWhiteSpace(value)) {
				throw new ArgumentNullException(nameof(value));
			}

			Value = value;

			IsFile = File.Exists(value);

			if (!IsFile) {
				IsUrl = ImageUtilities.IsDirectImage(value);
			}

			if (!IsUrl && !IsFile) {
				throw new ArgumentException($"{value} is neither file nor direct image link");
			}


			Uri = IsUrl ? new(Value) : ImgOpsEngine.QuickUpload(Value);
		}

		public static implicit operator ImageQuery(string value) => new(value);


		public override string ToString()
		{
			return $"{Value} | {Uri}";
		}
	}
}