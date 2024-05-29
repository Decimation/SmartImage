// Author: Deci | Project: SmartImage.Lib | Name: BinaryImageFile.cs
// Date: 2024/05/02 @ 10:05:55

using System.Diagnostics;
using System.Net;
using Flurl.Http;
using JetBrains.Annotations;
using Novus.FileTypes;
using Novus.FileTypes.Uni;
using Novus.Streams;
using Novus.Win32;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Processing;
using SmartImage.Lib.Model;

namespace SmartImage.Lib.Images;

/// <summary>
/// <seealso cref="Novus.FileTypes.Uni.UniSourceType"/>
/// </summary>
public enum BinaryImageFileSource
{

	Unknown = 0,
	File,
	Uri,
	Stream

}

/// <summary>
/// <seealso cref="Novus.FileTypes.Uni.UniSource"/>
/// </summary>
public class BinaryImageFile : IItemSize, IDisposable, IAsyncDisposable, IEquatable<BinaryImageFile>
{

	public Stream Stream { get; internal init; }

	public object Value { get; internal init; }

	public BinaryImageFileSource Type { get; internal init; }

	public long Size
	{
		get
		{
			if (HasValue && Stream.CanRead) {
				return Stream.Length;
			}

			return Native.INVALID;
		}
	}

	[CBN]
	public string ValueString => HasValue ? Value.ToString() : null;

	[MNNW(true, nameof(Value))]
	public bool HasValue => Value != null;

	[MN]
	public string FilePath { get; private set; }

	[MNNW(true, nameof(FilePath))]
	public bool HasFile => FilePath != null && File.Exists(FilePath);

	[MN]
	public IImageFormat Info { get; private init; }

	[MNNW(true, nameof(Info))]
	public bool HasInfo => Info != null;

	public bool IsUri => Type == BinaryImageFileSource.Uri;

	public bool IsFile => Type == BinaryImageFileSource.File;

	public bool IsStream => Type == BinaryImageFileSource.Stream;

	public static readonly BinaryImageFile Null = new();

	internal BinaryImageFile(object value, Stream stream, BinaryImageFileSource type, IImageFormat format)
	{
		Stream = stream;
		Value  = value;
		Type   = type;
		Info   = format;
	}

	internal BinaryImageFile(object value, Stream stream, BinaryImageFileSource type)
		: this(value, stream, type, null) { }

	private BinaryImageFile() : this(null, Stream.Null, BinaryImageFileSource.Unknown) { }

	public static async Task<BinaryImageFile> TryCreateAsync(object o, CancellationToken t = default)
	{
		Stream                str;
		IImageFormat          fmt;
		BinaryImageFileSource qt;
		string                s = null;

		if (IsFileType(o, out var fi)) {
			// var s = ((FileInfo) fi).FullName;
			s   = (string) o;
			str = File.OpenRead(s);
			qt  = BinaryImageFileSource.File;
		}
		else if (IsUriType(o, out var url2)) {
			var res = await HandleUriAsync(url2, t);
			str = await res.GetStreamAsync();
			qt  = BinaryImageFileSource.Uri;
		}
		else if (o is Stream) {
			str = (Stream) o;
			qt  = BinaryImageFileSource.Stream;
		}
		else {
			return Null;
		}

		str.TrySeek();

		fmt = await ISImage.DetectFormatAsync(str, t);

		str.TrySeek();

		var query = new BinaryImageFile(o, str, qt, fmt)
		{
			FilePath = s
		};

		return query;
	}

	public static async Task<IFlurlResponse> HandleUriAsync(Url value, CancellationToken ct)
	{
		// value = value.CleanString();

		var res = await value.AllowAnyHttpStatus()
			          .WithHeaders(new
			          {
				          // todo
				          User_Agent = R1.UserAgent1,
			          })
			          .GetAsync(cancellationToken: ct);

		if (res.ResponseMessage.StatusCode == HttpStatusCode.NotFound) {
			throw new ArgumentException($"{value} returned {HttpStatusCode.NotFound}");
		}

		return res;
	}

	#region

	public static bool IsStreamType(object o, out Stream t2)
	{
		t2 = Stream.Null;

		if (o is Stream sz) {
			t2 = sz;
		}

		return t2 != Stream.Null;
	}

	public static bool IsUriType(object o, out Url u)
	{
		u = o switch
		{
			Url u2   => u2,
			string s => s,
			_        => null
		};
		return Url.IsValid(u);
	}

	public static bool IsFileType(object o, out FileInfo f)
	{
		f = null;

		if (o is string { } s && File.Exists(s)) {
			f = new FileInfo(s);
		}

		return f != null;
	}

	public static bool IsValidSourceType(object str, bool checkExt = true)
	{
		// UniSourceType v        = UniHandler.GetUniType(str, out object o2);
		/*bool isFile   = UniSourceFile.IsType(str, out var f);
		bool isUri    = UniSourceUrl.IsType(str, out var f2);
		bool isStream = UniSourceStream.IsType(str, out var f3);*/
		bool isFile   = IsFileType(str, out var f);
		bool isUri    = IsUriType(str, out var f2);
		bool isStream = IsStreamType(str, out var f3);
		bool ok       = isFile || isUri || isStream;

		if (isFile && checkExt) {
			//todo
			string ext = Path.GetExtension(str.ToString())?[1..];
			return FileType.Image.Any(x => x.Subtype == ext);
		}

		return ok;
	}

	#endregion

	public bool TryGetFile(string fn = null)
	{
		if (!HasFile && HasValue) {
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

	[MustUseReturnValue]
	public string WriteToFile(Action<IImageProcessingContext> operation = null, [CBN] string fn = null)
	{
		if (!HasValue) {
			throw new InvalidOperationException();
		}

		string t;
		fn ??= Path.GetTempFileName();

		var encoder = new PngEncoder();

		using ISImage image = ISImage.Load(Stream);

		if (operation != null) {
			image.Mutate(operation);

		}

		image.Save(fn, encoder);

		Stream.TrySeek();

		return fn;
	}

	[MustUseReturnValue]
	[ICBN]
	public string WriteToFile(string fn = null)
	{
		if (!HasValue) {
			throw new InvalidOperationException($"{nameof(HasValue)} is {false}");
		}

		string t;
		fn ??= Path.GetTempFileName();

		if (Type != BinaryImageFileSource.File) {
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

	public void Dispose()
	{
		Stream?.Dispose();

		// ImageInfo?.Dispose();
		Debug.WriteLine($"Disposing {ValueString} w/ {Size}");
	}

	public async ValueTask DisposeAsync()
	{
		if (Stream != null)
			await Stream.DisposeAsync();
	}

	public override string ToString()
	{
		string s = $"{ValueString} ({Type}) [{Info.DefaultMimeType}]";

		return s;
	}

	#region Equality members

	public bool Equals(BinaryImageFile other)
	{
		if (ReferenceEquals(null, other)) return false;
		if (ReferenceEquals(this, other)) return true;

		return Equals(Value, other.Value);
	}

	public override bool Equals(object obj)
	{
		return ReferenceEquals(this, obj) || obj is BinaryImageFile other && Equals(other);
	}

	public override int GetHashCode()
	{
		// return HashCode.Combine(Uni, Upload, Size);
		return HashCode.Combine(Value);

		// return Uni.GetHashCode();
	}

	public static bool operator ==(BinaryImageFile left, BinaryImageFile right)
	{
		return Equals(left, right);
	}

	public static bool operator !=(BinaryImageFile left, BinaryImageFile right)
	{
		return !Equals(left, right);
	}

	#endregion

}