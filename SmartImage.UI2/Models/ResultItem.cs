// Author: Deci | Project: SmartImage.UI2 | Name: ResultItem.cs
// Date: 2025/10/29 @ 11:10:52

using ReactiveUI;
using SmartImage.Lib.Engines.Results;

namespace SmartImage.UI2.Models;

public class ResultItem : ReactiveObject
{

	private SearchResultItem m_item;

	public ResultItem(SearchResultItem item)
	{
		m_item = item;
	}

}