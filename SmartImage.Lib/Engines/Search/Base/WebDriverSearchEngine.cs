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

	public static string BrowserPath { get; } = Path.Combine(SearchConfig.AppFolder, ".local-chromium");

	protected WebDriverSearchEngine(string baseUrl) : base(baseUrl) { }

	public abstract override SearchEngineOptions EngineOption { get; }

	public abstract override EngineSearchType SearchType { get; }

	protected abstract Task<List<ImageResult>> BrowseAsync(ImageQuery sd, SearchResult r);

	protected static async Task<RevisionInfo> GetBrowserRevisionAsync()
	{
		if (Directory.Exists(BrowserPath)) {
			return new RevisionInfo()
			{
				FolderPath = BrowserPath
			};
		}

		using var browserFetcher = new BrowserFetcher();

		var ri = await browserFetcher.DownloadAsync();

		// Debug.WriteLine($"{ri.ExecutablePath}, {ri.FolderPath}");
		Directory.Move(ri.FolderPath, BrowserPath);


		return ri;
	}

	protected static async Task<Browser> LaunchBrowserAsync(RevisionInfo ri)
	{
		var extra = new PuppeteerExtra() { };
		extra.Use(new StealthPlugin());

		string exe = GetExecutableForRevision(ri);

		await using Browser browser = await extra.LaunchAsync(new LaunchOptions
		{
			// Headless       = true,
			ExecutablePath = exe,
		});

		return browser;
	}

	private static string GetExecutableForRevision(RevisionInfo ri)
	{
		var df = Directory.EnumerateFiles(ri.FolderPath, enumerationOptions: new EnumerationOptions()
		{
			MaxRecursionDepth     = 2,
			RecurseSubdirectories = true
		}, searchPattern: CHROME_EXE_PATTERN);

		var exe = df.First();
		return exe;
	}
}