using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kantan.Diagnostics;
using SmartImage.Lib.Searching;

namespace SmartImage.Lib.Engines
{
	public static class EngineHelper
	{
		public static SearchResult TryGet(SearchResult sr, ImageQuery query, Func<SearchResult, SearchResult> f)
		{
			

			if (!sr.IsSuccessful) {
				return sr;
			}

			try {

				sr = f(sr);
			}
			catch (Exception e) {
				sr.Status = ResultStatus.Failure;
				Trace.WriteLine($"{sr.Engine.Name}: {e.Message}", LogCategories.C_ERROR);
			}

			return sr;
		}
	}
}