using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Flurl.Http;
using JetBrains.Annotations;
using Kantan.Cli.Controls;
using Kantan.Model;
using Kantan.Net;
using Kantan.Net.Content;
using Kantan.Net.Utilities;
using Kantan.Text;
using Kantan.Utilities;
using SmartImage.Lib.Engines;
using SmartImage.Lib.Engines.Upload;
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
	public bool IsFile => Resource.IsFile;

	/// <summary>
	/// Whether <see cref="Value"/> is an image link
	/// </summary>
	public bool IsUri => Resource.IsUri;

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

	public HttpResource Resource { get; }

	public Image Image { get; }

	public ImageResult AsImageResult { get; }

	public ImageQuery([NotNull] string value, [CanBeNull] BaseUploadEngine engine = null)
	{
		var now = Stopwatch.GetTimestamp();

		if (String.IsNullOrWhiteSpace(value)) {
			throw new ArgumentNullException(nameof(value), "No input specified");
		}

		value = value.CleanString();

		Resource = MediaHelper.GetMediaInfo(value);

		if (!Resource.IsBinary) {
			var errStr = !Resource.IsFile ? "Invalid file" : "Invalid URI";
			throw new ArgumentException($"Input error: {errStr}");
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

		var client = new HttpClient(); //todo
		Stream     = IsFile ? File.OpenRead(value) : client.GetStream(value);
		UploadTime = TimeSpan.FromTicks(Stopwatch.GetTimestamp() - now);

		Trace.WriteLine($"{nameof(ImageQuery)}: {UploadUri}", C_SUCCESS);

		HttpResource directImage = new()
		{
			// Stream = Stream,
			Url = UploadUri.ToString()
		};

		Image = Image.FromStream(Stream);
		AsImageResult = new ImageResult(null)
		{
			// Image = Image.FromStream(Stream),
			Url = UploadUri,
			OtherMetadata =
			{
				{ "Upload engine", UploadEngine.Name },
				{ "Input type", IsUri ? "URI" : "File" },
				{ "Input value", Value },
				{ "Time", $"(upload: {UploadTime.TotalSeconds:F3})" }
			},
			Width  = Image.Width,
			Height = Image.Height
		};

		AsImageResult.DirectImages.Add(directImage);
	}

	public static implicit operator ImageQuery(Uri value) => new(value.ToString());

	public static implicit operator ImageQuery(string value) => new(value);

	public override string ToString()
	{
		return $"{Value} | {UploadUri}";
	}

	public ConsoleOption GetConsoleOption()
	{
		return AsImageResult.GetConsoleOption("(Original image)", Color.Red.ChangeBrightness(-0.1f));
	}

	public void Dispose()
	{
		Stream?.Dispose();
		Image.Dispose();
		Resource.Dispose();
	}
}