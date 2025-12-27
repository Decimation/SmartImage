// Read S SmartImage.Lib BaseUploadResponse.cs
// 2023-05-28 @ 7:49 PM

using System.Text.Json.Serialization;
using SmartImage.Lib.Model;

namespace SmartImage.Lib.Engines.Upload;

[JsonDerivedType(typeof(PomfFileResult))]
public class UploadResult : IDisposable, ILength
{

	public Url Url { get; set;}

	[JPN("Size")]
	public long? Length { get; set;}

	/*public static implicit operator Url(UploadResult result)
	{
		if (!result.IsValid) {
			throw new Exception();
		}

		return result.Url;
	}*/
	
	// [JsonConstructor]
	public UploadResult() {  }

	// [JsonConstructor]
	public UploadResult(Url url, long? size)
	{
		Url  = url;
		Length = size;
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