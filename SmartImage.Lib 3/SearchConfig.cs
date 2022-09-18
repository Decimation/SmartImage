using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace SmartImage.Lib;

public sealed class SearchConfig
{
	public SearchEngineOptions SearchEngines { get; set; } = SearchEngineOptions.All;

	public SearchEngineOptions PriorityEngines { get; set; } = SearchEngineOptions.Auto;

	public SearchConfig() { }

	public override string ToString()
	{
		return $"{nameof(SearchEngines)}: {SearchEngines}, \n" +
		       $"{nameof(PriorityEngines)}: {PriorityEngines}";
	}
}