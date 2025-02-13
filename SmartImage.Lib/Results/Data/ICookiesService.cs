// Author: Deci | Project: SmartImage.Lib | Name: ICookieProvider.cs
// Date: 2024/10/15 @ 12:10:00

// global using MemoryCache = Microsoft.Extensions.Caching.Memory.MemoryCache;

using System.Net;
using Flurl.Http;
using Kantan.Net.Web;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using SmartImage.Lib.Utilities.Integration;

namespace SmartImage.Lib.Results.Data;

public interface ICookiesService : IMemoryCache
{

	public ValueTask<bool> GetOrLoadCookiesAsync(CancellationToken ct = default);

}

