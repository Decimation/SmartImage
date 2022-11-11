global using MN = System.Diagnostics.CodeAnalysis.MaybeNullAttribute;
global using CBN = JetBrains.Annotations.CanBeNullAttribute;
global using NN = System.Diagnostics.CodeAnalysis.NotNullAttribute;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Flurl.Http;
using Kantan.Net.Utilities;
using Novus.FileTypes;
using Kantan.Text;
using SmartImage.Lib.Engines.Upload;

[assembly: InternalsVisibleTo("SmartImage")]

namespace SmartImage.Lib;

//todo: UniFile

public sealed class SearchQuery : IDisposable
{
	public UniFile Uni { get; }

	[MN]
	public Url Upload { get; private set; }

	public long Size { get; private set; }

	internal SearchQuery([MN] UniFile f)
	{
		Uni = f;
	}

	public static readonly SearchQuery Null = new(null);

	public static async Task<SearchQuery> TryCreateAsync(string value)
	{
		var uf = await UniFile.TryGetAsync(value, whitelist: FileType.Find("image").ToArray());

		var sq = new SearchQuery(uf)
			{ };

		return sq;
	}

	public async Task<Url> UploadAsync(BaseUploadEngine engine = null)
	{
		if (Uni.IsUri) {
			Upload = Uni.Value;
			Size   = -1; // todo: indeterminate/unknown size
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

	#region IDisposable

	public void Dispose()
	{
		Uni.Dispose();
	}

	#endregion

	#region Overrides of Object

	public override string ToString()
	{
		return Uni.ToString();
	}

	#endregion

	public static bool IsIndicatorValid(string str)
	{
		return Url.IsValid(str) || File.Exists(str);
	}
}