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
		Query   = sq ?? SearchQuery.Null;
		Client  = new SearchClient(new SearchConfig());
		IsReady = new ManualResetEvent(false);
		IsExit  = new ManualResetEvent(false);

	}

	public SearchQuery Query { get; set; }

	public SearchConfig Config => Client.Config;

	public SearchClient Client { get; init; }

	//todo
	public volatile int Status;

	public virtual async Task<object> RunAsync(string[] args, object? sender = null)
	{
		PreSearch(sender);

		Client.OnResult   += OnResult;
		Client.OnComplete += OnComplete;
		
		Status = 0;

		IsReady.WaitOne();
		
		var results = await Client.RunSearchAsync(Query, CancellationToken.None);

		Status = 1;

		PostSearch(sender, results);

		// await run;
		/*Application.MainLoop.Invoke(async () =>
		{
			await Task.Delay(100);

			if (_main.Status == 2) {
				Application.RequestStop();
			}
		});*/

		return Task.CompletedTask;
	}

	public abstract void PreSearch(object? sender);

	public abstract void PostSearch(object? sender, List<SearchResult> results1);

	public abstract void OnResult(object o, SearchResult r);

	public abstract void OnComplete(object sender, List<SearchResult> e);

	public abstract Task CloseAsync();

	protected int ResultCount { get; set; }

	public ManualResetEvent IsReady { get; protected set; }

	public ManualResetEvent IsExit { get; protected set; }

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