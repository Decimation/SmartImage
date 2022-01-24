using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Kantan.Cli.Controls;
using Kantan.Model;
using Kantan.Net;
using Kantan.Text;
using Kantan.Utilities;
using SmartImage.Lib.Engines;
using SmartImage.Lib.Engines.Upload;
using SmartImage.Lib.Engines.Upload.Base;
using SmartImage.Lib.Utilities;
using static Kantan.Diagnostics.LogCategories;

#pragma warning disable CA1416
namespace SmartImage.Lib.Searching;

/// <summary>
/// Search query
/// </summary>
public sealed class ImageQuery : IDisposable, IConsoleOption
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
	public Uri UploadUri { get; }

	/// <summary>
	/// Upload engine used for uploading the input file; if applicable
	/// </summary>
	public BaseUploadEngine UploadEngine { get; }

	public Stream Stream { get; }

	public TimeSpan UploadTime { get; }

	public ImageQuery([NotNull] string value, [CanBeNull] BaseUploadEngine engine = null)
	{
		var now = Stopwatch.GetTimestamp();

		if (String.IsNullOrWhiteSpace(value)) {
			throw new ArgumentNullException(nameof(value), "No input specified");
		}

		value = value.CleanString();

		(IsUri, IsFile) = IsUriOrFile(value);

		if (!IsUri && !IsFile) {
			throw new ArgumentException("Input was neither file nor direct image link", nameof(value));
		}

		Value = value;

		//note: default upload engine
		UploadEngine = engine ?? new LitterboxEngine();

		if (IsFile) {
			var task = UploadEngine.UploadFileAsync(Value);
			task.Wait();
			UploadUri = task.Result;
		}
		else if (IsUri) {
			UploadUri = new Uri(Value);
		}


		var client = new HttpClient();//todo
		Stream = IsFile ? File.OpenRead(value) : client.GetStream(value);


		UploadTime = TimeSpan.FromTicks(Stopwatch.GetTimestamp() - now);

		Trace.WriteLine($"{nameof(ImageQuery)}: {UploadUri}", C_SUCCESS);
	}

	public static implicit operator ImageQuery(Uri value) => new(value.ToString());

	public static implicit operator ImageQuery(string value) => new(value);

	public static (bool IsUri, bool IsFile) IsUriOrFile(string x)
	{
		//todo
		var isUriOrFile = (ImageHelper.IsBinaryImage(x, out var di,-1), File.Exists(x));
		// di?.Dispose();
		return isUriOrFile;
	}

	public ImageResult GetImageResult()
	{


		MediaResource directImage = new()
		{
			// Stream = Stream,
			Url = UploadUri
		};

		var result = new ImageResult(null)
		{
			
			// Image = Image.FromStream(Stream),
			Url = UploadUri
		};

		result.DirectImages.Add(directImage);

		result.OtherMetadata.Add("Upload engine", UploadEngine.Name);
		result.OtherMetadata.Add("Input type", IsUri ? "URI" : "File");
		result.OtherMetadata.Add("Input value", Value);
		result.OtherMetadata.Add("Time", $"(upload: {UploadTime.TotalSeconds:F3})");

		return result;
	}

	public override string ToString()
	{
		return $"{Value} | {UploadUri}";
	}

	public ConsoleOption GetConsoleOption()
	{
		return GetImageResult().GetConsoleOption("(Original image)", Color.Red.ChangeBrightness(-0.1f));
	}

	public void Dispose()
	{
		Stream?.Dispose();
	}
}