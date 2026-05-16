// Author: Deci | Project: SmartImage.Lib | Name: UniImage.cs
// Date: 2024/05/02 @ 10:05:55

// ReSharper disable InconsistentNaming

#nullable disable
#pragma warning disable CS0168 // Variable is declared but never used

using AngleSharp.Css.Values;
using CoenM.ImageHash;
using Microsoft.Extensions.Logging;
using Microsoft.IO;
using Novus.FileTypes.Uni;
using Novus.Streams;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SmartImage.Lib.Model;
using SmartImage.Lib.Utilities;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.Net.WebSockets;

namespace SmartImage.Lib.Images.Uni;

/// <summary>
/// Represents an image dynamically loaded from a described source
/// <seealso cref="UniSource"/>
/// </summary>	
public abstract class UniImage : IUniImage, IEquatable<UniImage>, ITryCreate<UniImage>
{

	protected static readonly ILogger s_logger;

	internal static readonly RecyclableMemoryStreamManager MemMgr = new(new RecyclableMemoryStreamManager.Options { });

	static UniImage()
	{
		s_logger = AppSupport.Factory.CreateLogger(nameof(UniImage));
	}

	/// <summary>
	/// Name of <see cref="Value"/>
	/// </summary>
	public virtual string Name { get; protected set; }

	public UniImageType Type { get; }

	[JI]
	public string Value { get; }

	[MN]
	public string LocalFilePath { get; protected set; }

	[MNNW(true, nameof(LocalFilePath))]
	public bool HasLocalFilePath => File.Exists(LocalFilePath);

	public bool IsUri => Type == UniImageType.Uri;

	public bool IsFile => Type == UniImageType.File;

	public bool IsUnknown => Type == UniImageType.Unknown;

#region

	[MN]
	[JI]
	public ISImage Image
	{
		get;
		protected set
		{
			if (SetField(ref field, value)) {
				OnPropertyChanged(nameof(IImage.ImageFormat));
				OnPropertyChanged(nameof(IImage.HasImage));
			}
		}
	}

	public IImageFormat ImageFormat => Image?.Metadata.DecodedImageFormat;

	[MNNW(true, nameof(ImageFormat), nameof(Image))]
	public bool HasImageFormat => ImageFormat != null;

	[MNNW(true, nameof(Image), nameof(ImageFormat))]
	public bool HasImage => Image != null;

#endregion

#region

	public ulong? Hash
	{
		get;
		protected set
		{
			if (SetField(ref field, value)) {
				OnPropertyChanged(nameof(IHashable.HasHash));
			}
		}
	}

#endregion

#region

	public double? Similarity
	{
		get;
		protected set => SetField(ref field, value);
	}

	public virtual bool CalculateSimilarity(IHashable hashable)
	{
		Similarity = ISimilarity.CalculateHashSimilarity(this, hashable);
		return Similarity.HasValue;
	}

#endregion

#region

	public byte[] Bytes
	{
		get;
		protected set
		{
			if (SetField(ref field, value)) {
				OnPropertyChanged(nameof(IUniImage.HasBytes));
			}
		}
	}

	[MNNW(true, nameof(Bytes), nameof(IUniImage.Length))]
	public bool HasBytes => Bytes != null;

	public long? Length => Bytes?.Length;

#endregion


	private protected UniImage(string value, UniImageType type)
	{
		Value = value;
		Type  = type;
	}


#region

	[MURV]
	public Stream GetSource()
	{
		if (!HasBytes) {
			// throw new InvalidOperationException($"{nameof(Bytes)} not loaded");
			return Stream.Null;
		}

		return UniImage.MemMgr.GetStream(Name, Bytes);
	}


	[MNNW(true, nameof(Bytes))]
	public abstract ValueTask<bool> AllocSourceAsync(CancellationToken ct = default);

	[MNNW(true, nameof(Image))]
	public virtual async ValueTask<bool> AllocImageAsync(CancellationToken ct = default)
	{
		if (!HasImage) {

			try {
				var src = await AllocSourceAsync(ct);

				if (!src) {
					return false;
				}

				await using var stream = GetSource();
				Image = await ISImage.LoadAsync(stream, ct);
				stream.Rewind();
				Hash = ImageUtilities.Hasher.Hash(stream);
			}
			catch (Exception exception) {
				s_logger.LogError(exception, "{Value} failed to allocate image", Value);
				return false;
			}

		}

		return HasImage;
	}

	// TODO: Dispose failed

	/// <returns><see cref="AllocSourceAsync"/>, <see cref="AllocImageAsync"/></returns>
	public async ValueTask<(bool AllocSourceOk, bool AllocImageOk)> AllocAllAsync(CancellationToken ct)
	{
		bool allocOk    = await AllocSourceAsync(ct);
		bool allocImgOk = false;

		if (allocOk) {
			allocImgOk = await AllocImageAsync(ct);
		}

		return (allocOk, allocImgOk);
	}

	/// <summary>
	/// Attempts to create the appropriate <see cref="UniImage" /> for <paramref name="o" />.
	/// </summary>
	public static async Task<UniImage> TryCreateAsync(object o, bool autoInit = true, bool autoDisposeOnError = true, CancellationToken ct = default)
	{
		UniImage ui = null;

		try {

			if (UniImageFile.IsFileType(o, out var fi)) {
				ui = new UniImageFile(fi);
			}
			else if (UniImageUrl.IsUrlType(o, out var url2)) {
				ui = new UniImageUrl(url2);
			}
			else {
				goto ret;
			}

			if (autoInit) {
				bool allocOk    = false;
				bool allocImgOk = false;

				(allocOk, allocImgOk) = await ui.AllocAllAsync(ct);

				s_logger.LogTrace("{Value} :: {AllocSrcOk} {AllocImgOk}", o, allocOk, allocImgOk);

				if (autoDisposeOnError && (!allocOk || !allocImgOk)) {
					ui?.Dispose();
				}
				else { }
			}

		}
		catch (Exception e) {
			// str?.Dispose();
			s_logger.LogError(e, "{Value}", o);
		}

	ret:
		return ui;
	}

	/*public static bool Union(object o, [CBN] out UniImage img)
	{
		img = null;

		switch (o) {
			case string s when String.IsNullOrWhiteSpace(s) && File.Exists(s):
				img = new UniImageFile(new FileInfo(s));
				break;

			case string s2 when Url.IsValid(s2):
				Url u2 = s2;

				if (ImageScanner.LegalSchemeWhitelist.Contains(u2.Scheme)) {
					img = new UniImageUrl(u2);
				}

				break;

			case Url u when ImageScanner.LegalSchemeWhitelist.Contains(u.Scheme):
				img = new UniImageUrl(u);
				break;
		}


		return img != null;
	}*/

	public static bool IsValidSourceType(object o)
	{
		bool isFile = UniImageFile.IsFileType(o, out var f);
		bool isUri  = UniImageUrl.IsUrlType(o, out var url);
		bool ok     = isFile || isUri;

		return ok;
	}

#endregion

#region

	public bool TryWriteOrGetFile([CBN] string fn = null)
	{
		if (!HasLocalFilePath) {
			LocalFilePath = WriteImageToFile(fn);
		}

		return HasLocalFilePath;
	}

	public bool TryDeleteFile()
	{
		if (HasLocalFilePath) {
			File.Delete(LocalFilePath);
			LocalFilePath = null;
		}

		return !HasLocalFilePath;
	}

	[MURV]
	[return: NN]
	public virtual string WriteImageToFile([CBN] string fn = null)
	{
		if (!HasImage) {
			throw new InvalidOperationException();
		}

		if (HasLocalFilePath) {
			return LocalFilePath;
		}

		fn ??= String.IsNullOrWhiteSpace(Name) ? Path.GetRandomFileName() : Name;

		fn = Path.ChangeExtension(fn, ImageFormat.FileExtensions.First());

		var path = Path.Combine(Path.GetTempPath(), fn);

		Image.Save(path);

		return path;
	}

#endregion


	public virtual void Dispose()
	{
		GC.SuppressFinalize(this);
		Image?.Dispose();
	}

	public override string ToString()
	{
		return $"{Type} : {Value} {Length} bytes of type {(HasImageFormat ? ImageFormat.Name : "?")}";
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
		return (Value != null ? Value.GetHashCode() : 0);
	}

	public static bool operator ==(UniImage left, UniImage right)
		=> Equals(left, right);

	public static bool operator !=(UniImage left, UniImage right)
		=> !Equals(left, right);

#endregion

	public event PropertyChangedEventHandler PropertyChanged;

	protected virtual void OnPropertyChanged([CMN] string propertyName = null)
	{
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}

	protected virtual bool SetField<T>(ref T field, T value, [CMN] string propertyName = null)
	{
		if (EqualityComparer<T>.Default.Equals(field, value))
			return false;

		field = value;
		OnPropertyChanged(propertyName);
		return true;
	}

}

/// <summary>
/// <seealso cref="UniSourceType"/>
/// </summary>
public enum UniImageType
{

	Unknown = 0,
	File,
	Uri

}

[Flags]
public enum AllocFlags
{

	//todo
	None   = 0,
	Stream = 1 << 0,
	Image  = 1 << 1,

}