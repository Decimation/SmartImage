// Author: Deci | Project: SmartImage.Lib | Name: ISearchEngine.cs
// Date: 2025/09/24 @ 16:09:37

namespace SmartImage.Lib.Engines;

public interface ISearchEngine
{

	public SearchEngineOptions EngineOption { get; }

	public string Name
	{
		get => EngineOption.ToString();
	}

	// public bool IsPriority { get; }

	// public static abstract T Create<T>(SearchConfig cfg) where T : BaseSearchEngine, ISearchEngine;

}