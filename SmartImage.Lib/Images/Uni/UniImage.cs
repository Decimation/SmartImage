// Author: Deci | Project: SmartImage.Lib | Name: UniImage.cs
// Date: 2024/05/02 @ 10:05:55


using System.Buffers;
using System.ComponentModel;
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
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using CoenM.ImageHash;
using CommunityToolkit.HighPerformance;
using Kantan.Net.Utilities;
using Microsoft.IO;
using SixLabors.ImageSharp.Memory;
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

/// <summary>
/// <seealso cref="UniSource"/>
/// </summary>	
public abstract class UniImage : IDisposable, ILength, IEquatable<UniImage>, ISimilarity, IHashable,
                                 IImage, INotifyPropertyChanged
{

	protected static readonly ILogger s_logger;

	static UniImage()
	{
		s_logger = AppSupport.Factory.CreateLogger(nameof(UniImage));
	}

	internal static readonly RecyclableMemoryStreamManager MemMgr = new(new RecyclableMemoryStreamManager.Options
		                                                                    { });

	public UniImageType Type { get; }

	public virtual long? Length => Bytes.Length;

	// public virtual string Name {get; protected set;}

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
	public IImageFormat ImageFormat => Image?.Metadata.DecodedImageFormat;

	[MNNW(true, nameof(ImageFormat), nameof(Image))]
	public bool HasImageFormat => ImageFormat != null;

	[MN]
	[JI]
	public ISImage Image
	{
		get;
		protected set
		{
			if (SetField(ref field, value)) {
				OnPropertyChanged(nameof(ImageFormat));
				OnPropertyChanged(nameof(HasImage));
			}
		}
	}

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
				OnPropertyChanged(nameof(HasHash));
			}
		}
	}

	[MNNW(true, nameof(Hash))]
	public bool HasHash => Hash.HasValue;

#endregion

#region

	public double? Similarity
	{
		get;
		internal set => SetField(ref field, value);
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
				OnPropertyChanged(nameof(Length));
				OnPropertyChanged(nameof(HasBytes));
			}
		}
	}

	[MNNW(true, nameof(Bytes), nameof(Length))]
	public bool HasBytes => Bytes != null;

	[MURV]
	public Stream GetStream()
	{
		// return HasBytes ? new MemoryStream(Bytes, writable: false) : Stream.Null;
		var str = MemMgr.GetStream(Value, Bytes);
		return str;
	}

#endregion


	private protected UniImage(string value, UniImageType type)
	{
		Value = value;
		Type  = type;
	}


#region

	/// <summary>
	/// Allocates <see cref="Bytes"/>
	/// </summary>
	protected abstract ValueTask<bool> AllocSourceAsync(CancellationToken ct = default);

	/// <summary>
	/// Allocates <see cref="Image"/> from <see cref="Bytes"/>
	/// </summary>
	public virtual async ValueTask<bool> AllocImageAsync(CancellationToken ct = default)
	{
		if (!HasImage) {

			try {

				await using var stream = GetStream();
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

	/// <returns><see cref="AllocSourceAsync"/>, <see cref="AllocImageAsync"/></returns>
	protected virtual async ValueTask<(bool AllocOk, bool AllocImageOk)> AllocAll(CancellationToken ct)
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
	public static async Task<UniImage> TryCreateAsync(object o, bool autoInit = true, bool autoDisposeOnError = true,
	                                                  CancellationToken ct = default)
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

				(allocOk,allocImgOk) = await ui.AllocAll(ct);

				s_logger.LogTrace("{Value} :: {AllocOk} {AllocImgOk}", o, allocOk, allocImgOk);

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

	public static bool IsValidSourceType(object o)
	{
		bool isFile = UniImageFile.IsFileType(o, out var f);
		bool isUri  = UniImageUrl.IsUrlType(o, out var url);
		bool ok     = isFile || isUri;

		return ok;
	}

#endregion

#region New region

	public bool TryWriteOrGetFile(string fn = null)
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
	public virtual string WriteImageToFile([CBN] string fn = null)
	{
		if (!HasImage) {
			throw new InvalidOperationException();
		}

		fn ??= this switch
		{
			UniImageUrl uri   => uri.Url.GetFileName(),
			UniImageFile file => file.LocalFileInfo.Name,
			_                 => Path.GetRandomFileName()
		};

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
		s_logger.LogTrace("Disposing {Uv} {Ut}", Value, Type);
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
	{
		return Equals(left, right);
	}

	public static bool operator !=(UniImage left, UniImage right)
	{
		return !Equals(left, right);
	}

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