using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PuppeteerExtraSharp;
using PuppeteerExtraSharp.Plugins.ExtraStealth;
using PuppeteerSharp;
using SmartImage.Lib.Properties;
using SmartImage.Lib.Searching;

namespace SmartImage.Lib.Engines.Search.Base;

/// <summary>
/// Represents a search engine whose results are parsed through a web driver.
/// </summary>
public abstract class WebDriverSearchEngine : ProcessedSearchEngine
{
	/*
	 * TODO: THIS IS A STOPGAP
	 */

	protected const string CHROME_EXE_PATTERN = "*chrome.exe*";
	private const   string LOCAL_CHROMIUM     = ".local-chromium";

	public static string BrowserPath { get; } = Path.Combine(SearchConfig.AppFolder, LOCAL_CHROMIUM);

	protected WebDriverSearchEngine(string baseUrl) : base(baseUrl) { }

	public abstract override SearchEngineOptions EngineOption { get; }

	public abstract override EngineSearchType SearchType { get; }

	protected abstract Task<List<ImageResult>> BrowseAsync(ImageQuery sd, SearchResult r);

	protected static async Task<RevisionInfo> GetBrowserRevisionAsync()
	{

		RevisionInfo ri = default;

		if (!Directory.Exists(LOCAL_CHROMIUM)) {
			using var browserFetcher = new BrowserFetcher();
			ri                = await browserFetcher.DownloadAsync();

			// Debug.WriteLine($"{ri.ExecutablePath}, {ri.FolderPath}");
			// Directory.Move(ri.FolderPath, BrowserPath);
		}
		else {
			// ri.FolderPath     = BrowserPath;
			// ri.ExecutablePath = GetExecutableForRevision(BrowserPath);

		}


		return ri;
	}

	protected static async Task<Browser> LaunchBrowserAsync(RevisionInfo ri)
	{
		var extra = new PuppeteerExtra() { };
		extra.Use(new StealthPlugin());
		

		await using Browser browser = await extra.LaunchAsync(new LaunchOptions
		{
			Headless       = true,

			// ExecutablePath = ri?.ExecutablePath ?? LOCAL_CHROMIUM,
			
		});
		
		return browser;
	}

	private static string GetExecutableForRevision(string f)
	{
		var df = Directory.EnumerateFiles(f, enumerationOptions: new EnumerationOptions()
		{
			MaxRecursionDepth     = 2,
			RecurseSubdirectories = true
		}, searchPattern: CHROME_EXE_PATTERN);

		var exe = df.First();
		return exe;
	}
}