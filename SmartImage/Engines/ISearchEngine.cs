using System.Drawing;
using SmartImage.Searching;

namespace SmartImage.Engines
{
	public interface ISearchEngine
	{
		public string Name { get; }

		public SearchEngineOptions Engine { get; }

		public FullSearchResult GetResult(string url);

		public Color Color { get; }
	}
}