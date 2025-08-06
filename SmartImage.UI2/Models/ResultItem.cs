// Author: Deci | Project: SmartImage.UI2 | Name: Models.cs
// Date: 2025/08/06 @ 13:08:44

using SmartImage.Lib.Engines.Results;

namespace SmartImage.UI2.Models;

public class ResultItem
{

	public string Url { get; }

	public ResultItem(SearchResultItem sri)
	{
		Url = sri.Url;
	}

}