using System;
using System.Diagnostics;
using System.Net;
using System.Threading;
using SmartImage.Lib.Searching;
using static Kantan.Diagnostics.LogCategories;

namespace SmartImage.Lib.Engines.Search.Base;

public abstract class ProcessedSearchEngine : BaseSearchEngine
{
	protected ProcessedSearchEngine(string baseUrl) : base(baseUrl) { }

	public abstract override SearchEngineOptions EngineOption { get; }

	public abstract override string Name { get; }

	public abstract override EngineSearchType SearchType { get; }

	/// <summary>
	/// Processes engine results
	/// </summary>
	/// <param name="obj">Content upon which to operate, returned by <see cref="ParseContent"/></param>
	/// <param name="sr"><see cref="SearchResult"/> to build</param>
	/// <returns>Final <see cref="SearchResult"/></returns>
	protected abstract SearchResult Process(object obj, SearchResult sr);

	protected abstract object ParseContent(SearchResultOrigin s);

	[DebuggerHidden]
	public sealed override SearchResult GetResult(ImageQuery query, CancellationToken? c = null)
	{
		var sr = base.GetResult(query);

		if (sr.Origin.Response.StatusCode == HttpStatusCode.TooManyRequests) {
			sr.Status = ResultStatus.Cooldown;
			goto ret;
		}

		if (!sr.IsSuccessful) {
			// sr.Origin.Dispose();
			goto ret;
		}


		try {

			object obj = ParseContent(sr.Origin);

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
}