using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmartImage.Lib.Searching;

namespace SmartImage.Lib.Engines.Search.Base;

public abstract class WebDriverSearchEngine : ProcessedSearchEngine
{
	protected WebDriverSearchEngine(string baseUrl) : base(baseUrl) { }

	public abstract override SearchEngineOptions EngineOption { get; }
	
	public abstract override EngineSearchType SearchType { get; }

	protected abstract Task<List<ImageResult>> Browse(ImageQuery sd, SearchResult r);
}