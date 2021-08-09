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


		[DebuggerHidden]
		public override SearchResult GetResult(ImageQuery query)
		{
			return TryProcess(GetResult(query, out var response), sr =>
			{
				sr = Process(response, sr);

				return sr;
			});
		}
	}
}