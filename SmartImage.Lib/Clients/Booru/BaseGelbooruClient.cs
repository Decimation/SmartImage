using System.Reflection;
using Flurl.Http;

namespace SmartImage.Lib.Clients.Booru;

// TODO
public abstract class BaseGelbooruClient : IDisposable
{

	public FlurlClient Client { get; }

	public Url Base { get; }

	[CBN]
	public string Key { get; set; }

	[CBN]
	public string Id { get; set; }

	protected BaseGelbooruClient(Url @base)
	{
		Base = @base;

		Client = new FlurlClient()
		{
			BaseUrl = @base,
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

	public void Dispose()
	{
		Client?.Dispose();
	}

}

public class Rule34Booru : BaseGelbooruClient
{

	public Rule34Booru() : base("https://rule34.xxx")
	{
		PostMax = 1000;
	}

}