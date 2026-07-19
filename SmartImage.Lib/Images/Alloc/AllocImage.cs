// Author: Deci | Project: SmartImage.Lib | Name: AllocImage.cs
// Date: 2024/05/02 @ 10:05:55

// ReSharper disable InconsistentNaming

#nullable disable
#pragma warning disable CS0168 // Variable is declared but never used

using System.ComponentModel;
using System.Diagnostics;
using CoenM.ImageHash;
using Microsoft.Extensions.Logging;
using Microsoft.IO;
using Novus.FileTypes.Uni;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SmartImage.Lib.Model;
using SmartImage.Lib.Utilities;

namespace SmartImage.Lib.Images.Alloc;

/// <summary>
/// Represents an image dynamically loaded from a described source
/// <seealso cref="UniSource"/>
/// </summary>	
public abstract class AllocImage : IAllocImage, IEquatable<AllocImage>, IAllocFromSource<IAllocImage>
{

	private protected static readonly ILogger s_logger = AppSupport.Factory.CreateLogger(nameof(AllocImage));

	internal static readonly RecyclableMemoryStreamManager MemMgr = new(new RecyclableMemoryStreamManager.Options { });

	/// <summary>
	/// Name of <see cref="Value"/>
	/// </summary>
	public virtual string Name { get; protected set; }

	public AllocImageType Type { get; }

	[JI]
	public string Value { get; }

	// public AllocImageFlags Flags { get; protected set;}

	[MN]
	public string LocalFilePath { get; protected set; }

	[MNNW(true, nameof(LocalFilePath))]
	public bool HasLocalFilePath => File.Exists(LocalFilePath);

	public bool IsUri => Type == AllocImageType.Uri;

	public bool IsFile => Type == AllocImageType.File;

	public bool IsUnknown => Type == AllocImageType.Unknown;

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
		set => SetField(ref field, value);
	}

#endregion

#region

	public byte[] Source
	{
		get;
		protected set
		{
			if (SetField(ref field, value)) {
				OnPropertyChanged(nameof(HasSource));
			}
		}
	}

	[MNNW(true, nameof(Source), nameof(IAllocImage.Length))]
	public bool HasSource => Source != null;

	public long? Length => Source?.Length;

#endregion

	protected AllocImage(string value, AllocImageType type)
	{
		Value = value;
		Type  = type;
	}


#region

	[MNNW(true, nameof(HasSource))]
	public Stream GetSource()
	{
		if (!HasSource) {
			Debugger.Break();

			// throw new InvalidOperationException($"{nameof(Source)} not loaded");
			return Stream.Null;
		}

		return MemMgr.GetStream(Name, Source);
	}


	public abstract Task<bool> AllocSourceAsync(CancellationToken ct = default);

	public virtual async Task<bool> AllocImageAsync(CancellationToken ct = default)
	{
		if (!HasImage) {

			try {
				var src = await AllocSourceAsync(ct);

				if (!src) {
					return false;
				}

				await using var stream = GetSource();

				Image = await ISImage.LoadAsync(stream, ct);

				await using var hashStream = GetSource();
				Hash = ImageUtilities.Hasher.Hash(hashStream);
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
	public async Task<(bool AllocSourceOk, bool AllocImageOk)> AllocAllAsync(CancellationToken ct)
	{

		const TaskContinuationOptions CONT_OPTIONS = TaskContinuationOptions.OnlyOnRanToCompletion | TaskContinuationOptions.NotOnCanceled;

		bool allocOk = false;

		bool allocImgOk = false;

		allocImgOk = await AllocSourceAsync(ct).ContinueWith((task) =>
		{
			allocOk = task.Result;

			return allocOk ? AllocImageAsync(ct) : Task.FromResult(false);

		}, cancellationToken: ct, CONT_OPTIONS, TaskScheduler.Default).Unwrap();


		return (allocOk, allocImgOk);
	}

	/// <summary>
	/// Attempts to create the appropriate <see cref="AllocImage" /> for <paramref name="src" />.
	/// </summary>
	public static async Task<IAllocImage> FromSourceAsync(object            src, bool autoInit = true, bool autoDisposeOnError = true,
	                                                      CancellationToken ct = default)
	{
		AllocImage ui = null;

		try {

			if (AllocImageFile.IsFileType(src, out var fi)) {
				ui = new AllocImageFile(fi);
			}
			else if (AllocImageUrl.IsUrlType(src, out var url2)) {
				ui = new AllocImageUrl(url2);
			}
			else {
				goto ret;
			}

			if (autoInit) {
				bool allocOk    = false;
				bool allocImgOk = false;

				(allocOk, allocImgOk) = await ui.AllocAllAsync(ct);

				s_logger.LogTrace("{Value} :: {AllocSrcOk} {AllocImgOk}", src, allocOk, allocImgOk);

				if (autoDisposeOnError && (!allocOk || !allocImgOk)) {
					ui?.Dispose();
				}
				else { }
			}

		}
		catch (Exception e) {
			// str?.Dispose();
			s_logger.LogError(e, "{Value}", src);
		}

	ret:
		return ui;
	}

	/*public static bool Union(object o, [CBN] out AllocImage img)
	{
		img = null;

		switch (o) {
			case string s when String.IsNullOrWhiteSpace(s) && File.Exists(s):
				img = new AllocImageFile(new FileInfo(s));
				break;

			case string s2 when Url.IsValid(s2):
				Url u2 = s2;

				if (ImageScanner.LegalSchemeWhitelist.Contains(u2.Scheme)) {
					img = new AllocImageUrl(u2);
				}

				break;

			case Url u when ImageScanner.LegalSchemeWhitelist.Contains(u.Scheme):
				img = new AllocImageUrl(u);
				break;
		}


		return img != null;
	}*/

	public static bool IsValidSourceType(object o)
	{
		bool isFile = AllocImageFile.IsFileType(o, out var f);
		bool isUri  = AllocImageUrl.IsUrlType(o, out var url);
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

	/*public virtual object Clone()
	{
		object clone = this;

		if (IsFile && this is AllocImageFile uif) {
			var ui = new AllocImageFile(uif.LocalFileInfo);
			clone = ui;
		}
		else if (IsUri && this is AllocImageUrl uiu) {
			var ui = new AllocImageUrl(uiu.Url)
			{
				Source = Source
			};
			clone = ui;
		}

		if (clone is AllocImage ui2 && ui2 != this) {
			ui2.Source = Source;
			ui2.LocalFilePath = LocalFilePath;
			Similarity = Similarity;
			Hash = Hash;
			Image = Image;
		}

		return clone;
	}*/

#region Equality members

	public bool Equals(AllocImage other)
	{
		if (other is null)
			return false;

		if (ReferenceEquals(this, other))
			return true;

		return Equals(Value, other.Value);
	}

	public override bool Equals(object obj)
	{
		return ReferenceEquals(this, obj) || obj is AllocImage other && Equals(other);
	}

	public override int GetHashCode()
	{
		return (Value != null ? Value.GetHashCode() : 0);
	}

	public static bool operator ==(AllocImage left, AllocImage right)
		=> Equals(left, right);

	public static bool operator !=(AllocImage left, AllocImage right)
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
public enum AllocImageType
{

	Unknown = 0,
	File,
	Uri

}