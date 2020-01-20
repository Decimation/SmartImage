using System.Collections.Generic;
using RestSharp;

namespace SmartImage
{
	public abstract class Indexer
	{
		protected string Endpoint { get; }
		protected RestClient Client { get; }

		protected Indexer(string endpoint)
		{
			Endpoint = endpoint;
			Client = new RestClient(Endpoint);
		}

		public abstract Result[] GetResults(string url,string apiKey);
	}
}