using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using PuppeteerExtraSharp;
using PuppeteerExtraSharp.Plugins.ExtraStealth;
using PuppeteerSharp;
using SmartImage.Lib.Engines.Search.Base;
using SmartImage.Lib.Searching;

namespace SmartImage.Lib.Engines.Search;

public static class WebDriverExtensions
{
	public static string ToValueString(this JSHandle h)
		=> h.ToString().Replace("jshandle:", string.Empty, StringComparison.InvariantCultureIgnoreCase);
}

public sealed class TinEyeEngine : WebDriverSearchEngine
{
	public TinEyeEngine() : base("https://www.tineye.com/search?url=") { }

	protected override object GetProcessingObject(SearchResult r)
	{
		return r.Origin.Query;
	}

	public override SearchEngineOptions EngineOption => SearchEngineOptions.TinEye;

	public override EngineSearchType SearchType => EngineSearchType.Image;

	protected override async Task<List<ImageResult>> Browse(ImageQuery sd, SearchResult r)
	{
		using var browserFetcher = new BrowserFetcher();

		var ri = await browserFetcher.DownloadAsync();

		Debug.WriteLine($"{ri}");

		var extra = new PuppeteerExtra();
		extra.Use(new StealthPlugin());

		await using Browser browser = await extra.LaunchAsync(new LaunchOptions
		{
			Headless = true
		});


		await using Page page = await browser.NewPageAsync();

		await page.GoToAsync(BaseUrl + sd.UploadUri);
		// await page.ScreenshotAsync(@"C:\Users\Deci\Downloads\a.png");
		await page.WaitForNavigationAsync();

		var rd = page.Url;
		Debug.WriteLine($"{rd}");

		/*var rcText = await (await page.WaitForSelectorAsync("#result_count"))
			             .GetPropertyAsync("textContent");*/

		//div[class="match"]
		//div[class*='match-row']

		var resultElems = await page.QuerySelectorAllAsync("div[class='match']");

		var img = new List<ImageResult>();

		if ((await (await resultElems[0].QuerySelectorAsync("span")).GetPropertyAsync("textContent"))
		    .ToValueString().Contains("sponsored", StringComparison.InvariantCultureIgnoreCase)) {
			resultElems = resultElems.Skip(1).ToArray();
		}

		foreach (ElementHandle elem in resultElems) {

			var ir = new ImageResult(r) { };

			var p    = await elem.QuerySelectorAllAsync("p");
			var h4   = await elem.QuerySelectorAsync("h4");

			var name = await h4.GetPropertyAsync("textContent");

			ir.Name = name.ToValueString();

			foreach (ElementHandle t1 in p) {
				var a = await t1.QuerySelectorAllAsync("a");

				var uri = new List<Uri>();

				foreach (ElementHandle t in a) {
					var href = await t.GetPropertyAsync("href");

					string s = href.ToValueString();
					Debug.WriteLine($"{s} | {await href.JsonValueAsync()}");
					if (!string.IsNullOrWhiteSpace(s)) {
						uri.Add(new Uri(s));
					}
				}

				ir.OtherUrl.AddRange(uri);
				

				var imgElems  = await t1.QuerySelectorAllAsync("img");
				var imgList = new List<Uri>();

				for (int k = 0; k < imgElems.Length; k++) {
					var src = await a[k].GetPropertyAsync("src");
					
					imgList.Add(new Uri(src.ToValueString()));
				}
				
				ir.OtherUrl.AddRange(imgList);
				ir.Url = ir.OtherUrl.FirstOrDefault();

				/*var union = imgList.Union(uri).ToArray();

				var plr=Parallel.For(0, union.Length, (i, s) =>
				{
					if (ImageHelper.IsBinaryImage(union[i].ToString(), out var b,2000)) {
						ir.DirectImages.Add(b);
					}

				});
				r.Scanned = true;*/
			}

			img.Add(ir);
		}
		
		return img;
	}


	protected override SearchResult Process(object obj, SearchResult sr)
	{
		var query = (ImageQuery) obj;
		
		// var vr = base.GetResult(query);

		var task = Browse(query, sr);
		task.Wait();
		List<ImageResult> list = task.Result;
		Debug.WriteLine($"{list.Count}");
		sr.OtherResults.AddRange(list);
		sr.PrimaryResult = list.First();
		return sr;
	}


	/*
	 * https://github.com/Jabeyjabes/TinEye-API/blob/master/TinEye_API
	 * https://github.com/mkroman/tineye/blob/master/library/tineye/client.rb
	 * https://stackoverflow.com/questions/704956/getting-the-redirected-url-from-the-original-url
	 * https://github.com/search?p=3&q=TinEye&type=Repositories
	 */
}