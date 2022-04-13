using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Kantan.Net;
using Kantan.Net.Content;
using Kantan.Net.Content.Filters;
using Kantan.Net.Media;
using Novus.OS;

#pragma warning disable IDE0079

#pragma warning disable CS0168
#pragma warning disable IDE0059

#pragma warning disable CS0618
#pragma warning disable SYSLIB0014
#pragma warning disable CA1416
// ReSharper disable ConvertIfStatementToReturnStatement
// ReSharper disable CognitiveComplexity
// ReSharper disable PossibleNullReferenceException
// ReSharper disable UnusedParameter.Local
// ReSharper disable PossibleMultipleEnumeration
// ReSharper disable AssignNullToNotNullAttribute
// ReSharper disable LoopCanBeConvertedToQuery
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global

namespace SmartImage.Lib.Utilities;

public static class ImageMedia
{
	private const int TIMEOUT = -1;

	/*
	 * Direct images are URIs that point to a binary image file
	 */

	/*
	 * https://stackoverflow.com/questions/35151067/algorithm-to-compare-two-images-in-c-sharp
	 * https://stackoverflow.com/questions/23931/algorithm-to-compare-two-images
	 * https://github.com/aetilius/pHash
	 * http://hackerfactor.com/blog/index.php%3F/archives/432-Looks-Like-It.html
	 * https://github.com/ishanjain28/perceptualhash
	 * https://github.com/Tom64b/dHash
	 * https://github.com/Rayraegah/dhash
	 * https://tineye.com/faq#how
	 * https://github.com/CrackedP0t/Tidder/
	 * https://github.com/taurenshaman/imagehash
	 */

	/*
	 * https://github.com/mikf/gallery-dl
	 * https://github.com/regosen/gallery_get
	 */

	public static async Task<HttpResource[]> ScanAsync(string url, int ms)
	{
		List<string> urls = await IHttpResourceFilter.Default.Extract(url);

		var v = await Task.WhenAll(urls.Select(async Task<HttpResource>(s1) =>
		{
			var resource = await HttpResource.GetAsync(s1);

			var tt = resource?.Resolve(true);

			return resource;
		}));

		v = v.Where(t => t != null && t.IsBinary).ToArray();

		return v;
	}

	public static HttpResource[] Scan(string url, int ms)
	{
		var t = ScanAsync(url, ms);
		t.Wait();
		;
		return t.Result;
	}

	public static MediaResourceInfo GetMediaInfo(string x, int ms = TIMEOUT)
	{
		var isFile = File.Exists(x);

		var di = HttpResource.GetAsync(x);
		di.Wait();

		var o = di.Result;
		o?.Resolve();

		var mri = new MediaResourceInfo
		{
			Resource = o,
			IsFile   = isFile,
			IsUri    = o is { IsBinary: true }
		};

		return mri;
	}

	[CanBeNull]
	public static string Download(Uri src, string path)
	{
		string    filename = UriUtilities.NormalizeFilename(src);
		string    combine  = Path.Combine(path, filename);
		using var wc       = new WebClient();

		Debug.WriteLine($"{nameof(ImageMedia)}: Downloading {src} to {combine} ...", C_DEBUG);

		try {
			wc.DownloadFile(src.ToString(), combine);
			return combine;
		}
		catch (Exception e) {
			Debug.WriteLine($"{nameof(ImageMedia)}: {e.Message}", C_ERROR);
			return null;
		}
	}
}