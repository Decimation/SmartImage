// Author: Deci | Project: SmartImage.Lib | Name: AllocImageUrl.cs
// Date: 2024/07/17 @ 02:07:26

using Flurl.Http;
using Kantan.Net.Utilities;
using SmartImage.Lib.Model;

namespace SmartImage.Lib.Images.Alloc;


public class AllocImageUrl : AllocImageStream, IUrl
{

	[MN]
	[JPN("url")]
	public Url Url { get; protected set; }

	public override string Name => Url.GetFileName();


	// public override string Name => Url?.GetFileName();


	internal AllocImageUrl(Url url) : base(url?.ToString(), AllocImageType.Uri)
	{
		Url = url;
	}

	public override async Task<bool> AllocSourceAsync(CancellationToken ct = default)
	{
		IFlurlResponse response = null;

		if (HasSource) {
			goto ret;
		}

		response = await ImageScanner.GetResponseAsync(Url, ct);

		if (response is null or {StatusCode: 403}) {
			goto ret;
		}

		Source = await response.GetBytesAsync();

	ret:

		response?.Dispose();
		return HasSource;

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