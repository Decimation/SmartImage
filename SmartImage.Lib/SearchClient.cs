#region

global using static Kantan.Diagnostics.LogCategories;
global using MN = System.Diagnostics.CodeAnalysis.MaybeNullAttribute;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Kantan.Net;
using Kantan.Utilities;
using Novus.Utilities;
using SmartImage.Lib.Engines;
using SmartImage.Lib.Engines.Search.Base;
using SmartImage.Lib.Engines.Upload;
using SmartImage.Lib.Searching;
using SmartImage.Lib.Utilities;

#endregion

// ReSharper disable InconsistentNaming

// ReSharper disable SuggestVarOrType_Elsewhere
// ReSharper disable CognitiveComplexity
// ReSharper disable LoopCanBeConvertedToQuery
// ReSharper disable UnusedMember.Global

[assembly: InternalsVisibleTo("SmartImage")]

namespace SmartImage.Lib;

/*
 * TODO: THE DESIGN OF THIS TYPE IS POORLY DONE
 * TODO: REFACTOR
 *
 * TODO: Results should not be fields, and should be returned by their respective functions
 *
 */


/// <summary>
///     Handles searches
/// </summary>
public sealed class SearchClient : IDisposable
{
	public SearchClient(SearchConfig config)
	{
		Config = config;

		Results         = new List<SearchResult>();
		FilteredResults = new List<SearchResult>();
		DirectResults   = new List<ImageResult>();
		DetailedResults = new List<ImageResult>();
		ContinueTasks   = new List<Task>();

		ContinueTaskCompletionSource = new();

		Reload();

	}

	/// <summary>
	///     The configuration to use when searching
	/// </summary>
	public SearchConfig Config { get; init; }

	/// <summary>
	///     Whether the search process is complete
	/// </summary>
	public bool IsComplete { get; private set; }

	/// <summary>
	///     Search engines to use
	/// </summary>
	public BaseSearchEngine[] Engines { get; private set; }

	/// <summary>
	///     Contains search results
	/// </summary>
	public List<SearchResult> Results { get; }

	/// <summary>
	///     Contains the <see cref="ImageResult" /> elements of <see cref="Results" /> which contain
	///     direct image links
	/// </summary>
	public List<ImageResult> DirectResults { get; }

	/// <summary>
	///     Contains the most detailed <see cref="ImageResult" /> elements of <see cref="Results" />
	/// </summary>
	public List<ImageResult> DetailedResults { get; }

	/// <summary>
	///     Contains filtered search results
	/// </summary>
	public List<SearchResult> FilteredResults { get; }

	// public List<SearchResult> AllResults => Results.Union(FilteredResults).Distinct().ToList();

	/// <summary>
	///     Number of pending results
	/// </summary>
	public int PendingCount => Tasks.Count;

	public int CompleteCount => Results.Count;

	public bool IsContinueComplete { get; private set; }

	public List<Task<SearchResult>> Tasks { get; private set; }

	public List<Task> ContinueTasks { get; }

	public TaskCompletionSource ContinueTaskCompletionSource { get; private set; }

	public void Dispose()
	{
		Debug.WriteLine($"Disposing {nameof(SearchClient)}");

		// var rg  = AllResults;
		// var rg2 = DetailedResults.Union(DirectResults).ToList();

		var un = (Results.Union(FilteredResults).Distinct())
		         .Cast<IDisposable>()
		         .Union(DirectResults.Union(DetailedResults).Distinct())
		         .ToList();

		foreach (var result in un) {
			result.Dispose();

		}

		foreach (var v in new IList[] { Results, FilteredResults, DirectResults, DetailedResults }) {
			v.Clear();
		}

		/*Results.Clear();
		DirectResults.Clear();
		FilteredResults.Clear();
		DetailedResults.Clear();*/
		ContinueTasks.Clear();

		IsComplete         = false;
		IsContinueComplete = false;

		ContinueTaskCompletionSource = new();
	}

	/// <summary>
	///     Reloads <see cref="Config" /> and <see cref="Engines" /> accordingly.
	/// </summary>
	public void Reload(bool saveCfg = false)
	{
		if (Config.SearchEngines == SearchEngineOptions.None) {
			Config.SearchEngines = SearchEngineOptions.All;
		}

		Engines = BaseSearchEngine.GetAllSearchEngines()
		                          .Where(e => Config.SearchEngines.HasFlag(e.EngineOption))
		                          .ToArray();

		Trace.WriteLine($"{nameof(SearchClient)}: Config:\n{Config}", C_DEBUG);

		if (saveCfg) {
			Config.Save();
		}
	}

	/// <summary>
	///     Performs an image search asynchronously.
	/// </summary>
	public async Task RunSearchAsync(CancellationTokenSource cts2, CancellationToken cts)
	{
		if (IsComplete) {
			throw new SmartImageException();
		}

		Tasks = GetSearchTasks(cts);

		while (!IsComplete && !cts.IsCancellationRequested) {
			var finished = await Task.WhenAny(Tasks);

			var task = finished.ContinueWith(GetResultContinueCallback, state: cts2, cts,
			                                 0, TaskScheduler.Default);

			ContinueTasks.Add(task);

			SearchResult value = finished.Result;

			Tasks.Remove(finished);

			bool? isFiltered;
			bool  isPriority = Config.PriorityEngines.HasFlag(value.Engine.EngineOption);

			//
			//                          Filtering
			//                         /         \
			//                     true           false
			//                     /                 \
			//               IsNonPrimitive         [Results]
			//                /          \
			//              true         false
			//             /               \
			//        [Results]        [FilteredResults]
			//

			if (Config.Filtering) {

				if (value.IsNonPrimitive) {
					Results.Add(value);
					DetailedResults.Add(value.PrimaryResult);
					isFiltered = false;
				}
				else {
					FilteredResults.Add(value);
					isFiltered = true;
				}
			}
			else {
				Results.Add(value);
				isFiltered = null;
			}

			//

			// Call event
			ResultCompleted?.Invoke(null, new ResultCompletedEventArgs(value)
			{
				IsFiltered = isFiltered,
				IsPriority = isPriority
			});

			IsComplete = !Tasks.Any();

		}

		Trace.WriteLine($"{nameof(SearchClient)}: Search complete", C_SUCCESS);

		/* 2nd pass */

		// DetailedResults.AddRange(ApplyPredicateFilter(Results, v => v.IsNonPrimitive));

		var args = new SearchCompletedEventArgs { };

		SearchCompleted?.Invoke(null, args);
	}

	public List<Task<SearchResult>> GetSearchTasks(CancellationToken cts)
	{
		return new List<Task<SearchResult>>(Engines.Select(engine =>
		{
			var task = engine.GetResultAsync(Config.Query, cts);

			return task;
		}));
	}

	public async Task RunContinueAsync(CancellationToken c)
	{

		IsContinueComplete = false;

		while (!IsContinueComplete && !c.IsCancellationRequested) {
			var task = await Task.WhenAny(ContinueTasks);
			await task;

			ContinueTasks.Remove(task);
			IsContinueComplete = !ContinueTasks.Any();

		}

	}

	public void GetResultContinueCallback(Task<SearchResult> task, object state)
	{
		var value = task.Result;

		if (!value.IsStatusSuccessful || !value.IsNonPrimitive || value.Scanned) {
			return;
		}

		var cts    = (CancellationTokenSource) state;
		var result = value.GetBinaryImageResults(cts);

		if (result.Any()) {

			DirectResults.AddRange(result);
			value.Scanned = true;

			if (DirectResults.Count > 0 /*||
			    !DirectResultsWaitHandle.SafeWaitHandle.IsClosed*/ /*|| ContinueTasks.Count==1*/) {

				if (ContinueTaskCompletionSource.TrySetResult()) {
					Debug.WriteLine($"{nameof(ContinueTaskCompletionSource)} set");
				}
			}

			ContinueCompleted?.Invoke(null, EventArgs.Empty);
		}
	}

	/// <summary>
	///     Fires when <see cref="GetResultContinueCallback" /> returns
	/// </summary>
	public event EventHandler ContinueCompleted;

	/// <summary>
	///     Fires when a result is returned (<see cref="RunSearchAsync" />).
	/// </summary>
	public event EventHandler<ResultCompletedEventArgs> ResultCompleted;

	/// <summary>
	///     Fires when a search is complete (<see cref="RunSearchAsync" />).
	/// </summary>
	public event EventHandler<SearchCompletedEventArgs> SearchCompleted;
}

public sealed class SearchCompletedEventArgs : EventArgs
{
	//todo
}

public sealed class ResultCompletedEventArgs : EventArgs
{
	public ResultCompletedEventArgs(SearchResult result)
	{
		Result = result;
	}

	/// <summary>
	///     Search result
	/// </summary>
	public SearchResult Result { get; }

	/// <summary>
	///     When <see cref="SearchConfig.Filtering" /> is <c>true</c>:
	///     <c>true</c> if the result was filtered; <c>false</c> otherwise
	///     <para></para>
	///     When <see cref="SearchConfig.Filtering" /> is <c>false</c>:
	///     <c>null</c>
	/// </summary>
	public bool? IsFiltered { get; init; }

	/// <summary>
	///     Whether this result was returned by an engine that is
	///     one of the specified <see cref="SearchConfig.PriorityEngines" />
	/// </summary>
	public bool IsPriority { get; init; }

	public override string ToString()
	{
		return $"{nameof(Result)}: {Result}, {nameof(IsFiltered)}: {IsFiltered}, {nameof(IsPriority)}: {IsPriority}";
	}
}