global using MN = System.Diagnostics.CodeAnalysis.MaybeNullAttribute;
global using CBN = JetBrains.Annotations.CanBeNullAttribute;
global using NN = System.Diagnostics.CodeAnalysis.NotNullAttribute;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using AngleSharp.Js.Dom;
using Flurl.Http;
using Kantan.Net.Utilities;
using Novus.FileTypes;
using Kantan.Text;
using Novus;
using SmartImage.Lib.Engines;
using SmartImage.Lib.Engines.Upload;

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

	public async Task<Url> UploadAsync(BaseUploadEngine engine = null)
	{
		if (Uni.IsUri) {
			Upload = Uni.Value.ToString();
			Size   = BaseSearchEngine.NA_SIZE;
			Debug.WriteLine($"Skipping upload for {Uni.Value}", nameof(UploadAsync));
		}
		else {
			engine ??= BaseUploadEngine.Default;
			var u = await engine.UploadFileAsync(Uni.Value.ToString());
			Upload = u;
			Size   = engine.Size;
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
			Image = Image.FromStream(Uni.Stream, useEmbeddedColorManagement: false, validateImageData: false);
			Debug.WriteLine($"Loaded image: {Image.PhysicalDimension}", nameof(LoadImage));
			return true;
		}
		catch (Exception e) {
			Debug.WriteLine($"{e.Message}", nameof(LoadImage));
			return false;
		}

	}

	public static bool IsUriOrFile(string str)
	{
		var (f, u) = UniSource.IsUriOrFile(str);
		return f || u;
	}

	public void Dispose()
	{
		if (OperatingSystem.IsWindows()) {
			Image?.Dispose();
		}

		Uni.Dispose();
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
}