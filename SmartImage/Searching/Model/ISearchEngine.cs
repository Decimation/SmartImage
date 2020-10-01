#region

#endregion

using System;
using System.Drawing;

namespace SmartImage.Searching.Model
{
	public interface ISearchEngine
	{
		public string Name { get; }

		public SearchEngines Engine { get; }

		public SearchResult GetResult(string url);

		public Color Color { get; }

	}
}