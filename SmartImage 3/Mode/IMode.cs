// Read S SmartImage IMode.cs
// 2023-02-14 @ 12:12 AM

namespace SmartImage.Mode;

public interface IMode
{
	public Task<object?> RunAsync(object? sender);
}