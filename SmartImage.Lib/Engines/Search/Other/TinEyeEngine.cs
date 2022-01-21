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

namespace SmartImage.Lib.Engines.Search.Other;

public static class WebDriverExtensions
{
	public static string ToValueString(this JSHandle h)
		=> h.ToString().Replace("jshandle:", string.Empty, StringComparison.InvariantCultureIgnoreCase);
}

public sealed class TinEyeEngine : WebDriverSearchEngine
{
	public TinEyeEngine() : base("https://www.tineye.com/search?url=") { }

	protected override object GetProcessObj(SearchResult r)
	{
		return r.Origin.Query;
	}

	public override SearchEngineOptions EngineOption => SearchEngineOptions.TinEye;

	public override string Name => EngineOption.ToString();

	public override EngineSearchType SearchType => EngineSearchType.Image;


	protected override async Task<List<ImageResult>> Browse(ImageQuery sd, SearchResult r)
	{
		using var browserFetcher = new BrowserFetcher();

		var ri = await browserFetcher.DownloadAsync();
		Debug.WriteLine($"{ri}");
		/*await using var browser = await Puppeteer.LaunchAsync(
									  new LaunchOptions { Headless = true,  });

		await using var page = await browser.NewPageAsync();*/

		// Initialization plugin builder

		var extra = new PuppeteerExtra();

		// Use stealth plugin
		extra.Use(new StealthPlugin());

		// Launch the puppeteer browser with plugins
		var browser = await extra.LaunchAsync(new LaunchOptions
		{
			Headless = true
		});

		await using var page = await browser.NewPageAsync();

		var res = await page.GoToAsync(BaseUrl + sd.UploadUri);
		var rd  = page.Url;
		Debug.WriteLine($"{rd}");

		var rc     = await page.WaitForSelectorAsync("#result_count");
		var rcText = await rc.GetPropertyAsync("textContent");

		var results = await page.QuerySelectorAllAsync("div[class*='match-row']");
		var img     = new List<ImageResult>();

		for (int i = 0; i < results.Length; i++) {

			var elem = results[i];

			var q  = await elem.QuerySelectorAsync("img");
			var qs = await q.GetPropertyAsync("src");

			var a    = await elem.QuerySelectorAllAsync("a");
			var href = new List<JSHandle>();
			a = a.Skip(1).ToArray();
			var imggg = new List<JSHandle>();
			var im    = await elem.QuerySelectorAllAsync("p[class*='image-link']");

			for (int j = 0; j < im.Length; j++) {
				imggg.Add(im[j]);

			}

			for (int j = 0; j < a.Length; j++) {
				var h = await a[j].GetPropertyAsync("href");
				href.Add(h);
				Debug.WriteLine($"{h}");
			}

			var q2  = await elem.QuerySelectorAsync("h4");
			var qs2 = await q2.GetPropertyAsync("textContent");

			var item = new ImageResult(r)
			{
				Name = qs2.ToValueString(),
				Url  = new Uri(href[0].ToValueString())
			};

			Debug.WriteLine($"{item}");

			img.Add(item);
		}

		return img;
	}


	protected override SearchResult Process(object obj, SearchResult vr)
	{
		var query = obj as ImageQuery;
		;

		// var vr = base.GetResult(query);

		var t = Browse(query, vr);
		t.Wait();
		List<ImageResult> list = t.Result;
		Debug.WriteLine($"{list.Count}");
		vr.OtherResults.AddRange(list);
		vr.PrimaryResult = vr.OtherResults[0];
		return vr;
	}


	/*
	 * https://github.com/Jabeyjabes/TinEye-API/blob/master/TinEye_API
	 * https://github.com/mkroman/tineye/blob/master/library/tineye/client.rb
	 * https://stackoverflow.com/questions/704956/getting-the-redirected-url-from-the-original-url
	 * https://github.com/search?p=3&q=TinEye&type=Repositories
	 */
}