#region

using System.Threading.Tasks;
using SmartImage.Searching;

#endregion

namespace SmartImage.Model
{
	public interface ISearchEngine
	{
		public string Name { get; }

		public SearchEngines Engine { get; }

		public SearchResult GetResult(string url);
	}
}