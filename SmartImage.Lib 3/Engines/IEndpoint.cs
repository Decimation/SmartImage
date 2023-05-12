// Read S SmartImage.Lib IEndpoint.cs
// 2023-05-12 @ 1:01 AM

using System.Collections.Concurrent;
using Flurl.Http;

namespace SmartImage.Lib.Engines;

public interface IEndpoint
{
	public Url EndpointUrl { get; }

	[ICBN]
	public static async ValueTask<T[]> QueryAlive<T>(T[] rg, CancellationToken ct = default) where T : IEndpoint
	{
		var cb = new ConcurrentBag<T>();

		await Parallel.ForEachAsync(rg, ct, async (b, c) =>
		{
			var u = ((Url) b.EndpointUrl).Root;
			var r = await u.HeadAsync(ct);

			if (r.ResponseMessage.IsSuccessStatusCode) {
				cb.Add(b);
			}
		});

		return cb.ToArray();
	}
}