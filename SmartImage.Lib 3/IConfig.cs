// Read Stanton SmartImage.Lib IConfig.cs
// 2023-01-13 @ 11:09 PM

namespace SmartImage.Lib;

public interface IConfig
{
	public ValueTask ApplyAsync(SearchConfig cfg);
}