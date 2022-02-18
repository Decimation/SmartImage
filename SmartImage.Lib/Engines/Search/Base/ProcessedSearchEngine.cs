using System;
using System.Diagnostics;
using System.Net;
using System.Threading;
using SmartImage.Lib.Searching;
using static Kantan.Diagnostics.LogCategories;

namespace SmartImage.Lib.Engines.Search.Base;

/// <summary>
/// Represents a search engine whose results require further processing.
/// </summary>
public abstract class ProcessedSearchEngine : BaseSearchEngine
{
	protected ProcessedSearchEngine(string baseUrl) : base(baseUrl) { }

	public abstract override SearchEngineOptions EngineOption { get; }

	public abstract override EngineSearchType SearchType { get; }


	public sealed override SearchResult GetResult(ImageQuery query, CancellationToken? c = null)
	{
		var sr = base.GetResult(query, c);

		if (sr.Origin.Response?.StatusCode == HttpStatusCode.TooManyRequests) {
			sr.Status = ResultStatus.Cooldown;
			goto ret;
		}

		if (!sr.IsStatusSuccessful) {
			// sr.Origin.Dispose();
			goto ret;
		}

		try {

			// object obj = ParseContent(sr.Origin);
			var obj = GetProcessingObject(sr);
			sr = Process(obj, sr);

			if (obj is IDisposable d) {
				d.Dispose();
			}
			// sr.Origin.Dispose();

			return sr;
		}
		catch (Exception e) {

			sr.Status       = ResultStatus.Failure;
			sr.ErrorMessage = e.Message;

			Trace.WriteLine($"{sr.Engine.Name}: {e.Message}", C_ERROR);
		}

		ret:
		return sr;
	}

	protected virtual object GetProcessingObject(SearchResult sr) => sr.Origin.Query;

	/// <summary>
	/// Processes engine results
	/// </summary>
	protected abstract SearchResult Process(object obj, SearchResult sr);
}