// Read S SmartImage.Lib BaseUploadResponse.cs
// 2023-05-28 @ 7:49 PM

using Flurl.Http;

namespace SmartImage.Lib.Results;

public sealed class UploadResult : IDisposable
{
	public Url Url { get; init; }

	// public IFlurlResponse Response { get; init; }

	public long? Size { get; init; }

	public bool IsValid { get; init; }

	[CBN]
	public object Value { get; init; }

	/*public static implicit operator Url(UploadResult result)
	{
		if (!result.IsValid) {
			throw new Exception();
		}

		return result.Url;
	}*/

	public void Dispose() { }
}