using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SmartImage.Lib.Engines;
using SmartImage.Lib.Searching;

namespace SmartImage.Lib
{
	/// <summary>
	/// Contains configuration for <see cref="SearchClient"/>
	/// </summary>
	public sealed class SearchConfig
	{
		public ImageQuery Query { get; set; }

		public SearchEngineOptions SearchEngines { get; set; } = SearchEngineOptions.All;

		public SearchEngineOptions PriorityEngines { get; set; }

		public bool Filter { get; set; } = true;

	}
}