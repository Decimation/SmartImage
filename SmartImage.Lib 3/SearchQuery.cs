global using MN = System.Diagnostics.CodeAnalysis.MaybeNullAttribute;
global using CBN = JetBrains.Annotations.CanBeNullAttribute;
global using NN = System.Diagnostics.CodeAnalysis.NotNullAttribute;
global using MNNW = System.Diagnostics.CodeAnalysis.MemberNotNullWhenAttribute;
global using ISImage = SixLabors.ImageSharp.Image;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using Flurl.Http;
using JetBrains.Annotations;
using Microsoft;
using Novus.FileTypes;
using Novus.FileTypes.Uni;
using Novus.Streams;
using Novus.Win32;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Processing;
using SmartImage.Lib.Engines;
using SmartImage.Lib.Engines.Impl.Upload;
using SmartImage.Lib.Model;
using SmartImage.Lib.Results;
using SmartImage.Lib.Utilities;
using SixLabors.ImageSharp.Formats;
using System.Net;

[assembly: InternalsVisibleTo("SmartImage")]
[assembly: InternalsVisibleTo("SmartImage.UI")]
[assembly: InternalsVisibleTo("SmartImage.Rdx")]

namespace SmartImage.Lib;

public enum QueryType
{

	Unknown = 0,
	File,
	Uri,
	Stream

}

public sealed class SearchQuery : IDisposable, IEquatable<SearchQuery>, IItemSize
{

	public Stream Stream { get; internal set; }

	public object Value { get; internal set; }

	[MN]
	public Url Upload { get; internal set; }

	public QueryType Type { get; internal set; }

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

	[MNNW(true, nameof(Upload))]
	public bool IsUploaded => Url.IsValid(Upload);

	[MNNW(true, nameof(Value))]
	public bool HasValue => Value != null;

	[MN]
	public string FilePath { get; private set; }

	[MNNW(true, nameof(FilePath))]
	public bool HasFile => FilePath != null && File.Exists(FilePath);

	internal SearchQuery([MN] object f, Stream s, QueryType type)
	{
		Value  = f;
		Stream = s;
		Type   = type;

		// Size = Uni == null ? default : Uni.Stream.Length;
	}

	internal SearchQuery([MN] object f) : this(f, Stream.Null, QueryType.Unknown) { }

	private SearchQuery() : this(null) { }

	static SearchQuery() { }

	public static readonly SearchQuery Null = new();

	[MN]
	public IImageFormat ImageInfo { get; private set; }

	[MNNW(true, nameof(ImageInfo))]
	public bool HasImage => ImageInfo != null;

	public bool IsUri => Type == QueryType.Uri;

	public bool IsFile => Type == QueryType.File;

	public static async Task<SearchQuery> TryCreateAsync(object o, CancellationToken t = default)
	{
		Stream       str;
		IImageFormat fmt;
		QueryType    qt;
		string       s = null;

		if (IsFileType(o, out var fi)) {
			// var s = ((FileInfo) fi).FullName;
			s = (string) o;
			str = File.OpenRead(s);
			qt  = QueryType.File;
		}
		else if (IsUriType(o, out var url2)) {
			var url = (Url) url2;
			var res = await HandleUriAsync(url, t);
			str = await res.GetStreamAsync();
			qt  = QueryType.Uri;
		}
		else if (o is Stream) {
			str = (Stream) o;
			qt  = QueryType.Stream;
		}
		else {
			return Null;
		}

		fmt = await ISImage.DetectFormatAsync(str, t);
		str.TrySeek();

		var query = new SearchQuery(o, str, qt)
		{
			ImageInfo = fmt,
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

	public async Task<Url> UploadAsync(BaseUploadEngine engine = null, CancellationToken ct = default)
	{
		if (IsUploaded) {
			return Upload;
		}

		string fu = Value.ToString();

		if (Type == QueryType.Uri) {
			Upload = fu;

			// Size   = BaseSearchEngine.NA_SIZE;
			Debug.WriteLine($"Skipping upload for {Value}", nameof(UploadAsync));
		}
		else {
			// fu = await test(fu);

			engine ??= BaseUploadEngine.Default;

			UploadResult u = await engine.UploadFileAsync(fu, ct);
			Url          url;

			if (!u.IsValid) {
				url = null;
				Debug.WriteLine($"{u} is invalid!");

				// Debugger.Break();
			}
			else {
				url = u.Url;

			}

			// TODO: AUTO-RETRY
			/*
			UploadResult u = await UploadAutoAsync(engine, fu, ct);
			Url          url = u?.Url;
			*/

			/*if (!u.IsValid) {
				engine = BaseUploadEngine.All[Array.IndexOf(BaseUploadEngine.All, engine) + 1];
				Debug.WriteLine($"{u.Response.ResponseMessage} failed, retrying with {engine.Name}");
				u = await engine.UploadFileAsync(Uni.Value.ToString(), ct);
			}*/

			Upload = url;

			/*if (u.Response is { }) {
				Size = NetHelper.GetContentLength(u.Response) ?? Size;
			}*/
			// Size = u.Size ?? Size;
			u.Dispose();
		}

		return Upload;
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

	public static bool IsValidSourceType(object str)
	{
		// UniSourceType v        = UniHandler.GetUniType(str, out object o2);
		bool isFile   = UniSourceFile.IsType(str, out var f);
		bool isUri    = UniSourceUrl.IsType(str, out var f2);
		bool isStream = UniSourceStream.IsType(str, out var f3);
		bool ok       = isFile || isUri || isStream;

		if (isFile) {
			string ext = Path.GetExtension(str.ToString())?[1..];
			return FileType.Image.Any(x => x.Subtype == ext);
		}

		return ok;
	}

	#endregion

	public void Dispose()
	{
		Stream?.Dispose();

		// ImageInfo?.Dispose();
		Debug.WriteLine($"Disposing {ValueString} w/ {Size}");
	}

	public override string ToString()
	{
		string s = $"{ValueString} ({Type}) [{ImageInfo.DefaultMimeType}]";

		return s;
	}

	#region Equality members

	public bool Equals(SearchQuery other)
	{
		if (ReferenceEquals(null, other)) return false;
		if (ReferenceEquals(this, other)) return true;

		return Equals(Stream, other.Stream) && Equals(Upload, other.Upload) && Size == other.Size &&
		       (!OperatingSystem.IsWindows());
	}

	public override bool Equals(object obj)
	{
		return ReferenceEquals(this, obj) || (obj is SearchQuery other && Equals(other));
	}

	public override int GetHashCode()
	{
		// return HashCode.Combine(Uni, Upload, Size);
		return HashCode.Combine(Stream);

		// return Uni.GetHashCode();
	}

	public static bool operator ==(SearchQuery left, SearchQuery right)
	{
		return Equals(left, right);
	}

	public static bool operator !=(SearchQuery left, SearchQuery right)
	{
		return !Equals(left, right);
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

		if (Type != QueryType.File) {
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

}

#if OLD_SQ
public sealed class SearchQuery : IDisposable, IEquatable<SearchQuery>, IItemSize
{

	[MN]
	public UniSource Uni { get; }

	[MN]
	public Url Upload { get; internal set; }

	public long Size
	{
		get
		{
			if (HasUni && Uni.Stream.CanRead) {
				return Uni.Stream.Length;
			}

			return Native.INVALID;
		}
	}

	[CBN]
	public string ValueString => HasUni ? Uni.Value.ToString() : null;

	[MNNW(true, nameof(Upload))]
	public bool IsUploaded => Url.IsValid(Upload);

	public bool IsUploading { get; private set; }

	[MNNW(true, nameof(Uni))]
	public bool HasUni => Uni != null;

	internal SearchQuery([MN] UniSource f)
	{
		Uni = f;

		// Size = Uni == null ? default : Uni.Stream.Length;
	}

	private SearchQuery() : this(null) { }

	public static readonly SearchQuery Null = new();

	static SearchQuery() { }

	[MN]
	public ImageInfo ImageInfo { get; private set; }

	[MNNW(true, nameof(ImageInfo))]
	public bool HasImage => ImageInfo != null;

	[MN]
	public string FilePath { get; private set; }

	[MNNW(true, nameof(FilePath))]
	public bool HasFile => FilePath != null;

	public bool LoadImage()
	{
		if (HasUni && ImageInfo == null) {
			if (HasFile) {
				ImageInfo = ISImage.Identify(FilePath);
			}
			else if (OperatingSystem.IsWindows()) {
				Uni.Stream.TrySeek();
				ImageInfo = ISImage.Identify(Uni.Stream);
			}

		}

		return HasImage;
	}

	public static async Task<SearchQuery> TryCreateAsync(string value, CancellationToken ct = default)
	{
		UniSource uf = await UniSource.TryGetAsync(value, ct: ct);

		if (uf == null || !FileType.Image.Contains(uf.FileType)) {
			uf?.Dispose();
			return Null;

		}

		else {
			/*if (uf.IsUri) {
				var r    = await (uf.Value as Url).GetAsync();
				var uri2 = r.ResponseMessage.RequestMessage.RequestUri.ToString();

				if (uri2 == "https://i.imgur.com/removed.png") {
					return Null;
				}
			}*/

			var sq = new SearchQuery(uf)
				{ };

			// sq.LoadImage();

			return sq;

		}
	}

	public async Task<Url> UploadAsync(BaseUploadEngine engine = null, CancellationToken ct = default)
	{
		if (IsUploaded) {
			return Upload;
		}

		IsUploading = true;

		string fu = Uni.Value.ToString();

		if (Uni.IsUri) {
			Upload = fu;

			// Size   = BaseSearchEngine.NA_SIZE;
			Debug.WriteLine($"Skipping upload for {Uni.Value}", nameof(UploadAsync));
		}
		else {
			// fu = await test(fu);

			engine ??= BaseUploadEngine.Default;

			UploadResult u = await engine.UploadFileAsync(fu, ct);
			Url          url;

			if (!u.IsValid) {
				url = null;
				Debug.WriteLine($"{u} is invalid!");

				// Debugger.Break();
			}
			else {
				url = u.Url;

			}

			// TODO: AUTO-RETRY
			/*
			UploadResult u = await UploadAutoAsync(engine, fu, ct);
			Url          url = u?.Url;
			*/

			/*if (!u.IsValid) {
				engine = BaseUploadEngine.All[Array.IndexOf(BaseUploadEngine.All, engine) + 1];
				Debug.WriteLine($"{u.Response.ResponseMessage} failed, retrying with {engine.Name}");
				u = await engine.UploadFileAsync(Uni.Value.ToString(), ct);
			}*/

			Upload = url;

			/*if (u.Response is { }) {
				Size = NetHelper.GetContentLength(u.Response) ?? Size;
			}*/
			// Size = u.Size ?? Size;
			u.Dispose();
		}

		IsUploading = false;

		return Upload;
	}

	public static bool IsValidSourceType(object str)
	{
		// UniSourceType v        = UniHandler.GetUniType(str, out object o2);
		bool isFile = UniSourceFile.IsType(str, out var f);
		bool isUri = UniSourceUrl.IsType(str, out var f2);
		bool isStream = UniSourceStream.IsType(str, out var f3);
		bool ok = isFile || isUri || isStream;

		if (isFile) {
			string ext = Path.GetExtension(str.ToString())?[1..];
			return FileType.Image.Any(x => x.Subtype == ext);
		}

		return ok;
	}

	public void Dispose()
	{
		Uni?.Dispose();

		// ImageInfo?.Dispose();
		Debug.WriteLine($"Disposing {ValueString} w/ {Size}");
	}

	public override string ToString()
	{
		string s = $"{Uni} | {ValueString}";

		return s;
	}

	#region Equality members

	public bool Equals(SearchQuery other)
	{
		if (ReferenceEquals(null, other)) return false;
		if (ReferenceEquals(this, other)) return true;

		return Equals(Uni, other.Uni) && Equals(Upload, other.Upload) && Size == other.Size &&
		       (!OperatingSystem.IsWindows());
	}

	public override bool Equals(object obj)
	{
		return ReferenceEquals(this, obj) || (obj is SearchQuery other && Equals(other));
	}

	public override int GetHashCode()
	{
		// return HashCode.Combine(Uni, Upload, Size);
		return HashCode.Combine(Uni);

		// return Uni.GetHashCode();
	}

	public static bool operator ==(SearchQuery left, SearchQuery right)
	{
		return Equals(left, right);
	}

	public static bool operator !=(SearchQuery left, SearchQuery right)
	{
		return !Equals(left, right);
	}

	#endregion

	public bool LoadFile(string fn = null)
	{
		if (!HasFile && HasUni) {
			FilePath = GetFilePathOrTemp(fn);

		}

		return HasFile;
	}

	public bool DeleteFile()
	{
		if (File.Exists(FilePath)) {
			File.Delete(FilePath);
			FilePath = null;
		}

		return !HasFile;
	}

	[MustUseReturnValue]
	public string WriteToFile(Action<IImageProcessingContext> operation = null, [CanBeNull] string fn = null)
	{
		if (!HasUni) {
			throw new InvalidOperationException();
		}

		string t;
		fn ??= Path.GetTempFileName();

		var encoder = new PngEncoder();

		using Image image = ISImage.Load(Uni.Stream);

		if (operation != null) {
			image.Mutate(operation);

		}

		image.Save(fn, encoder);
		
		Uni.Stream.TrySeek();

		return fn;
	}

	[MustUseReturnValue]
	[ICBN]
	public string GetFilePathOrTemp(string fn = null)
	{
		if (!HasUni) {
			throw new InvalidOperationException($"{nameof(HasUni)} is {false}");
		}

		string t;
		fn ??= Path.GetTempFileName();

		if (!Uni.IsFile) {
			t = Path.Combine(Path.GetTempPath(), fn);

			using FileStream fs = File.Create(t);

			bool s = Uni.Stream.CanSeek;

			if (s) {
				Uni.Stream.Position = 0;
			}

			Uni.Stream.CopyTo(fs);
			fs.Flush();

		}
		else {
			t = Uni.Value.ToString();

		}

		return t;
	}

}
#endif