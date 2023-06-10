global using MN = System.Diagnostics.CodeAnalysis.MaybeNullAttribute;
global using CBN = JetBrains.Annotations.CanBeNullAttribute;
global using NN = System.Diagnostics.CodeAnalysis.NotNullAttribute;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using Flurl.Http;
using JetBrains.Annotations;
using Novus.FileTypes;
using Novus;
using SmartImage.Lib.Engines;
using SmartImage.Lib.Engines.Impl.Upload;
using SmartImage.Lib.Utilities;
using Image = System.Drawing.Image;

[assembly: InternalsVisibleTo("SmartImage")]

namespace SmartImage.Lib;

public sealed class SearchQuery : IDisposable, IEquatable<SearchQuery>
{
	public UniSource Uni { get; }

	[MN]
	public Url Upload { get; private set; }

	public long Size { get; private set; }

	[SupportedOSPlatform(Global.OS_WIN)]
	public Image Image { get; private set; }

	internal SearchQuery([MN] UniSource f)
	{
		Uni = f;
	}

	public static readonly SearchQuery Null = new(null);

	static SearchQuery() { }

	public static async Task<SearchQuery> TryCreateAsync(string value)
	{
		var uf = await UniSource.TryGetAsync(value, whitelist: FileType.Image);

		var sq = new SearchQuery(uf)
			{ };

		return sq;
	}

	public async Task<Url> UploadAsync(BaseUploadEngine engine = null, CancellationToken ct = default)
	{

		var fu = Uni.Value.ToString();

		if (Uni.IsUri) {
			Upload = fu;
			Size   = BaseSearchEngine.NA_SIZE;
			Debug.WriteLine($"Skipping upload for {Uni.Value}", nameof(UploadAsync));
		}
		else {
			// fu = await test(fu);

			engine ??= BaseUploadEngine.Default;

			var u = await engine.UploadFileAsync(fu, ct);

			/*if (!u.IsValid) {
				engine = BaseUploadEngine.All[Array.IndexOf(BaseUploadEngine.All, engine) + 1];
				Debug.WriteLine($"{u.Response.ResponseMessage} failed, retrying with {engine.Name}");
				u = await engine.UploadFileAsync(Uni.Value.ToString(), ct);
			}*/

			Upload = u.Url;

			if (u.Response is { }) {
				Size = NetHelper.GetContentLength(u.Response) ?? 0;
			}

			u.Dispose();
		}

		return Upload;
	}

	public bool LoadImage()
	{
		if (!OperatingSystem.IsWindows()) {
			return false;
		}

		if (Image != null) {
			return true;
		}

		try {
#if ALT
			Image = Image.FromStream(Uni.Stream, useEmbeddedColorManagement: false, validateImageData: false);
			Debug.WriteLine($"Loaded image: {Image.PhysicalDimension}", nameof(LoadImage));
#endif

			return true;
		}
		catch (Exception e) {
			Debug.WriteLine($"{e.Message}", nameof(LoadImage));
			return false;
		}

	}

	public static bool IsValidSourceType(object str)
	{
		var v = UniSource.GetSourceType(str);
		return v is UniSourceType.Uri or UniSourceType.File or UniSourceType.Stream;
	}

	public void Dispose()
	{
		if (OperatingSystem.IsWindows()) {
			Image?.Dispose();
		}

		Uni?.Dispose();
	}

	public override string ToString()
	{
		var s = $"{Uni}";

		if (OperatingSystem.IsWindows()) {
			s += $" {Image?.PhysicalDimension}";
		}

		return s;
	}

	#region Equality members

	public bool Equals(SearchQuery other)
	{
		if (ReferenceEquals(null, other)) return false;
		if (ReferenceEquals(this, other)) return true;

		return Equals(Uni, other.Uni) && Equals(Upload, other.Upload) && Size == other.Size &&
		       (!OperatingSystem.IsWindows() || Equals(Image, other.Image));
	}

	public override bool Equals(object obj)
	{
		return ReferenceEquals(this, obj) || (obj is SearchQuery other && Equals(other));
	}

	public override int GetHashCode()
	{
		unchecked {
			int hashCode = (Uni != null ? Uni.GetHashCode() : 0);
			hashCode = (hashCode * 397) ^ (Upload != null ? Upload.GetHashCode() : 0);
			hashCode = (hashCode * 397) ^ Size.GetHashCode();

			if (OperatingSystem.IsWindows()) {
				hashCode = (hashCode * 397) ^ (Image != null ? Image.GetHashCode() : 0);
			}

			return hashCode;
		}
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