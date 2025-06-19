// Read S SmartImage.Lib IResultParsable.cs
// 2023-07-04 @ 1:27 PM


// Read S SmartImage.Lib IResultParsable.cs
// 2023-07-04 @ 1:27 PM

using SmartImage.Lib.Results;

namespace SmartImage.Lib.Results.Data;

public interface IItemConvertable<TItem>
{

	public ValueTask<TItem> ToItem(SearchResult sr);

}

public interface ISearchResultItemConvertable
	: IItemConvertable<SearchResultItem> { }