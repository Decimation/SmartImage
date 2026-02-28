// ReSharper disable StringLiteralTypo

// ReSharper disable UnusedMember.Global

using Flurl.Http;
using Flurl.Http.Content;
using SmartImage.Lib.Utilities;

namespace SmartImage.Lib.Engines.Upload;

public sealed class LitterboxEngine : BaseCatboxEngine
{

	private const string LITTERBOX_BASE_URL = "https://litterbox.catbox.moe";

	public override UploadEngineOptions Option => UploadEngineOptions.Litterbox;

	public override long? MaxLength => 1_000_000_000L;

	protected override CapturedMultipartContent BuildContent(CapturedMultipartContent mp, string file)
	{
		return base.BuildContent(mp, file).TrimQuotesFromContentTypeBoundary();
	}

	public LitterboxEngine() : base($"{LITTERBOX_BASE_URL}/resources/internals/api.php") { }
}