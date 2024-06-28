using System.Reflection;
using Flurl.Http;

namespace SmartImage.Lib.Clients.Booru;

// TODO
public abstract class BaseGelbooruClient : BaseBooruClient
{

	public FlurlClient Client { get; }

	[CBN]
	public string Key { get; set; }

	[CBN]
	public string Id { get; set; }

	protected BaseGelbooruClient(Url baseUrl) : base(baseUrl)
	{

		Client = new FlurlClient()
		{
			BaseUrl = baseUrl,
			Settings =
			{
				JsonSerializer = { }
			}
		};
	}

	public class PostsRequest
	{

		public int Limit { get; set; }

		public int Pid { get; set; }

		public string Tags { get; set; }

		public long Cid { get; set; }

		public int Id { get; set; }

		public int Json { get; set; } = 1;

		public PostsRequest() { }

	}

	protected int PostMax { get; set; } = 100;

	protected virtual bool Verify(PostsRequest r)
	{
		r.Limit = Math.Clamp(r.Limit, 1, PostMax);

		return true;
	}

	public virtual async Task<IFlurlResponse> GetPostsAsync(PostsRequest r)
	{
		if (!Verify(r)) {
			throw new ArgumentException();
		}

		var properties = new List<string>();

		foreach (PropertyInfo p in r.GetType().GetProperties()) {
			var o = p.GetValue(r);

			if (o == null || o is string s && string.IsNullOrWhiteSpace(s) || o.Equals(0)) {
				continue;
			}

			var sss = o.ToString();
			var h   = p.Name.ToLower() + "=" + Url.Encode(sss, true);
			properties.Add(h);
		}

		var ss = string.Join('&', properties);

		return await Client.Request("/index.php?page=post&s=list", ss)
			       .GetAsync();
	}

	public override void Dispose()
	{
		Client?.Dispose();
	}

}
