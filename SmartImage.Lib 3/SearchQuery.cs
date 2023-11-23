global using MN = System.Diagnostics.CodeAnalysis.MaybeNullAttribute;
global using CBN = JetBrains.Annotations.CanBeNullAttribute;
global using NN = System.Diagnostics.CodeAnalysis.NotNullAttribute;
global using MNNW = System.Diagnostics.CodeAnalysis.MemberNotNullWhenAttribute;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using Flurl.Http;
using JetBrains.Annotations;
using Novus.FileTypes;
using SmartImage.Lib.Engines;
using SmartImage.Lib.Engines.Impl.Upload;
using SmartImage.Lib.Utilities;

[assembly: InternalsVisibleTo("SmartImage")]
[assembly: InternalsVisibleTo("SmartImage.UI")]
[assembly: InternalsVisibleTo("SmartImage.Linux")]

namespace SmartImage.Lib;

public sealed class SearchQuery : IDisposable, IEquatable<SearchQuery>
{

	[MN]
	public UniSource Uni { get; }

	[MN]
	public Url Upload { get; internal set; }

	public long Size { get; private set; }

	[CBN]
	public string ValueString => HasUni ? Uni.Value.ToString() : null;

	[MNNW(true, nameof(Upload))]
	public bool IsUploaded => Url.IsValid(Upload);

	public bool IsUploading { get; private set; }

	[MNNW(true, nameof(Uni))]
	public bool HasUni => Uni != null;

	internal SearchQuery([CBN] UniSource f)
	{
		Uni  = f;
		Size = Uni == null ? default : Uni.Stream.Length;
	}

	private SearchQuery() : this(null) { }

	public static readonly SearchQuery Null = new();

	static SearchQuery() { }

	[MN]
	public Image Image { get; private set; }

	[MNNW(true, nameof(Image))]
	public bool HasImage => Image != null;

	public bool LoadImage()
	{
		if (HasUni && Image == null) {
			Image = Image.FromStream(Uni.Stream);
			return true;
		}

		return false;
	}

	public static async Task<SearchQuery> TryCreateAsync(string value, CancellationToken ct = default)
	{
		var uf = await UniSource.TryGetAsync(value, ct: ct, whitelist: FileType.Image);

		if (uf == null) {
			return Null;

		}
		else {
			var sq = new SearchQuery(uf)
				{ };

			return sq;

		}
	}

	public async Task<Url> UploadAsync(BaseUploadEngine engine = null, CancellationToken ct = default)
	{
		if (IsUploaded) {
			return Upload;
		}

		IsUploading = true;

		var fu = Uni.Value.ToString();

		if (Uni.IsUri) {
			Upload = fu;
			// Size   = BaseSearchEngine.NA_SIZE;
			Debug.WriteLine($"Skipping upload for {Uni.Value}", nameof(UploadAsync));
		}
		else {
			// fu = await test(fu);

			engine ??= BaseUploadEngine.Default;

			var u   = await engine.UploadFileAsync(fu, ct);
			var url = u.Url;

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
			Size = u.Size ?? Size;
			u.Dispose();
		}

		IsUploading = false;

		return Upload;
	}

	public static bool IsValidSourceType(object str)
	{
		var v        = UniHandler.GetUniType(str, out var o2);
		var isFile   = v == UniSourceType.File;
		var isUri    = v == UniSourceType.Uri;
		var isStream = v == UniSourceType.Stream;
		var ok       = isFile || isUri || isStream;

		if (isFile) {
			var ext = Path.GetExtension(str.ToString())?[1..];
			return FileType.Image.Any(x => x.Subtype == ext);
		}

		return ok;
	}

	public void Dispose()
	{
		Uni?.Dispose();
		Image?.Dispose();
		Debug.WriteLine($"Disposing {ValueString} w/ {Size}");
	}

	public override string ToString()
	{
		var s = $"{Uni}";

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

	[MustUseReturnValue]
	public async Task<(string, bool)> GetFilePathOrTempAsync(string fn = null)
	{
		string t;
		fn ??= Path.GetTempFileName();
		bool b;

		if (!Uni.IsFile) {
			t = Path.Combine(Path.GetTempPath(), fn);

			await using (var fs = File.Create(t)) {
				var s = Uni.Stream.CanSeek;

				if (s) {
					Uni.Stream.Position = 0;
				}

				await Uni.Stream.CopyToAsync(fs);
				fs.Flush();
			}

			b = true;
		}
		else {
			t = Uni.Value.ToString();
			b = false;
		}

		return (t, b);
	}

}