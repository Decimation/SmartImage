using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Flurl.Http;
using SmartImage.Lib.Utilities;

namespace SmartImage.Lib.Clients.Booru;

// TODO

[Experimental(AppSupport.DIAG_ID_EXPERIMENTAL)]
public abstract class BaseGelbooruClient : BaseBooruClient
{

	public const int POST_MAX = 100;

	public FlurlClient Client { get; }

	[CBN]
	public string Key { get; set; }

	[CBN]
	public string Id { get; set; }

	protected BaseGelbooruClient(Url url) : base(url)
	{

		Client = new FlurlClient()
		{
			BaseUrl = url,
			Settings =
			{
				JsonSerializer = { }
			}
		};
	}

	public static int PostMax { get; protected set; } = POST_MAX;

	public virtual Task<IFlurlResponse> GetPostsAsync(GelbooruPostsRequest r)
	{
		var properties = new List<string>();

		foreach (PropertyInfo p in r.GetType().GetProperties()) {
			var o = p.GetValue(r);

			if (o == null || o is string s && String.IsNullOrWhiteSpace(s) || o.Equals(0)) {
				continue;
			}

			var sss = o.ToString();
			var h   = p.Name.ToLower() + "=" + Url.Encode(sss, true);
			properties.Add(h);
		}

		var ss = String.Join('&', properties);

		return Client.Request("/index.php?page=post&s=list", ss)
			.GetAsync();
	}

	public override void Dispose()
	{
		Client?.Dispose();
		GC.SuppressFinalize(this);
	}

}

public class GelbooruPostsRequest
{

	public int Limit
	{
		get;
		set => field = Math.Clamp(value, 1, BaseGelbooruClient.PostMax);
	}

	public int Pid { get; set; }

	public string Tags { get; set; }

	public long Cid { get; set; }

	public int Id { get; set; }

	public int Json { get; set; } = 1;

	public GelbooruPostsRequest() { }

}