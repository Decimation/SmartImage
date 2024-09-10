// Author: Deci | Project: SmartImage.Lib | Name: UniImage.cs
// Date: 2024/05/02 @ 10:05:55

global using MURV = JetBrains.Annotations.MustUseReturnValueAttribute;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.Text;
using System.Threading.Channels;
using JetBrains.Annotations;
using Novus.FileTypes;
using Novus.FileTypes.Uni;
using Novus.Streams;
using Novus.Win32;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SmartImage.Lib.Model;

namespace SmartImage.Lib.Images.Uni;
#nullable disable

// #nullable enable
/// <summary>
/// <seealso cref="UniSourceType"/>
/// </summary>
public enum UniImageType
{

	Unknown = 0,
	File,
	Uri,
	Stream

}

/// <summary>
/// <seealso cref="UniSource"/>
/// </summary>
public abstract class UniImage : IItemSize, IDisposable, IAsyncDisposable, IEquatable<UniImage>
{

	[MN]
	public Stream Stream { get; protected set; }

	[MNNW(true, nameof(Stream))]
	public bool HasStream => Stream != Stream.Null;

	public object Value { get; protected init; }

	public UniImageType Type { get; }

	public long Size
	{
		get
		{
			if (HasStream && Stream.CanRead) {
				return Stream.Length;
			}

			return Native.ERROR_SV;
		}
	}

	[MN]
	public string ValueString => Value?.ToString();

	[MN]
	public string FilePath { get; private set; }

	[MNNW(true, nameof(FilePath))]
	public bool HasFile => FilePath != null && File.Exists(FilePath);

	[MN]
	public IImageFormat ImageFormat { get; private set; }

	[MNNW(true, nameof(ImageFormat))]
	public bool HasImageFormat => ImageFormat != null;

	/*[MNNW(true, nameof(Image), nameof(Image.Metadata))]
	public bool HasImageFormat => HasImage && Image.Metadata.DecodedImageFormat != null;*/

	public bool IsUri => Type == UniImageType.Uri;

	public bool IsFile => Type == UniImageType.File;

	public bool IsStream => Type == UniImageType.Stream;

	public bool IsUnknown => Type == UniImageType.Unknown;

	[MN]
	public ISImage Image { get; protected set; }

	[MNNW(true, nameof(Image))]
	public bool HasImage => Image != null;

	public static readonly UniImage Null = new UniImageUnknown(); // todo

	private protected UniImage(object value, UniImageType type)
		: this(value, Stream.Null, type) { }

	private protected UniImage(object value, Stream stream, UniImageType type)
	{
		Stream = stream;
		Value  = value;
		Type   = type;
	}

	#region

	/// <summary>
	/// Attempts to create the appropriate <see cref="UniImage" /> for <paramref name="o" />.
	/// </summary>
	public static async Task<UniImage> TryCreateAsync(object o, bool autoInit = true,
	                                                  bool autoInitNull = false,
	                                                  CancellationToken ct = default)
	{
		UniImage ui = Null;

		try {

			if (UniImageFile.IsFileType(o, out var fi)) {
				ui = new UniImageFile((string) o, fi);
			}
			else if (UniImageUri.IsUriType(o, out var url2)) {

				ui = new UniImageUri(o, url2);
			}
			else if (o is Stream stream) {
				ui = new UniImageStream(o, stream);
			}
			else {

				goto ret;
			}

			if (autoInit) {
				var allocOk = await ui.Alloc(ct);

				// var allocImgOk = await ui.AllocImage(ct);

				var hasInfo = await ui.DetectFormat(ct);

				if (autoInitNull) {
					if (!allocOk || !hasInfo) {
						ui.Dispose();
						ui = Null;

					}

				}
			}

		}
		catch (Exception e) {
			// str?.Dispose();
		}

	ret:
		return ui;
	}

	public virtual async ValueTask<bool> DetectFormat(CancellationToken ct = default)
	{
		if (!HasStream) {
			throw new InvalidOperationException($"{nameof(Stream)} must be allocated");
		}

		try {
			Stream.TrySeek();
			ImageFormat = await ISImage.DetectFormatAsync(Stream, ct);

			Stream.TrySeek();

		}
		finally { }

		return HasImageFormat;

	}

	public static bool IsValidSourceType(object str, bool checkExt = true)
	{
		// UniSourceType v        = UniHandler.GetUniType(str, out object o2);
		/*bool isFile   = UniSourceFile.IsType(str, out var f);
		bool isUri    = UniSourceUrl.IsType(str, out var f2);
		bool isStream = UniSourceStream.IsType(str, out var f3);*/
		bool isFile   = UniImageFile.IsFileType(str, out var f);
		bool isUri    = UniImageUri.IsUriType(str, out var f2);
		bool isStream = UniImageStream.IsStreamType(str, out var f3);
		bool ok       = isFile || isUri || isStream;

		if (isFile && checkExt) {
			//todo
			string ext = Path.GetExtension(str.ToString())?[1..];
			return FileType.Image.Any(x => x.Subtype == ext);
		}

		return ok;
	}

	#region

	public abstract ValueTask<bool> Alloc(CancellationToken ct = default);

	/// <summary>
	/// Allocates <see cref="Image"/>
	/// </summary>
	public async Task<bool> AllocImage(CancellationToken ct = default)
	{
		if (!HasImage) {

			try {
				Stream.TrySeek(); //todo
				Image = await ISImage.LoadAsync(Stream, ct);
				Stream.TrySeek(); //todo
			}
			catch (Exception e) {
				Debug.WriteLine($"{e.Message}");
			}
			finally { }
		}

		return HasImage;
	}

	#endregion

	#endregion

	public bool TryGetFile(string fn = null)
	{
		if (!HasFile) {
			FilePath = WriteToFile(fn);

		}

		return HasFile;
	}

	public bool TryDeleteFile()
	{
		if (HasFile) {
			File.Delete(FilePath);
			FilePath = null;
		}

		return !HasFile;
	}

	[MURV]
	public string WriteToFile(Action<IImageProcessingContext> operation = null, [CBN] string fn = null)
	{

		string t;
		fn ??= Path.GetTempFileName();

		var encoder = new PngEncoder();
		operation ??= _ => { };

		// using ISImage image = ISImage.Load(Stream);
		using var image = Image.Clone(operation);

		image.Mutate(operation);


		image.Save(fn, encoder);

		Stream.TrySeek();

		return fn;
	}

	[MURV]
	[ICBN]
	public string WriteToFile(string fn = null)
	{

		string t;
		fn ??= Path.GetTempFileName();

		if (Type != UniImageType.File) {
			t = Path.Combine(Path.GetTempPath(), fn);

			using FileStream fs = File.Create(t);

			bool s = Stream.CanSeek;

			if (s) {
				Stream.Position = 0;
			}

			Stream.CopyTo(fs);
			fs.Flush();
			Stream.TrySeek();

		}
		else {
			t = Value.ToString();

		}

		return t;
	}

	public virtual void Dispose()
	{
		Stream?.Dispose();
		Image?.Dispose();

		// ImageInfo?.Dispose();
		Debug.WriteLine($"Disposing {ValueString} w/ {Size}");
	}

	public virtual async ValueTask DisposeAsync()
	{
		if (Stream != null)
			await Stream.DisposeAsync();

		Image?.Dispose();

	}

	public override string ToString()
	{
		string s = $"{ValueString} ({Type}) [{(HasImageFormat ? ImageFormat : "?")}]";

		return s;
	}

	#region Equality members

	public bool Equals(UniImage other)
	{
		if (ReferenceEquals(null, other)) return false;
		if (ReferenceEquals(this, other)) return true;

		return Equals(Value, other.Value);
	}

	public override bool Equals(object obj)
	{
		return ReferenceEquals(this, obj) || obj is UniImage other && Equals(other);
	}

	public override int GetHashCode()
	{
		// return HashCode.Combine(Uni, Upload, Size);
		return HashCode.Combine(Value);

		// return Uni.GetHashCode();
	}

	public static bool operator ==(UniImage left, UniImage right)
	{
		return Equals(left, right);
	}

	public static bool operator !=(UniImage left, UniImage right)
	{
		return !Equals(left, right);
	}

	#endregion

}