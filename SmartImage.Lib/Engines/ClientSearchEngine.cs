using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AngleSharp.Dom;
using RestSharp;
using SmartImage.Lib.Searching;
using static SimpleCore.Diagnostics.LogCategories;

namespace SmartImage.Lib.Engines
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

		[DebuggerHidden]
		public override SearchResult GetResult(ImageQuery query)
		{
			var sr = base.GetResult(query);

			if (!sr.IsSuccessful) {
				return sr;
			}

			try {

				sr = Process(query, sr);
			}
			catch (Exception e) {
				sr.Status = ResultStatus.Failure;
				Trace.WriteLine($"{Name}: {e.Message}", C_ERROR);
			}

			return sr;
		}

		protected abstract SearchResult Process(ImageQuery query, SearchResult r);
	}
}