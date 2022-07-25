global using CBN = JetBrains.Annotations.CanBeNullAttribute;
global using NN = System.Diagnostics.CodeAnalysis.NotNullAttribute;
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
	public string Value => Resource.Value;

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
	public Uri UploadUri { get; set; }

	/// <summary>
	/// UploadAsync engine used for uploading the input file; if applicable
	/// </summary>
	public BaseUploadEngine UploadEngine { get; set; }

	public Stream Stream => Resource.Stream;

	public TimeSpan UploadTime { get; private set; }

	public ResourceHandle Resource { get; set; }

	public Image Image { get; }

	public ImageResult AsImageResult { get; }

	public ImageQuery([NotNull]  ResourceHandle o, BaseUploadEngine e = null)
	{

		Resource     = o;
		// Value        = o.Value;
		UploadEngine = e;

		// Stream = Resource.Stream;

		/*
		var client = new HttpClient(); //todo
		Stream     = IsFile ? File.OpenRead(value) : client.GetStream(value);*/

		// Trace.WriteLine($"{nameof(ImageQuery)}: {UploadUri}", C_SUCCESS);

		// Resource = o;

		Image = Image.FromStream(Stream);

		AsImageResult = new ImageResult(null)
		{
			// Image = Image.FromStream(Stream),
			OtherMetadata =
			{
				// { "UploadAsync engine", UploadEngine.Name },
				{ "Input type", IsUri ? "URI" : "File" },
				{ "Input value", Value },
				// { "Time", $"(upload: {UploadTime.TotalSeconds:F3})" }
			},
			Width  = Image.Width,
			Height = Image.Height
		};

		AsImageResult.DirectImages.Add(o);
	}

	public async Task<Uri> UploadAsync(BaseUploadEngine engine = null)
	{
		var now = Stopwatch.GetTimestamp();
		//note: default upload engine
		UploadEngine = engine ?? new LitterboxEngine();

		if (IsFile) {
			var task = await UploadEngine.UploadFileAsync(Value);

			UploadUri = task;
		}
		else {
			if (IsUri) {
				UploadUri = new Uri(Value);
			}

		}

		AsImageResult.Url = UploadUri;

		UploadTime = TimeSpan.FromTicks(Stopwatch.GetTimestamp() - now);

		return UploadUri;
	}

	public static async Task<ResourceHandle> TryAllocHandleAsync(string value)
	{
		ResourceHandle o = null;

		if (String.IsNullOrWhiteSpace(value)) {
			// throw new ArgumentNullException(nameof(value), "No input specified");
			return null;
		}

		value = value.CleanString();

		o = await ResourceHandle.GetAsync(value);

		o?.Resolve();

		// Resource = o;

		//www.youtube.com/watch?v=Ja_3FNMTsD8if
		if (o is { IsBinary: false }) {
			var errStr = !o.IsFile ? "Invalid file" : "Invalid URI";
			// throw new ArgumentException($"Input error: {errStr}");

			return null;
		}

		return o;
	}

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