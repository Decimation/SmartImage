using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Kantan.Net;
using Kantan.Threading;
using Novus.Utilities;
using SmartImage.Lib.Engines;
using SmartImage.Lib.Engines.Search.Base;
using SmartImage.Lib.Engines.Upload;
using SmartImage.Lib.Engines.Upload.Base;
using SmartImage.Lib.Searching;
using SmartImage.Lib.Utilities;
using static Kantan.Diagnostics.LogCategories;

// ReSharper disable InconsistentNaming

// ReSharper disable SuggestVarOrType_Elsewhere
// ReSharper disable CognitiveComplexity
// ReSharper disable LoopCanBeConvertedToQuery
// ReSharper disable UnusedMember.Global

[assembly: InternalsVisibleTo("SmartImage")]

namespace SmartImage.Lib;

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

		m_w = new AutoResetEvent(false);

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
	/// Contains the <see cref="ImageResult"/> elements of <see cref="Results"/> which contain
	/// direct image links
	/// </summary>
	public List<ImageResult> DirectResults { get; }

	/// <summary>
	/// Contains the most detailed <see cref="ImageResult"/> elements of <see cref="Results"/>
	/// </summary>
	public List<ImageResult> DetailedResults { get; }


	/// <summary>
	///     Contains filtered search results
	/// </summary>
	public List<SearchResult> FilteredResults { get; }

	public List<SearchResult> AllResults => Results.Union(FilteredResults).Distinct().ToList();

	/// <summary>
	/// Number of pending results
	/// </summary>
	public int PendingCount { get; private set; }

	public int CompleteCount => AllResults.Count;

	public bool IsContinueComplete { get; private set; }

	public  List<Task<SearchResult>> Tasks         { get; private set; }
	private List<Task>               ContinueTasks { get; }


	/// <summary>
	///     Reloads <see cref="Config" /> and <see cref="Engines" /> accordingly.
	/// </summary>
	public void Reload()
	{
		if (Config.SearchEngines == SearchEngineOptions.None) {
			Config.SearchEngines = SearchEngineOptions.All;
		}

		Engines = GetAllSearchEngines()
		          .Where(e => Config.SearchEngines.HasFlag(e.EngineOption))
		          .ToArray();

		Trace.WriteLine($"{nameof(SearchClient)}: Config:\n{Config}", C_DEBUG);

	}

	/// <summary>
	///     Resets this instance in order to perform a new search.
	/// </summary>
	public void Reset()
	{
		Results.Clear();
		DirectResults.Clear();
		FilteredResults.Clear();
		DetailedResults.Clear();
		ContinueTasks.Clear();
		PendingCount       = 0;
		IsComplete         = false;
		IsContinueComplete = false;
		m_w                = new AutoResetEvent(false);

		Reload();
	}

	/// <summary>
	///     Performs an image search asynchronously.
	/// </summary>
	public async Task RunSearchAsync(CancellationToken? cts = null)
	{
		if (IsComplete) {
			Reset();
		}


		cts ??= CancellationToken.None;

		Tasks = new List<Task<SearchResult>>(Engines.Select(engine =>
		{
			var task = engine.GetResultAsync(Config.Query, cts.Value);

			return task;
		}));

		PendingCount = Tasks.Count;


		while (!IsComplete && !cts.Value.IsCancellationRequested) {
			var finished = await Task.WhenAny(Tasks);

			var task = finished.ContinueWith(GetResultContinueCallback, null, cts.Value,
			                                 0, TaskScheduler.Default);
			ContinueTasks.Add(task);

			SearchResult value = await finished;

			Tasks.Remove(finished);
			PendingCount = Tasks.Count;

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


			if (DetailPredicate(value)) {
				DetailedResults.Add(value.PrimaryResult);
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

		DetailedResults.AddRange(ApplyPredicateFilter(Results, DetailPredicate));

		var args = new SearchCompletedEventArgs { };

		SearchCompleted?.Invoke(null, args);
	}

	public async Task RunContinueAsync(CancellationToken? c = null)
	{

		IsContinueComplete = false;

		c ??= CancellationToken.None;

		while (!IsContinueComplete && !c.Value.IsCancellationRequested) {
			var task = await Task.WhenAny(ContinueTasks);
			await task;

			ContinueTasks.Remove(task);
			IsContinueComplete = !ContinueTasks.Any();
		}
	}


	private void GetResultContinueCallback(Task<SearchResult> task, object state)
	{
		var value = task.Result;

		if (value.IsSuccessful && value.IsNonPrimitive) {


			if (!value.Scanned) {
				// var task2 = value.FindDirectResultsAsync();
				// task2.Wait();
				// var result = task2.Result;

				var result = value.FindDirectResultsAsync();

				if (result.Any()) {
					result = result /*.Where(x => x.Direct != null)*/
						.ToList();

					DirectResults.AddRange(result);

					var autoResetEvent = ((AutoResetEvent) m_w);

					if (DirectResults.Count > 0 && !autoResetEvent.SafeWaitHandle.IsClosed) {
						Debug.WriteLine("wait handle set");
						autoResetEvent.Set();

					}

					value.Scanned = true;

					ResultUpdated?.Invoke(null, EventArgs.Empty);

					// if (result.Any()) { }
				}
			}
		}
	}

	/// <summary>
	///     Refines search results by searching with the most-detailed result (<see cref="FindDirectResults" />).
	/// </summary>
	public async Task RefineSearchAsync()
	{
		if (!IsComplete) {
			throw SearchException;
		}

		var directResult = DirectResults.FirstOrDefault();

		if (directResult == null) {
			throw new SmartImageException("Could not find direct result");
		}

		var direct = directResult.Direct.Url;

		Debug.WriteLine($"{nameof(SearchClient)}: Refining by {direct}", C_DEBUG);

		Config.Query = direct;

		Reset();

		await RunSearchAsync();
	}

	public static List<ImageResult> ApplyPredicateFilter(List<SearchResult> results, Predicate<SearchResult> predicate)
	{
		var query = results.Where(r => predicate(r))
		                   .SelectMany(r => r.AllResults)
		                   .OrderByDescending(r => r.Similarity)
		                   .ThenByDescending(r => r.PixelResolution)
		                   .ThenByDescending(r => r.DetailScore).ToList();

		return query;
	}

	/// <summary>
	///     Maximizes search results by using the specified property selector.
	/// </summary>
	/// <returns><see cref="Results" /> ordered by <paramref name="property" /></returns>
	public List<SearchResult> MaximizeResults<T>(Func<SearchResult, T> property)
	{
		if (!IsComplete) {
			throw SearchException;
		}

		var res = Results.OrderByDescending(property).ToList();

		res.RemoveAll(r => !DetailPredicate(r));

		return res;
	}

	public static BaseUploadEngine[] GetAllUploadEngines()
	{
		return typeof(BaseUploadEngine).GetAllSubclasses()
		                               .Select(Activator.CreateInstance)
		                               .Cast<BaseUploadEngine>()
		                               .ToArray();
	}

	public static BaseSearchEngine[] GetAllSearchEngines()
	{
		return typeof(BaseSearchEngine).GetAllSubclasses()
		                               .Select(Activator.CreateInstance)
		                               .Cast<BaseSearchEngine>()
		                               .ToArray();
	}

	/// <summary>
	/// Fires when a result has been updated with new information
	/// </summary>
	public event EventHandler ResultUpdated;

	/// <summary>
	///     Fires when a result is returned (<see cref="RunSearchAsync" />).
	/// </summary>
	public event EventHandler<ResultCompletedEventArgs> ResultCompleted;

	/// <summary>
	///     Fires when a search is complete (<see cref="RunSearchAsync" />).
	/// </summary>
	public event EventHandler<SearchCompletedEventArgs> SearchCompleted;


	private static readonly Predicate<SearchResult> DetailPredicate = r => r.IsNonPrimitive;

	private static readonly SmartImageException SearchException = new("Search must be completed");

	public WaitHandle m_w;


	public void Dispose()
	{
		foreach (ImageResult result in DirectResults) {
			result.Dispose();
		}

		foreach (SearchResult result in AllResults) {
			result.Dispose();
		}

	}
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
}