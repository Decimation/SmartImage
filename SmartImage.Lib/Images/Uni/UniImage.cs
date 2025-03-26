// Author: Deci | Project: SmartImage.Lib | Name: UniImage.cs
// Date: 2024/05/02 @ 10:05:55

global using MURV = JetBrains.Annotations.MustUseReturnValueAttribute;
using System.Diagnostics;
using Novus.FileTypes;
using Novus.FileTypes.Uni;
using Novus.Streams;
using Novus.Win32;
using CoenM.ImageHash;
using Kantan.Diagnostics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Processing;
using SmartImage.Lib.Results.Data;

#pragma warning disable CS0168 // Variable is declared but never used

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
public abstract class UniImage : IDisposable, IItemSize, IAsyncDisposable, IEquatable<UniImage>, ISimilarity, IHashable
{

	[MN]
	public Stream Stream { get; protected set; }

	[MNNW(true, nameof(Stream))]
	public bool HasStream => Stream != null && Stream != Stream.Null;

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
	public virtual string ValueString => Value?.ToString();

	[MN]
	public string FilePath { get; protected set; }

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

	public Lazy<ulong> Hash { get; }

	public double? Similarity { get; internal set; }


	public static readonly UniImage Null = null;

	private protected UniImage(object value, UniImageType type)
		: this(value, Stream.Null, type) { }

	private protected UniImage(object value, Stream stream, UniImageType type)
	{
		Stream = stream;
		Value  = value;
		Type   = type;
		Hash   = new Lazy<ulong>(TryCalculateHash, LazyThreadSafetyMode.ExecutionAndPublication);
	}

	#region

	/// <summary>
	/// Attempts to create the appropriate <see cref="UniImage" /> for <paramref name="o" />.
	/// </summary>
	public static async Task<UniImage> TryCreateAsync(object o, bool autoInit = true,
	                                                  bool autoDisposeOnError = true,
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
				var allocOk = await ui.AllocAsync(ct);

				// var allocImgOk = await ui.AllocImage(ct);

				// var hashOk = ui.TryCalculateHash();

				var hasInfo = await ui.DetectFormatAsync(ct);

				if (hasInfo) {
					ui.TryCalculateHash();
				}

				if (autoDisposeOnError) {
					if (!allocOk || !hasInfo) {
						ui.Dispose();
						ui = Null;

					}

				}
			}

		}
		catch (Exception e) {
			// str?.Dispose();
			Trace.WriteLine($"{nameof(TryCreateAsync)} :: failed with exception {e.Message}");
		}

	ret:
		return ui;
	}

	public virtual async ValueTask<bool> DetectFormatAsync(CancellationToken ct = default)
	{
		if (!HasStream) {
			throw new InvalidOperationException($"{nameof(Stream)} must be allocated");
		}

		try {
			Stream.TrySeek();
			ImageFormat = await ISImage.DetectFormatAsync(Stream, ct);
			Stream.TrySeek();

		}
		catch (UnknownImageFormatException ex) {
			Debug.WriteLine($"{this} :: {ex.Message}");
		}
		finally { }

		return HasImageFormat;

	}

	public static bool IsValidSourceType(object str, bool checkExt = true)
	{
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

	public abstract ValueTask<bool> AllocAsync(CancellationToken ct = default);

	/// <summary>
	/// Allocates <see cref="Image"/>
	/// </summary>
	public async Task<bool> AllocImageAsync(CancellationToken ct = default)
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

	private ulong TryCalculateHash()
	{
		if (!HasStream) {
			throw new InvalidOperationException();
		}

		ulong hash;

		try {
			Stream.TrySeek();
			hash = ImageScanner.ImageHasher.Hash(Stream);
			Stream.TrySeek();
		}
		catch (Exception e) {
			hash = IHashable.HASH_ERROR;
		}
		finally { }

		return hash;
	}

	public bool TryCalculateSimilarity(IHashable comparand)
	{
		if (!((IHashable) this).HasHash || !comparand.HasHash) {
			throw new InvalidOperationException();
		}

		Similarity ??= CompareHash.Similarity(comparand.Hash.Value, Hash.Value);

		return Similarity.HasValue;
	}

	public bool TryWriteToFile(string fn = null)
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
	public string WriteImageToFile([CBN] string fn = null, [CBN] Action<IImageProcessingContext> operation = null)
	{
		if (!HasImage) {
			throw new InvalidOperationException();
		}

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

	public abstract string WriteToFile(string fn = null);

	[MURV]
	[ICBN]
	protected string WriteStreamToFile(string fn = null)
	{
		fn ??= Path.GetTempFileName();

		string t = Path.Combine(Path.GetTempPath(), fn);

		using FileStream fs = File.Create(t);


		if (HasStream && Stream.CanSeek) {
			Stream.Position = 0;
		}

		Stream.CopyTo(fs);
		fs.Flush();
		Stream.TrySeek();

		return t;
	}


	public virtual void Dispose()
	{
		Trace.WriteLine($"Disposing {ValueString} w/ {Size}", LogCategories.C_VERBOSE);
		Stream?.Dispose();
		Image?.Dispose();


		// ImageInfo?.Dispose();
	}

	public virtual async ValueTask DisposeAsync()
	{
		Trace.WriteLine($"Disposing {ValueString} w/ {Size}", LogCategories.C_VERBOSE);

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
		if (other is null)
			return false;

		if (ReferenceEquals(this, other))
			return true;

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