// Author: Deci | Project: SmartImage.Lib | Name: UniImageUri.cs
// Date: 2024/07/17 @ 02:07:26

using System.Collections.Immutable;
using System.Net;
using Flurl.Http;
using SixLabors.ImageSharp.PixelFormats;

namespace SmartImage.Lib.Images.Uni;

public class UniImageUri : UniImage
{

	public Url Url { get; }

	internal UniImageUri(object value, Url url)
		: base(value, UniImageType.Uri)
	{
		Url = url;
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

		return LegalSchemes.Contains(scheme);
	}


	public static readonly ImmutableArray<string> RestrictedSchemes = ["file", "javascript", "cpu"];

	public static readonly ImmutableArray<string> LegalSchemes = ["http", "https"];

#region Overrides of UniImage

	public override async Task<bool> AllocImageAsync(CancellationToken ct = default)
	{
		if (!HasImage) {
			try {
				// Stream     = File.OpenRead(fullName);

				using var fres = await GetResponseAsync(Url, ct);

				if (fres == null) {
					return false;
				}

				var res = await fres.GetStreamAsync();
				Size  = res.Length;
				Image = await ISImage.LoadAsync<Rgba32>(res, ct);

			}
			catch (Exception exception) {
				return false;
			}

		}

		return HasImage;

	}

#endregion

	public static async ValueTask<IFlurlResponse> GetResponseAsync(Url value, CancellationToken ct)
	{
		// value = value.CleanString();
		/*if (value.Scheme == "javascript") {
			throw new ArgumentException($"{value}");
		}*/

		var req1 = await ImageScanner.Client.Request(value).GetAsync(cancellationToken: ct);

		// var req  = ValueTask.FromResult(req1);

		/*.AllowAnyHttpStatus()
		.WithHeaders(new
		{
			// todo
			User_Agent = R1.UserAgent1,
		});*/

		// var res = await req.GetAsync(cancellationToken: ct);

		/*
		if (res.ResponseMessage.StatusCode == HttpStatusCode.NotFound) {
			throw new ArgumentException($"{value} returned {HttpStatusCode.NotFound}");

		}
		*/

		return req1;
	}

}