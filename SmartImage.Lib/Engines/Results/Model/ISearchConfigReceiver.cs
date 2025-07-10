// Read Stanton SmartImage.Lib ISearchConfigReceiver.cs
// 2023-01-13 @ 11:09 PM

namespace SmartImage.Lib.Engines.Results.Model;

public interface ISearchConfigReceiver
{
	public ValueTask<bool> ApplyConfigAsync(SearchConfig cfg, CancellationToken ct = default);
}