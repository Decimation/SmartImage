// Author: Deci | Project: SmartImage.Lib | Name: UniImageUrl.cs
// Date: 2024/07/17 @ 02:07:26

using AngleSharp.Html.Parser;
using Flurl.Http;
using Kantan.Net.Utilities;
using Microsoft.Extensions.Logging;
using Microsoft.IO;
using SixLabors.ImageSharp.PixelFormats;
using SmartImage.Lib.Engines.Results;
using SmartImage.Lib.Model;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Net;
using System.Text.Json.Serialization;
using System.Threading.Channels;

namespace SmartImage.Lib.Images.Uni;

public class UniImageUrl : UniImage, IUrl
{

	[MN]
	[JPN("url")]
	public Url Url { get; protected internal set; }

	internal UniImageUrl(Url url) : base(url?.ToString(), UniImageType.Uri)
	{
		Url = url;
	}

	// public override string Name => Url?.GetFileName();


	public async ValueTask<bool> ScanAsync<TUniImageUrl>(ChannelWriter<TUniImageUrl> cw, Func<string, TUniImageUrl> newItem, CancellationToken ct = default)
		where TUniImageUrl : UniImageUrl
	{
		var allocImageAsync = await AllocImageAsync(ct);

		if (allocImageAsync) {
			cw.TryComplete();
			return true;
		}

		await using var stream = GetStream();

		using var sr  = new StreamReader(stream);
		var       str = await sr.ReadToEndAsync(ct);

		var       hp   = new HtmlParser();
		var       urls = ImageScanner.ParseImageUrlsByRegex(str, Url);
		using var doc  = await hp.ParseDocumentAsync(str);

		await cw.WaitToWriteAsync(ct);

		await Parallel.ForEachAsync(urls, ct, async (s, token) =>
		{
			var item = newItem(s);

			var allocImgOk = await item.AllocImageAsync(token);

			if (allocImgOk) {
				await cw.WriteAsync(item, token);
			}
			else {
				item?.Dispose();
			}
		});
		cw.TryComplete();

		return true;
	}

	protected override async ValueTask<bool> AllocAsync(CancellationToken ct = default)
	{
		IFlurlResponse fres = null;

		if (HasBytes) {
			goto ret;
		}

		fres = await ImageScanner.GetResponseAsync(Url, ct);

		if (fres == null) {
			goto ret;
		}

		Bytes = await fres.GetBytesAsync();

	ret:
		fres?.Dispose();
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