using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Ini;

namespace SmartImage.Lib;

public class SearchConfig
{
	public IConfiguration Configuration { get; }

	public SearchEngineOptions Engines
	{
		get
		{
			var s = Configuration.GetValue<SearchEngineOptions>("engines");
			return s;
		}
	}

	public SearchConfig()
	{
		var builder = new ConfigurationBuilder();

		Configuration = builder
		                .SetBasePath(Directory.GetCurrentDirectory())
		                .AddIniFile("smartimage.ini")
		                .Build();
		
	}
}