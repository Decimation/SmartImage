using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Kantan.Net;
using PuppeteerExtraSharp;
using PuppeteerExtraSharp.Plugins.ExtraStealth;
using PuppeteerSharp;
using SmartImage.Lib.Engines.Search.Base;
using SmartImage.Lib.Searching;
using SmartImage.Lib.Utilities;

namespace SmartImage.Lib.Engines.Search;

public sealed class TinEyeEngine : WebDriverSearchEngine
{
	public TinEyeEngine() : base("https://www.tineye.com/search?url=") { }


	public override SearchEngineOptions EngineOption => SearchEngineOptions.TinEye;

	public override EngineSearchType SearchType => EngineSearchType.Image;

	public override void Dispose() { }

	protected override async Task<List<ImageResult>> BrowseAsync(ImageQuery sd, SearchResult r)
	{
		var browser = await LaunchBrowserAsync(await GetBrowserRevisionAsync());

		await using Page page = await browser.NewPageAsync();

		await page.GoToAsync(BaseUrl + sd.UploadUri);
		await page.WaitForNavigationAsync();

		var rd = page.Url;

		var resultElems = await page.QuerySelectorAllAsync("div[class='match']");

		var img = new List<ImageResult>();

		var firstSpan = await (await resultElems[0].QuerySelectorAsync("span"))
			                .GetPropertyAsync("textContent");

		if (firstSpan.ToValueString().Contains("sponsored", StringComparison.InvariantCultureIgnoreCase)) {
			resultElems = resultElems.Skip(1).ToArray();
		}

		foreach (ElementHandle elem in resultElems) {

			var ir = new ImageResult(r) { };

			var p  = await elem.QuerySelectorAllAsync("p");
			var h4 = await elem.QuerySelectorAsync("h4");

			var name = await h4.GetPropertyAsync("textContent");

			ir.Name = name.ToValueString();

			foreach (ElementHandle t1 in p) {
				var a = await t1.QuerySelectorAllAsync("a");

				var uri = new List<Uri>();

				// a=a.Distinct().ToArray();

				foreach (ElementHandle t in a) {
					var href = await t.GetPropertyAsync("href");

					string s = href.ToValueString();

					if (!string.IsNullOrWhiteSpace(s)) {
						var item = new Uri(s);
						// item = item.Normalize();

						if (!uri.Contains(item)) {
							uri.Add(item);
						}
					}
				}

				ir.OtherUrl.AddRange(uri);


				var imgElems = await t1.QuerySelectorAllAsync("img");
				var imgList  = new List<Uri>();

				for (int k = 0; k < imgElems.Length; k++) {
					var src = await a[k].GetPropertyAsync("src");

					imgList.Add(new Uri(src.ToValueString()));
				}

				ir.OtherUrl.AddRange(imgList);
				ir.Url = ir.OtherUrl.FirstOrDefault();
			}

			img.Add(ir);
		}

		// await browser.DisposeAsync();

		// browser.Dispose();
		return img;
	}


	protected override SearchResult Process(object obj, SearchResult sr)
	{
		var query = (ImageQuery) obj;

		// var vr = base.GetResult(query);

		var task = BrowseAsync(query, sr);
		task.Wait();
		var list = task.Result;
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

public static class WebDriverExtensions
{
	public static string ToValueString(this JSHandle h)
	{
		// return h.JsonValueAsync().GetAwaiter().GetResult().ToString();
		return h.ToString().Replace("jshandle:", string.Empty, StringComparison.InvariantCultureIgnoreCase);
	}
}