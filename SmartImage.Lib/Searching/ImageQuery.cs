using System;
using System.Diagnostics;
using System.IO;
using JetBrains.Annotations;
using SimpleCore.Net;
using SimpleCore.Utilities;
using SmartImage.Lib.Engines;
using SmartImage.Lib.Engines.Impl;
using SmartImage.Lib.Upload;
using SmartImage.Lib.Utilities;
using static SimpleCore.Diagnostics.LogCategories;

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
		public bool IsUri { get; }

		/// <summary>
		/// Uploaded direct image
		/// </summary>
		public Uri Uri { get; }

		/// <summary>
		/// Upload engine used for uploading the input file; if applicable
		/// </summary>
		public BaseUploadEngine UploadEngine { get; }

		public Stream Stream { get; }



		public ImageQuery([NotNull] string value, [CanBeNull] BaseUploadEngine engine = null)
		{
			if (String.IsNullOrWhiteSpace(value)) {
				throw new ArgumentNullException(nameof(value));
			}

			value = value.CleanString();

			Value = value;

			(IsUri, IsFile) = IsUriOrFile(value);

			if (!IsUri && !IsFile) {
				throw new ArgumentException("Input was neither file nor direct image link", nameof(value));
			}


			UploadEngine = engine ?? new LitterboxEngine(); //todo

			Uri = IsUri ? new Uri(Value) : UploadEngine.Upload(Value);

			Stream = IsFile ? File.OpenRead(value) : WebUtilities.GetStream(value);

			Trace.WriteLine($"{nameof(ImageQuery)}: {Uri}", C_SUCCESS);
		}


		public static implicit operator ImageQuery(Uri value) => new(value.ToString());

		public static implicit operator ImageQuery(string value) => new(value);

		public static (bool IsUri, bool IsFile) IsUriOrFile(string x)
		{
			x = x.CleanString();
			return (ImageHelper.IsDirect(x), File.Exists(x));
		}


		public override string ToString()
		{
			return $"{Value} | {Uri}";
		}
	}
}