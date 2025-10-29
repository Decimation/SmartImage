// Read S SmartImage.Lib BaseUploadResponse.cs
// 2023-05-28 @ 7:49 PM

using SmartImage.Lib.Model;

namespace SmartImage.Lib.Engines.Upload;

public class UploadResult : IDisposable, ISize
{

	public Url Url { get; }

	public long? Size { get; }

	/*public static implicit operator Url(UploadResult result)
	{
		if (!result.IsValid) {
			throw new Exception();
		}

		return result.Url;
	}*/

	public UploadResult(Url url, long? size)
	{
		Url  = url;
		Size = size;
	}

	public void Dispose()
	{
		// Response?.Dispose();
		GC.SuppressFinalize(this);
	}

	public override string ToString()
	{
		return Url;
	}

}