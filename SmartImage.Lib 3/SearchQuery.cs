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

public sealed class SearchQuery : IDisposable
{
	public UniFile Uni { get; }

	[MN]
	public Url Upload { get; private set; }

	public long Size { get; private set; }

	public Image Image { get; private set; }

	internal SearchQuery([MN] UniFile f)
	{
		Uni = f;
	}

	public static readonly SearchQuery Null = new(null);

	static SearchQuery() { }

	public static async Task<SearchQuery> TryCreateAsync(string value)
	{
		var uf = await UniFile.TryGetAsync(value, whitelist: FileType.Image);

		var sq = new SearchQuery(uf)
			{ };

		return sq;
	}

	public async Task<Url> UploadAsync(BaseUploadEngine engine = null)
	{
		if (Uni.IsUri) {
			Upload = Uni.Value;
			Size   = BaseSearchEngine.NA_SIZE;
			Debug.WriteLine($"Skipping upload for {Uni.Value}", nameof(UploadAsync));
		}
		else {
			engine ??= BaseUploadEngine.Default;
			var u = await engine.UploadFileAsync(Uni.Value);
			Upload = u;
			Size   = engine.Size;
		}

		return Upload;
	}

	[SupportedOSPlatform(Global.OS_WIN)]
	public bool LoadImage()
	{
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

	#region IDisposable

	public void Dispose()
	{
		if (OperatingSystem.IsWindows()) {
			Image?.Dispose();
		}

		Uni.Dispose();
	}

	#endregion

	#region Overrides of Object

	public override string ToString()
	{
		var s = $"{Uni}";

		if (OperatingSystem.IsWindows()) {
			s += $" {Image?.PhysicalDimension}";
		}
		return s;
	}

	#endregion

	public static bool IsUriOrFile(string str)
	{
		var (f, u) = UniFile.IsUriOrFile(str);
		return f || u;
	}
}