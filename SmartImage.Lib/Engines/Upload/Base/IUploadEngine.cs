// Author: Deci | Project: SmartImage.Lib | Name: IUploadEngine.cs
// Date: 2026/02/28 @ 12:02:15

using Flurl.Http;
using SmartImage.Lib.Images.Uni;
using SmartImage.Lib.Model;

namespace SmartImage.Lib.Engines.Upload.Base;

public interface IUploadEngine : INamedEnumOption<UploadEngineOption>, IMaxLength, IUrl, ITimeout, IDisposable
{

	Task<IUploadResult> UploadFileAsync(string file, CancellationToken ct = default);

	Task<IUploadResult> ProcessResponseAsync(IFlurlResponse response, CancellationToken ct = default);

	void Verify(IUniImage file);

}