using System;
using System.Collections.Generic;
using System.Text;
using SmartImage.Lib.Engines.Results;

namespace SmartImage.Lib.Images.Uni
{
	public class ScannedResultItem : SearchResultItem
	{

		internal ScannedResultItem(SearchResult r, bool isRaw = false) : base(r, isRaw) { }

	}
}
