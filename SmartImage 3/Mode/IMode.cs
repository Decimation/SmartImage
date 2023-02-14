// Read Stanton SmartImage IMain.cs
// 2023-01-31 @ 11:22 AM

namespace SmartImage.Mode;

public interface IMode
{
    public Task<object?> RunAsync(object? sender);
}