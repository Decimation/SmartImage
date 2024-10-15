// Read Stanton SmartImage.Lib IConfigurable.cs
// 2023-01-13 @ 11:09 PM

namespace SmartImage.Lib.Results.Data;

public interface IConfigurable
{
	public ValueTask ApplyAsync(SearchConfig cfg);

}