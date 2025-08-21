// Read S SmartImage.Lib BaseUploadResponse.cs
// 2023-05-28 @ 7:49 PM

namespace SmartImage.Lib.Engines.Upload;

public class UploadResult : IDisposable
{

	public Url Url { get; protected internal set; }

	public long? Size { get; init; }

	public bool? IsValid { get; init; }

	/*public static implicit operator Url(UploadResult result)
	{
		if (!result.IsValid) {
			throw new Exception();
		}

		return result.Url;
	}*/

	public void Dispose()
	{
		// Response?.Dispose();
		GC.SuppressFinalize(this);
	}

}