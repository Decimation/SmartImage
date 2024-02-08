﻿global using MN = System.Diagnostics.CodeAnalysis.MaybeNullAttribute;
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
using Novus.FileTypes;
using Novus.Streams;
using Novus.Win32;
using SixLabors.ImageSharp;
using SmartImage.Lib.Engines;
using SmartImage.Lib.Engines.Impl.Upload;
using SmartImage.Lib.Model;
using SmartImage.Lib.Results;
using SmartImage.Lib.Utilities;

[assembly: InternalsVisibleTo("SmartImage")]
[assembly: InternalsVisibleTo("SmartImage.UI")]
[assembly: InternalsVisibleTo("SmartImage.Rdx")]

namespace SmartImage.Lib;

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

	internal SearchQuery([CBN] UniSource f)
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
		UniSource uf = await UniSource.TryGetAsync(value, ct: ct, whitelist: FileType.Image);

		if (uf == null) {
			return Null;

		}

		else {
			if (uf.IsUri) {
				var r    = await (uf.Value as Url).GetAsync();
				var uri2 = r.ResponseMessage.RequestMessage.RequestUri.ToString();

				if (uri2 == "https://i.imgur.com/removed.png") {
					return Null;
				}
			}

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

			UploadResult u   = await engine.UploadFileAsync(fu, ct);
			Url          url = u.Url;

			if (!u.IsValid) {
				url = null;
				Debug.WriteLine($"{u} is invalid!");

				// Debugger.Break();
			}

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
		UniSourceType v        = UniHandler.GetUniType(str, out object o2);
		bool          isFile   = v == UniSourceType.File;
		bool          isUri    = v == UniSourceType.Uri;
		bool          isStream = v == UniSourceType.Stream;
		bool          ok       = isFile || isUri || isStream;

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