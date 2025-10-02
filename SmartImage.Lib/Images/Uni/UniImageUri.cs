// Author: Deci | Project: SmartImage.Lib | Name: UniImageUri.cs
// Date: 2024/07/17 @ 02:07:26

using System.Collections.Immutable;
using System.Net;
using System.Text.Json.Serialization;
using Flurl.Http;
using Kantan.Net.Utilities;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp.PixelFormats;
using SmartImage.Lib.Engines.Results;

namespace SmartImage.Lib.Images.Uni;

public class UniImageUri : UniImage
{
	[MN]
	[JPN("url")]
	public Url Url { get; protected internal set; }

	internal UniImageUri(Url url) : base(url?.ToString(), UniImageType.Uri)
	{
		Url = url;
	}


	protected override async Task<bool> AllocAsync(CancellationToken ct = default)
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

	public static bool IsUriType(object o, out Url u)
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

		return ImageScanner.LegalSchemes.Contains(scheme);
	}

}