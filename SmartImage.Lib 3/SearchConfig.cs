using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace SmartImage.Lib;

public class SearchConfig
{
	public SearchEngineOptions Engines
	{
		get;
		set;
	}

	public SearchConfig()
	{
	}
}