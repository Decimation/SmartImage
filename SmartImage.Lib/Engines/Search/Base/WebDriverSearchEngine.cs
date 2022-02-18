using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PuppeteerExtraSharp;
using PuppeteerExtraSharp.Plugins.ExtraStealth;
using PuppeteerSharp;
using SmartImage.Lib.Searching;

namespace SmartImage.Lib.Engines.Search.Base;

/// <summary>
/// Represents a search engine whose results are parsed through a web driver.
/// </summary>
public abstract class WebDriverSearchEngine : ProcessedSearchEngine
{
	protected WebDriverSearchEngine(string baseUrl) : base(baseUrl) { }

	public abstract override SearchEngineOptions EngineOption { get; }

	public abstract override EngineSearchType SearchType { get; }

	protected abstract Task<List<ImageResult>> BrowseAsync(ImageQuery sd, SearchResult r);
	
	protected static async Task<PuppeteerExtra> GetBrowserAsync()
	{
		using var browserFetcher = new BrowserFetcher();

		var ri = await browserFetcher.DownloadAsync();

		Debug.WriteLine($"{ri}");

		var extra = new PuppeteerExtra();
		extra.Use(new StealthPlugin());
		return extra;
	}
}