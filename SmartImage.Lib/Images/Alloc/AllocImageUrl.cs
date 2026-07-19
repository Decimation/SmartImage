// Author: Deci | Project: SmartImage.Lib | Name: AllocImageUrl.cs
// Date: 2024/07/17 @ 02:07:26

using System.Threading.Channels;
using Flurl.Http;
using Kantan.Net.Utilities;
using Microsoft.Extensions.Logging;
using SmartImage.Lib.Model;

namespace SmartImage.Lib.Images.Alloc;

public class AllocImageUrl : AllocImage, IUrl
{

	[MN]
	[JPN("url")]
	public Url Url { get; protected set; }

	internal AllocImageUrl(Url url) : base(url?.ToString(), UniImageType.Uri)
	{
		Url = url;
	}

	public override string Name => Url.GetFileName();


	// public override string Name => Url?.GetFileName();


	public override async Task<bool> AllocSourceAsync(CancellationToken ct = default)
	{
		IFlurlResponse response = null;

		if (HasBytes) {
			goto ret;
		}

		response = await ImageScanner.GetResponseAsync(Url, ct);

		if (response is null or {StatusCode: 403}) {
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

	// todo: create IImageScanner type

	public virtual async ValueTask<bool> ScanAsync(ChannelWriter<IAllocImage> cw, Func<Url, IAllocImage> f, CancellationToken ct = default)
	{
		var (allocOk, allocImgOk) = await AllocAllAsync(ct);

		if (allocImgOk) {
			await cw.WriteAsync((IAllocImage) this, ct);
			cw.TryComplete();
			return true;
		}

		await using var stream = GetSource();

		using var sr  = new StreamReader(stream);
		var       str = await sr.ReadToEndAsync(ct);

		var urls = ImageScanner.ParseImageUrls(str, Url).ToArray();
		s_logger.LogInformation("Parsed {Cnt} urls from {Url}", urls.Length, Url);


		await cw.WaitToWriteAsync(ct);

		await Parallel.ForEachAsync(urls, ct, async (s, token) =>
		{
			var item = f(s);

			var (allocOk2, allocImgOk2) = await item.AllocAllAsync(token);

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