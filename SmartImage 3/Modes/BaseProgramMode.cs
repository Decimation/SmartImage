global using R1 = SmartImage.Lib.Resources;
global using R2 = SmartImage.Resources;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
// using OpenCvSharp;
using SmartImage.Lib;
using Terminal.Gui;

namespace SmartImage.Modes;

public abstract class BaseProgramMode : IDisposable
{
	protected BaseProgramMode(string[] args1, SearchQuery? sq = null)
	{
		Args    = args1;
		Token   = new();
		Query   = sq ?? SearchQuery.Null;
		Client  = new SearchClient(new SearchConfig());
		IsReady = new ManualResetEvent(false);

		// QueryMat = null;

		Client.OnResult   += OnResult;
		Client.OnComplete += OnComplete;
	}

	public SearchQuery Query { get; set; }

	public SearchConfig Config => Client.Config;

	public SearchClient Client { get; init; }

	protected ProgramStatus Status { get; set; }

	protected string[] Args { get; init; }

	protected int ResultCount { get; set; }

	public ManualResetEvent IsReady { get; protected set; }

	protected CancellationTokenSource Token { get; set; }

	public virtual async Task<object?> RunAsync(object? sender = null)
	{
		var now = Stopwatch.StartNew();

		PreSearch(sender);

		Status = ProgramStatus.None;

		IsReady.WaitOne();

		var results = await Client.RunSearchAsync(Query, Token.Token);

		now.Stop();

		Status = ProgramStatus.Signal;

		PostSearch(sender, results);

		return null;
	}

	public abstract void PreSearch(object? sender);

	public abstract void PostSearch(object? sender, List<SearchResult> results1);

	public abstract void OnResult(object o, SearchResult r);

	public abstract void OnComplete(object sender, List<SearchResult> e);

	protected abstract void ProcessArg(object? val, IEnumerator e);

	protected virtual void ProcessArgs()
	{
		var e = Args.GetEnumerator();

		while (e.MoveNext()) {
			var val = e.Current;
			ProcessArg(val, e);
		}
	}

	public abstract void Close();

	public virtual void Dispose()
	{
		Client.Dispose();
		Query.Dispose();
		Token.Dispose();
	}
}

public enum ProgramStatus
{
	None,
	Signal,
	Restart
}