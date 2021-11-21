﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Novus.Utilities;
using SmartImage.Lib.Engines;
using SmartImage.Lib.Engines.Model;
using SmartImage.Lib.Searching;
using SmartImage.Lib.Upload;
using SmartImage.Lib.Utilities;
using static Kantan.Diagnostics.LogCategories;

// ReSharper disable CognitiveComplexity

// ReSharper disable LoopCanBeConvertedToQuery

// ReSharper disable UnusedMember.Global

namespace SmartImage.Lib
{
	/// <summary>
	///     Handles searches
	/// </summary>
	public sealed class SearchClient
	{
		public SearchClient(SearchConfig config)
		{
			Config = config;

			Results         = new List<SearchResult>();
			FilteredResults = new List<SearchResult>();
			DirectResults   = new List<ImageResult>();
			DetailedResults = new List<ImageResult>();

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

		public List<ImageResult> DirectResults { get; }

		public List<ImageResult> DetailedResults { get; }


		/// <summary>
		///     Contains filtered search results
		/// </summary>
		public List<SearchResult> FilteredResults { get; }



		public int Pending { get; private set; }

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
			IsComplete = false;
			Reload();
		}

		/*
		 * TODO
		 *
		 * Queue a thread to run in the background upon each result completion
		 * in which the thread scans for direct images, instead of doing the scanning post hoc
		 */

		#region

		/// <summary>
		///     Performs an image search asynchronously.
		/// </summary>
		public async Task RunSearchAsync()
		{
			if (IsComplete) {
				Reset();
			}

			var tasks = new List<Task<SearchResult>>(Engines.Select(e =>
			{
				var task = e.GetResultAsync(Config.Query);

				return task;
			}));

			Pending = tasks.Count;

			while (!IsComplete) {
				var finished = await Task.WhenAny(tasks);

				var value = await finished;

				tasks.Remove(finished);
				Pending = tasks.Count;

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

				ThreadPool.QueueUserWorkItem((c) => FindDirectResults(c, value));
				
				// Call event
				ResultCompleted?.Invoke(null, new ResultCompletedEventArgs(value)
				{
					IsFiltered = isFiltered,
					IsPriority = isPriority
				});

				IsComplete = !tasks.Any();
			}

			Trace.WriteLine($"{nameof(SearchClient)}: Search complete", C_SUCCESS);

			var args = new SearchCompletedEventArgs
			{
				Results  = Results,
				Detailed = PredicateFilter(Results, DetailPredicate),
				Direct   = DirectResults,
				Filtered = FilteredResults
			};

			SearchCompleted?.Invoke(null, args);
		}

		public Task<List<ImageResult>> WaitForDirect()
		{
			return Task.Run(() =>
			{
				while (Results.Any() && !DirectResults.Any()) {
					
				}

				return DirectResults;
			});
		}

		private void FindDirectResults(object state, SearchResult value, int count = 5, int i = 10)
		{
			// var u  = value.OtherResults.Union(new[] { value.PrimaryResult }).ToList();
			// var u2 = RefineFilter(u, DetailPredicate).ToList();

			var u2  = value.OtherResults.Union(new[] { value.PrimaryResult }).ToList();


			Debug.WriteLine($"*{nameof(SearchClient)}: Found {u2.Count} detailed results", C_DEBUG);

			var query = u2.Where(x => x.CheckDirect(DirectImageCriterion.Regex))
			              .Take(i)
			              .AsParallel();

			List<ImageResult> images = query.Where(x => x.CheckDirect(DirectImageCriterion.Binary))
			                                .Take(count)
			                                .ToList();

			if (images.Any()) {
				Debug.WriteLine($"*{nameof(SearchClient)}: Found {images.Count} direct results", C_DEBUG);
				DirectResults.AddRange(images);

				DirectFound?.Invoke(null, new DirectResultsFoundEventArgs()
				{
					DirectResultsSubset = images,
				});
			}
		}


		public List<ImageResult> PredicateFilter(List<SearchResult> results, Predicate<SearchResult> predicate)
		{
			var vb = results.Where((r) => predicate(r)).SelectMany(r =>
			{
				return r.OtherResults.Union(new[] { r.PrimaryResult });
			});

			var query = vb.OrderByDescending(r => r.Similarity)
			              .ThenByDescending(r => r.PixelResolution)
			              .ThenByDescending(r => r.DetailScore).ToList();

			return query;
		}

		/// <summary>
		///     Refines search results by searching with the most-detailed result (<see cref="GetDirectImageResult" />).
		/// </summary>
		public async Task RefineSearchAsync()
		{
			if (!IsComplete) {
				throw SearchException;
			}

			Debug.WriteLine($"{nameof(SearchClient)}: Finding direct result", C_DEBUG);

			var directResult = DirectResults.FirstOrDefault();

			if (directResult == null) {
				throw new SmartImageException("Could not find direct result");
			}

			var uri = directResult.Direct;

			Debug.WriteLine($"{nameof(SearchClient)}: Refining by {uri}", C_DEBUG);

			Config.Query = uri;

			Reset();

			await RunSearchAsync();
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

		#endregion

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
		///     An event that fires whenever a result is returned (<see cref="RunSearchAsync" />).
		/// </summary>
		public event EventHandler<ResultCompletedEventArgs> ResultCompleted;

		/// <summary>
		///     An event that fires when a search is complete (<see cref="RunSearchAsync" />).
		/// </summary>
		public event EventHandler<SearchCompletedEventArgs> SearchCompleted;

		public event EventHandler<DirectResultsFoundEventArgs> DirectFound;

		private static readonly Predicate<SearchResult> DetailPredicate = r => r.IsNonPrimitive;

		private static readonly SmartImageException SearchException = new("Search must be completed");
	}


	public sealed class DirectResultsFoundEventArgs : EventArgs
	{
		/// <remarks>
		/// This field will always be a subset of <see cref="SearchClient.DirectResults"/>
		/// <para/>
		/// <see cref="DirectResultsSubset"/> &#x2282; <see cref="SearchClient.DirectResults"/>
		/// </remarks>
		public List<ImageResult> DirectResultsSubset { get; init; }
	}

	public sealed class SearchCompletedEventArgs : EventArgs
	{
		public List<SearchResult> Results { get; init; }
		
		public List<ImageResult> Direct { get; internal set; }
		
		public List<ImageResult> Detailed { get; internal set; }
		
		public List<SearchResult> Filtered { get; internal set; }



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
}