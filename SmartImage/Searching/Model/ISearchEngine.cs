using System.Drawing;

namespace SmartImage.Searching.Model
{
	public interface ISearchEngine
	{
		public string Name { get; }

		public SearchEngineOptions Engine { get; }

		public FullSearchResult GetResult(string url);

		public Color Color { get; }
	}
}