// Author: Deci | Project: SmartImage.Lib | Name: UniImageUrl.cs
// Date: 2024/07/17 @ 02:07:26

using Flurl.Http;
using Kantan.Net.Utilities;
using Microsoft.Extensions.Logging;
using SmartImage.Lib.Model;
using System.Threading.Channels;
using SmartImage.Lib.Engines.Results;

namespace SmartImage.Lib.Images.Uni;

public class UniImageUrl : UniImage, IUrl
{

	[MN]
	[JPN("url")]
	public override Url Url { get; set; }

	internal UniImageUrl(Url url) : base(url?.ToString(), UniImageType.Uri)
	{
		Url = url;
	}


	public override string Name => Url.GetFileName();


	// public override string Name => Url?.GetFileName();

	public static async ValueTask<bool> ScanAsync(Url u, ChannelWriter<UniImageUrl> cw, CancellationToken ct = default)
	{
		bool ok = false;
		var  ui = await TryCreateAsync(u, autoInit: true, autoDisposeOnError: false, ct: ct) as UniImageUrl;

		if (ui == null) {
			s_logger.LogError("{Url} is null", u);
			return cw.TryComplete();
		}

		return await ui.ScanAsync(cw, s => { return new UniImageUrl(s); }, ct);
	}


	/// <summary>
	/// Scans for images within the webpage located at <see cref="Url"/>; if <see cref="Url"/> itself (<code>this</code>)
	/// points to binary image data, it is returned. todo: update this doc
	/// </summary>
	public async ValueTask<bool> ScanAsync<TUniUrl>(ChannelWriter<TUniUrl> cw, Func<string, TUniUrl> newItem, CancellationToken ct = default)
		where TUniUrl : IResultItem, IUniImage
	{
		var (allocOk, allocImgOk) = await AllocAll(ct);

		if (allocImgOk) {
			await cw.WriteAsync((TUniUrl) (this as IResultItem), ct);
			cw.TryComplete();
			return true;
		}

		await using var stream = GetStream();

		using var sr   = new StreamReader(stream);
		var       str  = await sr.ReadToEndAsync(ct);

		var urls = ImageScanner.ParseImageUrlsByRegex(str, Url);

		await cw.WaitToWriteAsync(ct);

		await Parallel.ForEachAsync(urls, ct, async (s, token) =>
		{
			var item = newItem(s);

			var (allocOk2, allocImgOk2) = await item.AllocAll(token);

			if (allocImgOk2) {
				await cw.WriteAsync(item, token);
			}
			else {
				item?.Dispose();
			}
		});

		var ok = cw.TryComplete();

		return ok;
	}

	public override async ValueTask<bool> AllocSourceAsync(CancellationToken ct = default)
	{
		IFlurlResponse response = null;

		if (HasBytes) {
			goto ret;
		}

		response = await ImageScanner.GetResponseAsync(Url, ct);

		if (response == null) {
			goto ret;
		}

		Bytes = await response.GetBytesAsync();

	ret:

		response?.Dispose();
		return HasBytes;

	}

	public static bool IsUrlType(object o, out Url u)
	{
		u = o switch
		{
			Url u2                       => u2,
			string s when Url.IsValid(s) => s,
			_                            => null
		};

		if (u == null) {
			return false;
		}

		var scheme = u.Scheme;

		return ImageScanner.LegalSchemeWhitelist.Contains(scheme);
	}

}