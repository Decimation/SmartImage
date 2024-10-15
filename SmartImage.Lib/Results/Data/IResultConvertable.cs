// Read S SmartImage.Lib IResultParsable.cs
// 2023-07-04 @ 1:27 PM


// Read S SmartImage.Lib IResultParsable.cs
// 2023-07-04 @ 1:27 PM

using SmartImage.Lib.Results;

namespace SmartImage.Lib.Results.Data;

public interface IResultConvertable
{
	public SearchResultItem Convert(SearchResult sr, out SearchResultItem[] children);

}