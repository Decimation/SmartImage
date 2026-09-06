// Author: Deci | Project: SmartImage.Lib | Name: DynamicImageLoader.cs
// Date: 2026/09/05 @ 19:09:54

using System.Runtime.Caching;
using Microsoft.Extensions.Caching.Memory;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SmartImage.Lib.Images.Alloc;
using MemoryCache=Microsoft.Extensions.Caching.Memory.MemoryCache;

namespace SmartImage.Lib.Images;

public class DynamicImageLoader
{
	private readonly MemoryCache m_reqCache;


	public DynamicImageLoader()
	{
		m_reqCache = new MemoryCache(new MemoryCacheOptions(){});
		
	}

	public async Task<IAllocImage> LoadAsync(string src, CancellationToken ct = default)
	{
		Stream stream;

		try {
			if (m_reqCache.TryGetValue(src, out IAllocImage img)) {
				
			}
			if (AllocImageFile.IsFileType(src, out var fi)) {
				

			}
			else if (AllocImageUrl.IsUrlType(src, out var url2)) {
				
			}
			
		}
		catch (Exception e) {
			Console.WriteLine(e);
			throw;
		}

		return default;
	}
}