using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Novus.Win32;
using SmartImage.Lib;

namespace SmartImage.Modes;

public abstract class BaseProgramMode : IDisposable
{

	protected BaseProgramMode(SearchQuery? sq = null)
	{
		Query  = sq ?? SearchQuery.Null;
		Client = new SearchClient(new SearchConfig());

	}

	public SearchQuery Query { get; set; }

	public SearchConfig Config => Client.Config;

	public SearchClient Client { get; init; }

	//todo
	public volatile bool Status;

	public abstract Task<object> RunAsync(string[] args);

	public abstract Task PreSearchAsync(object? sender);

	public abstract Task PostSearchAsync(object? sender, List<SearchResult> results1);

	public abstract Task OnResult(object o, SearchResult r);

	public abstract Task OnComplete(object sender, List<SearchResult> e);

	public abstract Task CloseAsync();

	public abstract Task<bool> CanRun();

	protected void SetConfig(SearchEngineOptions t2, SearchEngineOptions t3, bool t4)
	{
		Config.SearchEngines   = t2;
		Config.PriorityEngines = t3;

		Config.OnTop = t4;

		if (Config.OnTop) {
			Native.KeepWindowOnTop(Cache.HndWindow);
		}
	}

	#region Implementation of IDisposable

	public abstract void Dispose();

	#endregion
}