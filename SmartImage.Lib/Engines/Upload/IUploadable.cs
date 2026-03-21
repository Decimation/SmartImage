// Author: Deci | Project: SmartImage.Lib | Name: IUploadable.cs
// Date: 2026/03/21 @ 09:03:29

using SmartImage.Lib.Engines.Upload.Base;
using SmartImage.Lib.Model;

namespace SmartImage.Lib.Engines.Upload;

public interface IUploadable
{

	IUploadResult Upload { get; }

	[MNNW(true, nameof(Upload))]
	public bool IsUploaded => Upload != null && Url.IsValid(Upload.Url);

	// IUploadEngine Engine { get; }

	// Task<IUploadResult> UploadAsync(IUploadEngine engine);

}