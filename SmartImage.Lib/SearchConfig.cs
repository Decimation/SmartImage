using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmartImage.Lib.Engines;
using SmartImage.Lib.Searching;

namespace SmartImage.Lib
{
	public class SearchConfig
	{
		public ImageQuery Query { get; init; }

		public SearchEngineOptions SearchEngines { get; init; }

		public SearchEngineOptions PriorityEngines { get; init; }
	}
}
