// Author: Deci | Project: SmartImage.Lib | Name: UniImage.cs
// Date: 2024/05/02 @ 10:05:55

using System.Diagnostics;
using System.Net;
using Flurl.Http;
using JetBrains.Annotations;
using Novus.FileTypes;
using Novus.Streams;
using Novus.Win32;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Processing;
using SmartImage.Lib.Model;

namespace SmartImage.Lib;

public enum UniImageType
{

	Unknown = 0,
	File,
	Uri,
	Stream

}

public class UniImage : IItemSize, IDisposable, IAsyncDisposable, IEquatable<UniImage>
{

	public Stream Stream { get; internal init; }

	public object Value { get; internal init; }

	public UniImageType Type { get; internal init; }

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

	public bool IsUri => Type == UniImageType.Uri;

	public bool IsFile => Type == UniImageType.File;

	public bool IsStream => Type == UniImageType.Stream;

	public static readonly UniImage Null = new();

	internal UniImage(object value, Stream stream, UniImageType type)
	{
		Stream = stream;
		Value  = value;
		Type   = type;
	}

	private UniImage() : this(null, Stream.Null, UniImageType.Unknown) { }

	public static async Task<UniImage> TryCreateAsync(object o, CancellationToken t = default)
	{
		Stream       str;
		IImageFormat fmt;
		UniImageType qt;
		string       s = null;

		if (IsFileType(o, out var fi)) {
			// var s = ((FileInfo) fi).FullName;
			s   = (string) o;
			str = File.OpenRead(s);
			qt  = UniImageType.File;
		}
		else if (IsUriType(o, out var url2)) {
			var res = await HandleUriAsync(url2, t);
			str = await res.GetStreamAsync();
			qt  = UniImageType.Uri;
		}
		else if (o is Stream) {
			str = (Stream) o;
			qt  = UniImageType.Stream;
		}
		else {
			return Null;
		}

		str.TrySeek();

		fmt = await ISImage.DetectFormatAsync(str, t);

		str.TrySeek();

		var query = new UniImage(o, str, qt)
		{
			Info     = fmt,
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
				          User_Agent = Resources.UserAgent1,
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

	public bool Equals(UniImage other)
	{
		if (ReferenceEquals(null, other)) return false;
		if (ReferenceEquals(this, other)) return true;

		return Equals(Value, other.Value);
	}

	public override bool Equals(object obj)
	{
		return ReferenceEquals(this, obj) || (obj is UniImage other && Equals(other));
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