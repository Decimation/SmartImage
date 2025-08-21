// Author: Deci | Project: SmartImage.Lib | Name: IResultConverter.cs
// Date: 2025/04/16 @ 01:04:44


// Author: Deci | Project: SmartImage.Lib | Name: IResultConverter.cs
// Date: 2025/04/16 @ 01:04:44

namespace SmartImage.Lib.Engines.Results;

public interface IResultItemParseable<in TSource, out TItem>
	where TItem : SearchResultItem
{

	public static abstract TItem ParseSource(TSource n, SearchResult r);

}