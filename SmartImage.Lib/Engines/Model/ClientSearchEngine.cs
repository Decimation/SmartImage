using System;
using System.Diagnostics;
using RestSharp;
using SmartImage.Lib.Searching;

namespace SmartImage.Lib.Engines.Model
{
	/// <summary>
	/// Represents a search engine whose results are returned from an API.
	/// </summary>
	public abstract class ClientSearchEngine : BaseSearchEngine
	{
		protected ClientSearchEngine(string baseUrl, string endpointUrl) : base(baseUrl)
		{
			Client      = new RestClient(endpointUrl);
			EndpointUrl = endpointUrl;
		}

		public abstract override SearchEngineOptions EngineOption { get; }

		public abstract override string Name { get; }

		protected string EndpointUrl { get; }

		protected RestClient Client { get; }


		// todo: refactor this to inherit from ProcessedSearchEngine

		[DebuggerHidden]
		public override SearchResult GetResult(ImageQuery query)
		{
			return TryProcess(base.GetResult(query), sr =>
			{
				var t1 = Stopwatch.GetTimestamp();

				var process = Process(query, sr);

				var d = TimeSpan.FromTicks(Stopwatch.GetTimestamp() - t1);

				sr.ProcessingTime = d;

				return process;
			});
		}

		protected abstract SearchResult Process(ImageQuery query, SearchResult r);
	}
}