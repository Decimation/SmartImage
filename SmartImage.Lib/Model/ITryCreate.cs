// Author: Deci | Project: SmartImage.Lib | Name: ITryCreate.cs
// Date: 2026/02/28 @ 01:02:42

namespace SmartImage.Lib.Model;

public interface ITryCreate<T> where T : ITryCreate<T>
{

	public static abstract Task<T> TryCreateAsync(object o, bool autoInit = true, bool autoDisposeOnError = true, CancellationToken ct = default);

}