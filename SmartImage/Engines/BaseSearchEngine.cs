using SmartImage.Searching;
using System.Drawing;

namespace SmartImage.Engines
{
	/// <summary>
	/// Represents a search engine
	/// </summary>
	public abstract class BaseSearchEngine
	{
		public string BaseUrl { get; }

		protected BaseSearchEngine(string baseUrl)
		{
			BaseUrl = baseUrl;
		}


		public abstract SearchEngineOptions Engine { get; }

		public abstract string Name { get; }

		public abstract Color Color { get; }


		public virtual float? FilterThreshold => null;

		public virtual FullSearchResult GetResult(string url)
		{
			string rawUrl = GetRawResultUrl(url);

			var sr = new FullSearchResult(this, rawUrl)
			{
				RawUrl = rawUrl
			};


			return sr;
		}


		public virtual string GetRawResultUrl(string url)
		{
			return BaseUrl + url;
		}
	}
}