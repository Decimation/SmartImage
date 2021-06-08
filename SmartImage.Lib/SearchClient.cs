using JetBrains.Annotations;
using Novus.Utilities;
using SmartImage.Lib.Engines;
using SmartImage.Lib.Searching;
using SmartImage.Lib.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using SimpleCore.Net;
using SmartImage.Lib.Upload;
using static SimpleCore.Diagnostics.LogCategories;

// ReSharper disable UnusedMember.Global

namespace SmartImage.Lib
{
	public sealed class SearchClient
	{
		public SearchClient(SearchConfig config)
		{
			Config = config;

			Results = new List<SearchResult>();

			Engines = GetAllSearchEngines()
			          .Where(e => Config.SearchEngines.HasFlag(e.Engine))
			          .ToArray();
		}

		public SearchConfig Config { get; init; }

		public bool IsComplete { get; private set; }

		public BaseSearchEngine[] Engines { get; init; }

		public List<SearchResult> Results { get; }

		public void Reset()
		{
			Results.Clear();
			IsComplete = false;
		}

		[CanBeNull]
		public ImageResult FindBestResult() => FindBestResults(1).FirstOrDefault();

		public ImageResult[] FindBestResults(int n)
		{
			//todo: WIP
			Debug.WriteLine($"Finding best results");

			var best = Results.Where(r => r.Status != ResultStatus.Extraneous && !r.IsPrimitive)
			                  .SelectMany(r =>
			                  {
				                  var x = r.OtherResults;
				                  x.Insert(0, r.PrimaryResult);
				                  return x;
			                  });

			//==================================================================//


			best = best
			       .AsParallel()
			       .Where(r => r.Url != null && ImageHelper.IsDirect(r.Url.ToString()))
			       .OrderByDescending(r => r.Similarity)
			       .ThenByDescending(r => r.PixelResolution)
			       .ThenByDescending(r => r.DetailScore)
			       .Take(n);


			return best.ToArray();
		}

		public List<SearchResult> MaximizeResults<T>(Func<SearchResult, T> property)
		{
			// TODO: WIP

			//var t = RunSearchAsync();
			//await t;

			if (!IsComplete) {
				throw new SmartImageException();
			}

			var res = Results.OrderByDescending(property).ToList();

			res.RemoveAll(r => r.IsPrimitive);

			return res;
		}

		public async Task RefineSearchAsync()
		{
			if (!IsComplete) {
				throw new SmartImageException();
			}

			Trace.WriteLine($"Finding best result");

			var best = FindBestResult();

			if (best == null) {
				Trace.WriteLine($"Could not find best result");
				return;
			}

			var uri = best.Url;

			Trace.WriteLine($"Refining by {uri}");

			Config.Query = uri;

			await RunSearchAsync();
		}

		public async Task RunSearchAsync()
		{
			if (IsComplete) {
				Reset();
			}

			var tasks = new List<Task<SearchResult>>(Engines.Select(e => e.GetResultAsync(Config.Query)));

			while (!IsComplete) {
				var finished = await Task.WhenAny(tasks);

				var value = await finished;

				tasks.Remove(finished);

				Results.Add(value);

				// Call event
				ResultCompleted?.Invoke(null, new SearchResultEventArgs(value));

				IsComplete = !tasks.Any();
			}

			Trace.WriteLine($"{nameof(SearchClient)}: Search complete", C_SUCCESS);
			SearchCompleted?.Invoke(null, EventArgs.Empty);

			return;
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

		public event EventHandler<SearchResultEventArgs> ResultCompleted;

		public event EventHandler SearchCompleted;
	}

	public sealed class SearchResultEventArgs : EventArgs
	{
		public SearchResult Result { get; }

		public SearchResultEventArgs(SearchResult result)
		{
			Result = result;
		}
	}
}