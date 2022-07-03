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

	public Stream Stream => Resource.Stream;

	public TimeSpan UploadTime { get; }

	public ResourceHandle Resource { get; set; }

	public Image Image { get; }

	public ImageResult AsImageResult { get; }

	public ImageQuery([NotNull] string value, [CanBeNull] BaseUploadEngine engine = null)
	{
		var now = Stopwatch.GetTimestamp();


		if (TryVerifyInput(value, out var o)) {
			Value    = value;
			Resource = o;

		}
		else {
			throw new SmartImageException();
		}

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

		// Stream = Resource.Stream;

		/*
		var client = new HttpClient(); //todo
		Stream     = IsFile ? File.OpenRead(value) : client.GetStream(value);*/
		UploadTime = TimeSpan.FromTicks(Stopwatch.GetTimestamp() - now);

		Trace.WriteLine($"{nameof(ImageQuery)}: {UploadUri}", C_SUCCESS);

		Resource = o;

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

		AsImageResult.DirectImages.Add(o);
	}

	public static bool TryVerifyInput(string value, out ResourceHandle o)
	{
		o = null;
		if (String.IsNullOrWhiteSpace(value)) {
			// throw new ArgumentNullException(nameof(value), "No input specified");
			return false;
		}

		value = value.CleanString();

		var di = ResourceHandle.GetAsync(value);
		di.Wait();

		o = di.Result;
		o?.Resolve();

		// Resource = o;

		if (!o.IsBinary) {
			var errStr = !o.IsFile ? "Invalid file" : "Invalid URI";
			// throw new ArgumentException($"Input error: {errStr}");

			return false;
		}

		return true;
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
		Resource.Dispose();
		// Stream?.Dispose();
		Image.Dispose();
	}
}