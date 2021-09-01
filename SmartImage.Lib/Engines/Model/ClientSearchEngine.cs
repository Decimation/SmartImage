using System;
using System.Diagnostics;
using RestSharp;
using SmartImage.Lib.Searching;

namespace SmartImage.Lib.Engines.Model
{
	/// <summary>
	/// Represents a search engine whose results are returned from an API.
	/// </summary>
	public abstract class ClientSearchEngine : ProcessedSearchEngine
	{
		protected ClientSearchEngine(string baseUrl, string endpointUrl) : base(baseUrl)
		{
			Client      = new RestClient(endpointUrl);
			EndpointUrl = endpointUrl;
		}

		public abstract override SearchEngineOptions EngineOption { get; }

		public abstract override string Name { get; }

		public abstract override EngineSearchType SearchType { get; }

		protected string EndpointUrl { get; }

		protected RestClient Client { get; }
	}
}