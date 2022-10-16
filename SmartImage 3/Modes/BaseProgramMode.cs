using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Novus.Win32;
using SmartImage.Lib;
using Terminal.Gui;

namespace SmartImage.Modes;

public abstract class BaseProgramMode : IDisposable
{
	protected BaseProgramMode(string[] args1, SearchQuery? sq = null)
	{
		Query   = sq ?? SearchQuery.Null;
		Client  = new SearchClient(new SearchConfig());
		IsReady = new ManualResetEvent(false);
		IsExit  = new ManualResetEvent(false);
		Args    = args1;
	}

	public SearchQuery Query { get; set; }

	public SearchConfig Config => Client.Config;

	public SearchClient Client { get; init; }

	protected ProgramStatus Status { get; set; }

	protected string[] Args { get; set; }

	public virtual async Task<object?> RunAsync(object? sender = null)
	{
		var now = Stopwatch.StartNew();

		PreSearch(sender);

		Client.OnResult   += OnResult;
		Client.OnComplete += OnComplete;

		Status = ProgramStatus.None;

		IsReady.WaitOne();

		var results = await Client.RunSearchAsync(Query, CancellationToken.None);

		now.Stop();

		Status = ProgramStatus.Signal;

		PostSearch(sender, results);

		return Task.CompletedTask;
	}

	public abstract void PreSearch(object? sender);

	public abstract void PostSearch(object? sender, List<SearchResult> results1);

	public abstract void OnResult(object o, SearchResult r);

	public abstract void OnComplete(object sender, List<SearchResult> e);

	public abstract void Close();

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

public enum ProgramStatus
{
	None,
	Signal,
	Restart
}