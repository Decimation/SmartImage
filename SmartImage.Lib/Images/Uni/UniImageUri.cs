// Author: Deci | Project: SmartImage.Lib | Name: UniImageUri.cs
// Date: 2024/07/17 @ 02:07:26

using System.Net;
using Flurl.Http;

namespace SmartImage.Lib.Images.Uni;

public class UniImageUri : UniImage
{

	public IFlurlResponse Response { get; private set; }

	[MNNW(true, nameof(Response))]
	public bool HasResponse => Response != null;

	public Url Url { get; }

	internal UniImageUri(object value, Url url, IFlurlResponse response = null)
		: base(value, Stream.Null, UniImageType.Uri)
	{
		Url      = url;
		Response = response;
	}

	public override string WriteToFile(string fn = null)
		=> WriteStreamToFile(fn);

	public static bool IsUriType(object o, out Url u)
	{
		u = o switch
		{
			Url u2   => u2,
			string s => s,
			_        => null
		};

		var scheme = u.Scheme;

		return Url.IsValid(u) && Schemes.All(s => scheme != s);
	}

	public override async ValueTask<bool> Alloc(CancellationToken ct = default)
	{
		if (!HasResponse) {
			Response = await GetResponseAsync(Url, ct);
		}

		if (!HasStream) {
			Stream = await Response.GetStreamAsync();
		}

		return HasResponse && HasStream;
	}

	public async ValueTask<bool> AllocResponseAsync(CancellationToken ct = default)
	{
		Response = await GetResponseAsync(Url, ct);

		return HasResponse;
	}

	public static readonly string[] Schemes = ["file", "javascript"];

	public static async ValueTask<IFlurlResponse> GetResponseAsync(Url value, CancellationToken ct)
	{
		// value = value.CleanString();
		/*if (value.Scheme == "javascript") {
			throw new ArgumentException($"{value}");
		}*/

		var req = await ImageScanner.BuildRequest(value);
		/*.AllowAnyHttpStatus()
		.WithHeaders(new
		{
			// todo
			User_Agent = R1.UserAgent1,
		});*/

		var res = await req
			          .GetAsync(cancellationToken: ct);

		if (res.ResponseMessage.StatusCode == HttpStatusCode.NotFound) {
			throw new ArgumentException($"{value} returned {HttpStatusCode.NotFound}");

		}

		return res;
	}

}