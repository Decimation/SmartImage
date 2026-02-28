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
	public Url Url { get; protected set; }

	internal UniImageUrl(Url url) : base(url?.ToString(), UniImageType.Uri)
	{
		Url = url;
	}


	public override string Name => Url.GetFileName();


	// public override string Name => Url?.GetFileName();


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

	public static async ValueTask<bool> ScanAsync(Url u, ChannelWriter<IUniImage> cw, Func<string, IUniImage> newItem, CancellationToken ct = default)
	{
		bool ok = false;
		var  ui = await TryCreateAsync(u, autoInit: true, autoDisposeOnError: false, ct: ct) as UniImageUrl;

		if (ui == null) {
			s_logger.LogError("{Url} is null", u);
			return cw.TryComplete();
		}

		/*var (allocOk, allocImgOk) = await ui.AllocAll(ct);

		if (allocImgOk) {
			await cw.WriteAsync((T) ui, ct);
			cw.TryComplete();
			return true;
		}*/

		return await ui.ScanAsync(cw, newItem, ct);
	}


	
	public async ValueTask<bool> ScanAsync(ChannelWriter<IUniImage> cw, Func<string, IUniImage> newItem, CancellationToken ct = default)
	{
		var (allocOk, allocImgOk) = await AllocAll(ct);

		if (allocImgOk) {
			await cw.WriteAsync(this, ct);
			cw.TryComplete();
			return true;
		}

		await using var stream = GetStream();

		using var sr  = new StreamReader(stream);
		var       str = await sr.ReadToEndAsync(ct);

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

}