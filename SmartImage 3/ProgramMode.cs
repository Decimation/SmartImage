using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Novus.Win32;
using SmartImage.Lib;

namespace SmartImage;

public abstract class ProgramMode : IDisposable
{
	public ProgramMode(SearchQuery q)
	{
		Query = q;
	}

	public ProgramMode() : this(SearchQuery.Null) { }

	public SearchQuery Query { get; protected set; }

	public SearchConfig Config { get; protected set; }

	public volatile bool Status;

	public abstract Task<object> Run(SearchClient c, string[] args);

	public abstract Task PreSearch(SearchConfig c, object? sender);

	public abstract Task PostSearch(SearchConfig c, object? sender, List<SearchResult> results1);

	public abstract Task OnResult(object o, SearchResult r);

	public abstract Task OnComplete(object sender, List<SearchResult> e);
		
	public abstract Task Close();

	protected void RootHandler(SearchEngineOptions t2, SearchEngineOptions t3, bool t4)
	{
		Config.SearchEngines   = t2;
		Config.PriorityEngines = t3;
			
		Config.OnTop = t4;

		if (Config.OnTop) {
			Native.KeepWindowOnTop(Cache.HndWindow);
		}
	}

	#region Implementation of IDisposable

	public abstract void Dispose();

	#endregion
}