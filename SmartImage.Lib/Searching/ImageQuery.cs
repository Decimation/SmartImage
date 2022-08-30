global using CBN = JetBrains.Annotations.CanBeNullAttribute;
global using NN = System.Diagnostics.CodeAnalysis.NotNullAttribute;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Flurl.Http;
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
	/// Original query
	/// </summary>
	public string Query => Resource.Value;

	public ResourceHandle Resource { set; get; }

	/// <summary>
	/// Whether <see cref="Resource"/> is a file
	/// </summary>
	public bool IsFile => Resource.IsFile;

	/// <summary>
	/// Whether <see cref="Resource"/> is an image link
	/// </summary>
	public bool IsUri => Resource.IsUri;

	/// <summary>
	/// Uploaded direct image
	/// </summary>
	[MN]
	public Uri UploadUri { get; set; }

	public ImageQuery([JetBrains.Annotations.NotNull] ResourceHandle value)
	{
		Resource     = value;

	}

	/// <summary>
	/// <see cref="System.Drawing.Image"/> of <see cref="Query"/>
	/// </summary>
	public ImageResult ToImageResult()
	{
		var i = Image.FromStream(Resource.Stream);

		var ir = new ImageResult(null)
		{
			OtherMetadata =
			{
				{ "Input type", IsUri ? "URI" : "File" },
				{ "Input value", Query },
			},
			Width  = i.Width,
			Height = i.Height,
			Url    = UploadUri

		};

		ir.DirectImages.Add(Resource);

		return ir;

	}

	public bool IsUploaded => Resource is { } && UploadUri is { };

	public async Task<Uri> UploadAsync(BaseUploadEngine uploadEngine = null)
	{
		uploadEngine ??= new LitterboxEngine();

		if (IsFile) {
			var uri = await uploadEngine.UploadFileAsync(Query);

			UploadUri = uri;
		}
		else {
			if (IsUri) {
				UploadUri = new Uri(Query);
			}
		}

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

		// Resource = value;

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
		return $"{Query} | {UploadUri}";
	}

	public void Dispose()
	{

		Resource?.Dispose();
		// Resource  = null;
		// UploadUri = null;
		// Stream?.Dispose();
	}

	public ConsoleOption GetConsoleOption()
	{
		var ir = ToImageResult(); //todo
		return ir.GetConsoleOption("(Original image)", Color.Red.ChangeBrightness(-0.1f));
	}
}