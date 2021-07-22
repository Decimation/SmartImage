using System;
using System.Diagnostics;
using RestSharp;
using SmartImage.Lib.Searching;

namespace SmartImage.Lib.Engines.Model
{
	public abstract class ProcessedSearchEngine : BaseSearchEngine
	{
		protected ProcessedSearchEngine(string baseUrl) : base(baseUrl) { }

		public abstract override SearchEngineOptions EngineOption { get; }

		public abstract override string Name { get; }

		protected abstract SearchResult Process(object content, SearchResult sr);

		protected abstract object GetContent(IRestResponse response);

		[DebuggerHidden]
		public override SearchResult GetResult(ImageQuery query)
		{
			return TryProcess(GetPreliminaryResult(query, out var response), sr =>
			{
				var t1  = Stopwatch.GetTimestamp();
				var doc = GetContent(response);
				sr.RetrievalTime = TimeSpan.FromTicks(Stopwatch.GetTimestamp() - t1);


				var t2 = Stopwatch.GetTimestamp();
				sr                = Process(doc, sr);
				sr.ProcessingTime = TimeSpan.FromTicks(Stopwatch.GetTimestamp() - t2);

				return sr;
			});
		}
	}
}