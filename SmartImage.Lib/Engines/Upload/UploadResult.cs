// Read S SmartImage.Lib BaseUploadResponse.cs
// 2023-05-28 @ 7:49 PM

using System.Text.Json.Serialization;
using SmartImage.Lib.Engines.Upload.Base;
using SmartImage.Lib.Model;

namespace SmartImage.Lib.Engines.Upload;

public interface IUploadResult : IUrl, ILength { }

public class UploadResult : IUploadResult
{

	public Url Url { get; set; }

	[JPN("Size")]
	public long? Length { get; set; }

	public UploadResult() { }

	public UploadResult(Url url, long? size)
	{
		Url    = url;
		Length = size;
	}

	public override string ToString()
	{
		return Url;
	}

}