// Read Stanton SmartImage IMain.cs
// 2023-01-31 @ 11:22 AM

namespace SmartImage.Utilities;

public interface IMain
{
	public Task<object?> RunAsync(object? sender);
}