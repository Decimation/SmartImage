using System;
using System.Diagnostics;
using System.IO;
using JetBrains.Annotations;
using SimpleCore.Net;
using SmartImage.Lib.Engines;
using SmartImage.Lib.Engines.Impl;
using SmartImage.Lib.Utilities;

namespace SmartImage.Lib.Searching
{
	
	public class ImageQuery
	{
		/// <summary>
		/// Original input
		/// </summary>
		public string Value { get; init; }

		public bool IsFile { get; }

		public bool IsUrl { get; }

		/// <summary>
		/// Uploaded direct image
		/// </summary>
		public Uri Uri { get; }

		public IUploadEngine UploadEngine { get; }

		public ImageQuery([NotNull] string value, [CanBeNull] IUploadEngine engine = null)
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


			UploadEngine = engine ?? new CatBoxEngine(); //todo

			Uri = IsUrl ? new(Value) : UploadEngine.Upload(Value);


			Trace.WriteLine($"{Uri}");
		}

		public static implicit operator ImageQuery(string value) => new(value);


		public override string ToString()
		{
			return $"{Value} | {Uri}";
		}
	}
}