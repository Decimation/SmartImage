// Author: Deci | Project: SmartImage.Lib | Name: IUploadEngine.cs
// Date: 2026/02/28 @ 12:02:15

using Flurl.Http;
using SmartImage.Lib.Images.Uni;
using SmartImage.Lib.Model;

namespace SmartImage.Lib.Engines.Upload;

public interface IUploadEngine : INamedEnumOption<UploadEngineOptions>, IMaxLength, IUrl, ITimeout, IDisposable
{

	Task<UploadResult> UploadAsync(UniImage query, CancellationToken ct = default);

	Task<UploadResult> UploadFileAsync(string file, CancellationToken ct = default);

	Task<UploadResult> ProcessResponseAsync(IFlurlResponse response, CancellationToken ct = default);

	void Verify(UniImage file);

}