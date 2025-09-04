// Author: Deci | Project: SmartImage.Lib | Name: UniImage.cs
// Date: 2024/05/02 @ 10:05:55


using System.Buffers;
using Kantan.Diagnostics;
using Microsoft.Extensions.Logging;
using Novus.FileTypes;
using Novus.FileTypes.Uni;
using Novus.Streams;
using Novus.Win32;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.Diagnostics;
using System.Runtime.InteropServices;
using CoenM.ImageHash;
using CommunityToolkit.HighPerformance;
using Microsoft.IO;
using SmartImage.Lib.Model;
using SmartImage.Lib.Utilities;

// ReSharper disable InconsistentNaming


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
	Uri

}

public enum SearchHashType
{

	None = 0,
	PHash,
	SHA256,
	MD5,
	Base64,
	Base64MD5,

}

/// <summary>
/// <seealso cref="UniSource"/>
/// </summary>	
public abstract class UniImage : IDisposable, ISize, IAsyncDisposable, IEquatable<UniImage>, ISimilarity, IHashable, IImageSource
{

	/*[MN]
	public Stream Stream { get; protected set; }

	[MNNW(true, nameof(Stream))]
	public bool HasStream => Stream != null && Stream != Stream.Null;*/

	protected static readonly ILogger s_logger;

	static UniImage()
	{
		s_logger = AppSupport.Factory.CreateLogger(nameof(UniImage));
	}

	public UniImageType Type { get; }

	public virtual long? Size => Bytes.Length;

	public string Value { get; }

	[MN]
	public string LocalFilePath { get; protected set; }

	[MNNW(true, nameof(LocalFilePath))]
	public bool HasFilePath => LocalFilePath != null && File.Exists(LocalFilePath);

	/*[MNNW(true, nameof(Image), nameof(Image.Metadata))]
	public bool HasImageFormat => HasImage && Image.Metadata.DecodedImageFormat != null;*/

	public bool IsUri => Type == UniImageType.Uri;

	public bool IsFile => Type == UniImageType.File;

	public bool IsUnknown => Type == UniImageType.Unknown;

#region

	[MN]
	public IImageFormat ImageFormat => Image?.Metadata.DecodedImageFormat;

	[MNNW(true, nameof(ImageFormat))]
	public bool HasImageFormat => ImageFormat != null;

	[MN]
	public Image Image { get; protected set; }

	[MNNW(true, nameof(Image))]
	public bool HasImage => Image != null;

#endregion

#region

	public ulong? Hash { get; protected set; }

	[MNNW(true, nameof(Hash))]
	public bool HasHash => Hash.HasValue;

#endregion

	public double? Similarity { get; internal set; }

#region

	public byte[] Bytes { get; protected set; }

	[MNNW(true, nameof(Bytes))]
	public bool HasBytes => Bytes != null;

	public Stream GetStream()
	{
		return new MemoryStream(Bytes, writable: false);
	}

#endregion


	public static readonly UniImage Null = null;


	private protected UniImage(string value, UniImageType type)
	{
		Value = value;
		Type  = type;
	}


	protected abstract Task<bool> AllocAsync(CancellationToken ct = default);

	/// <summary>
	/// Allocates <see cref="Image"/>
	/// </summary>
	public virtual async Task<bool> AllocImageAsync(CancellationToken ct = default)
	{
		if (!HasImage) {

			try {

				await using var stream = GetStream();
				Image = await ISImage.LoadAsync(stream, ct);
				stream.Rewind();
				Hash  = ImageScanner.ImageHasher.Hash(stream);
			}
			catch (Exception exception) {
				s_logger.LogError(exception, "{Value} failed to allocate image", Value);
				return false;
			}

		}

		return HasImage;

	}

	public virtual bool CalculateSimilarity(IHashable hashable)
	{
		Similarity = ISimilarity.CalculateHashSimilarity(this, hashable);
		return Similarity.HasValue;
	}

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
				ui = new UniImageFile(fi);
			}
			else if (UniImageUri.IsUriType(o, out var url2)) {
				ui = new UniImageUri(url2);
			}
			else {
				goto ret;
			}

			if (autoInit) {
				bool allocOk    = false;
				bool allocImgOk = false;

				allocOk = await ui.AllocAsync(ct);

				if (allocOk) {
					allocImgOk = await ui.AllocImageAsync(ct);
				}

				s_logger.LogTrace("{Value} :: {AllocOk} {AllocImgOk}", o, allocOk, allocImgOk);

				if (autoDisposeOnError && (!allocOk || !allocImgOk)) {
					ui?.Dispose();
				}
				else {
				}
			}

		}
		catch (Exception e) {
			// str?.Dispose();
			s_logger.LogError(e, "{Value}", o);
		}

	ret:
		return ui;
	}

	public static bool IsValidSourceType(object str, bool checkExt = true)
	{
		bool isFile = UniImageFile.IsFileType(str, out var f);
		bool isUri  = UniImageUri.IsUriType(str, out var f2);
		bool ok     = isFile || isUri;

		if (isFile && checkExt) {
			//todo
			string ext = Path.GetExtension(str.ToString())?[1..];
			return FileType.Image.Any(x => x.Subtype == ext);
		}

		return ok;
	}

	public bool TryWriteToFile(string fn = null)
	{
		if (!HasFilePath) {
			LocalFilePath = WriteToFile(fn);
		}

		return HasFilePath;
	}

	public bool TryDeleteFile()
	{
		if (HasFilePath) {
			File.Delete(LocalFilePath);
			LocalFilePath = null;
		}

		return !HasFilePath;
	}

	[MURV]
	public virtual string WriteToFile([CBN] string fn = null, [CBN] Action<IImageProcessingContext> operation = null)
	{
		if (!HasImage) {
			throw new InvalidOperationException();
		}

		fn ??= Path.GetTempFileName();

		var encoder = new PngEncoder();
		operation ??= static _ => { };

		// using ISImage image = ISImage.Load(Stream);
		using var image = Image.Clone(operation);

		// Image.Save(fn);
		image.Mutate(operation);
		image.Save(fn, encoder);

		// Stream.TrySeek();
		// Image.Save(fn);
		// Image.Mutate(operation);

		return fn;
	}


	public virtual void Dispose()
	{
		GC.SuppressFinalize(this);
		Image?.Dispose();
	}

	public virtual ValueTask DisposeAsync()
	{
		Dispose();
		return ValueTask.CompletedTask;
	}

	public override string ToString()
	{
		return $"{Type} : {Value} {Size} bytes of type {(HasImageFormat ? ImageFormat.Name : "?")}";
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