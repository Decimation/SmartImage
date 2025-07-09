// Read S SmartImage.Lib IResultParsable.cs
// 2023-07-04 @ 1:27 PM


// Read S SmartImage.Lib IResultParsable.cs
// 2023-07-04 @ 1:27 PM

using SmartImage.Lib.Results;

namespace SmartImage.Lib.Results.Data;


public interface IToSearchResultItem
{

	public ValueTask<SearchResultItem> ToItem(SearchResult sr);


}