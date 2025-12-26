using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Caching;
using System.Text;
using System.Threading.Tasks;
using Microsoft.IO;
using SmartImage.Lib.Images.Uni;

namespace SmartImage.Lib.Images;

internal class ImageManager
{

	/*private static readonly MemoryCache _cache = new MemoryCache("imgCache");

	public static void CacheUniImage(string key, byte[] image, TimeSpan expiration)
	{
		_cache.Set(key, image, new CacheItemPolicy()
		{
			AbsoluteExpiration = DateTimeOffset.Now.AddMinutes(3)
		});
	}

	public static UniImage GetCachedUniImage(string key, byte[])
	{
		return _cache.AddOrGetExisting(key, ) ? image : null;
	}*/
	// internal static readonly ArrayPool<byte>               MemPool = ArrayPool<byte>.Shared;

	internal static readonly RecyclableMemoryStreamManager MemMgr = new(new RecyclableMemoryStreamManager.Options()
	{
		
	});

}