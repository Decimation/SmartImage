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
	/// <summary>
	/// Search query
	/// </summary>
	public sealed class ImageQuery
	{
		/// <summary>
		/// Original input
		/// </summary>
		public string Value { get; init; }

		/// <summary>
		/// Whether <see cref="Value"/> is a file
		/// </summary>
		public bool IsFile { get; }

		/// <summary>
		/// Whether <see cref="Value"/> is an image link
		/// </summary>
		public bool IsUrl { get; }

		/// <summary>
		/// Uploaded direct image
		/// </summary>
		public Uri Uri { get; }

		/// <summary>
		/// Upload engine used for uploading the input file; if applicable
		/// </summary>
		public IUploadEngine UploadEngine { get; }

		
		public ImageQuery([NotNull] string value, [CanBeNull] IUploadEngine engine = null)
		{
			if (String.IsNullOrWhiteSpace(value)) {
				throw new ArgumentNullException(nameof(value));
			}

			value = value.Trim('\"');

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


			Trace.WriteLine($"[success] {nameof(ImageQuery)}: {Uri}");
		}


		public static implicit operator ImageQuery(Uri value) => new(value.ToString());
		public static implicit operator ImageQuery(string value) => new(value);


		public override string ToString()
		{
			return $"{Value} | {Uri}";
		}
	}
}