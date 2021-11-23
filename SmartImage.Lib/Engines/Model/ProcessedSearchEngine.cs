using System;
using System.Diagnostics;
using System.Net.Http;
using AngleSharp.Dom;
using RestSharp;
using SmartImage.Lib.Searching;
using static Kantan.Diagnostics.LogCategories;

namespace SmartImage.Lib.Engines.Model;

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
	public sealed override SearchResult GetResult(ImageQuery query)
	{
		var sr = base.GetResult(query);



		if (!sr.IsSuccessful) {
			return sr;
		}

		try {

			object obj = ParseContent(sr.Origin);

			sr = Process(obj, sr);

			if (obj is IDisposable d) {
				d.Dispose();
			}
				
			// Debug.WriteLine($"[{sr.RetrievalTime}] [{sr.Origin.Retrieval}] | {Name}");
			// sr.RetrievalTime += sr.Origin.Retrieval;

			return sr;
		}
		catch (Exception e) {

			sr.Status       = ResultStatus.Failure;
			sr.ErrorMessage = e.Message;

			Trace.WriteLine($"{sr.Engine.Name}: {e.Message}", C_ERROR);
		}

		return sr;
	}
}