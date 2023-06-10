// Read S SmartImage.Lib BaseUploadResponse.cs
// 2023-05-28 @ 7:49 PM

using Flurl.Http;

namespace SmartImage.Lib.Engines.Impl.Upload;

public sealed class BaseUploadResponse : IDisposable
{
	public Url Url { get; init; }

	public IFlurlResponse Response { get; init; }

	public bool IsValid { get; init; }

	public void Dispose()
	{
		Response?.Dispose();
	}
}