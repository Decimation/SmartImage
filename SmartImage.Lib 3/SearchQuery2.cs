using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Net;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics.X86;
using System.Security.Cryptography;
using Flurl.Http;
using JetBrains.Annotations;
using Microsoft;
using Novus.FileTypes;
using Novus.FileTypes.Uni;
using Novus.Streams;
using Novus.Win32;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Processing;
using SmartImage.Lib.Engines;
using SmartImage.Lib.Engines.Impl.Upload;
using SmartImage.Lib.Model;
using SmartImage.Lib.Results;
using SmartImage.Lib.Utilities;

[assembly: InternalsVisibleTo("SmartImage")]
[assembly: InternalsVisibleTo("SmartImage.UI")]
[assembly: InternalsVisibleTo("SmartImage.Rdx")]

namespace SmartImage.Lib;

public enum QueryType
{

	File,
	Uri,
	Stream

}

public sealed class SearchQuery2 : IDisposable, IEquatable<SearchQuery2>, IItemSize
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

	public bool IsUploading { get; private set; }

	[MNNW(true, nameof(Value))]
	public bool HasValue => Value != null;

	internal SearchQuery2([MN] object f)
	{
		Value = f;

		// Size = Uni == null ? default : Uni.Stream.Length;
	}

	internal SearchQuery2([MN] object f, Stream s, QueryType type)
	{
		Value  = f;
		Stream = s;
		Type   = type;

		// Size = Uni == null ? default : Uni.Stream.Length;
	}

	private SearchQuery2() : this(null) { }

	static SearchQuery2() { }

	public static readonly SearchQuery2 Null = new();

	[MN]
	public IImageFormat ImageInfo { get; private set; }

	[MNNW(true, nameof(ImageInfo))]
	public bool HasImage => ImageInfo != null;

	public static async Task<SearchQuery2> Decode(object o, CancellationToken t = default)
	{
		Stream       str;
		IImageFormat fmt;
		QueryType    qt;

		if (IsFile(o, out var fi)) {
			// var s = ((FileInfo) fi).FullName;
			var s = (string) o;
			str = File.OpenRead(s);
			qt  = QueryType.File;
		}

		else if (IsUri(o, out var url2)) {
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

		fmt = await Image.DetectFormatAsync(str);
		str.TrySeek();

		var sq2 = new SearchQuery2(o, str, qt)
		{
			ImageInfo = fmt
		};

		return sq2;
	}

	public static bool IsUri(object o, out object u)
	{
		Url ux2 = o switch
		{
			Url u2   => u2,
			string s => s,
			_        => null
		};
		u = ux2;
		return Url.IsValid(ux2);
	}

	public static bool IsFile(object o, out object f)
	{
		f = null;

		if (o is string { } s && File.Exists(s)) {
			f = new FileInfo(s);
		}

		return f != null;
	}

	public static async Task<IFlurlResponse> HandleUriAsync(Url value, CancellationToken ct)
	{
		// value = value.CleanString();

		var res = await value.AllowAnyHttpStatus()
			          .WithHeaders(new
			          {
				          // todo
				          // User_Agent = ER.UserAgent,
			          })
			          .GetAsync(cancellationToken: ct);

		if (res.ResponseMessage.StatusCode == HttpStatusCode.NotFound) {
			throw new ArgumentException($"{value} returned {HttpStatusCode.NotFound}");
		}

		return res;
	}

	public static async Task<SearchQuery2> TryCreateAsync(string value, CancellationToken ct = default)
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

			var sq = new SearchQuery2(uf)
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

		IsUploading = false;

		return Upload;
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

	public void Dispose()
	{
		Stream?.Dispose();

		// ImageInfo?.Dispose();
		Debug.WriteLine($"Disposing {ValueString} w/ {Size}");
	}

	public override string ToString()
	{
		string s = $"{Stream} | {ValueString}";

		return s;
	}

	#region Equality members

	public bool Equals(SearchQuery2 other)
	{
		if (ReferenceEquals(null, other)) return false;
		if (ReferenceEquals(this, other)) return true;

		return Equals(Stream, other.Stream) && Equals(Upload, other.Upload) && Size == other.Size &&
		       (!OperatingSystem.IsWindows());
	}

	public override bool Equals(object obj)
	{
		return ReferenceEquals(this, obj) || (obj is SearchQuery2 other && Equals(other));
	}

	public override int GetHashCode()
	{
		// return HashCode.Combine(Uni, Upload, Size);
		return HashCode.Combine(Stream);

		// return Uni.GetHashCode();
	}

	public static bool operator ==(SearchQuery2 left, SearchQuery2 right)
	{
		return Equals(left, right);
	}

	public static bool operator !=(SearchQuery2 left, SearchQuery2 right)
	{
		return !Equals(left, right);
	}

	#endregion

	[MustUseReturnValue]
	public string WriteToFile(Action<IImageProcessingContext> operation = null, [CanBeNull] string fn = null)
	{
		if (!HasValue) {
			throw new InvalidOperationException();
		}

		string t;
		fn ??= Path.GetTempFileName();

		var encoder = new PngEncoder();

		using Image image = ISImage.Load(Stream);

		if (operation != null) {
			image.Mutate(operation);

		}

		image.Save(fn, encoder);

		Stream.TrySeek();

		return fn;
	}

}