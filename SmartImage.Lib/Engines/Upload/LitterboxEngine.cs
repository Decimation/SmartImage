// ReSharper disable StringLiteralTypo

// ReSharper disable UnusedMember.Global

using Flurl.Http;
using Flurl.Http.Content;
using SmartImage.Lib.Utilities;

#pragma warning disable CS0618 // Type or member is obsolete
namespace SmartImage.Lib.Engines.Upload;

public sealed class LitterboxEngine : BaseCatboxEngine
{

	private const string LITTERBOX_BASE_URL = "https://litterbox.catbox.moe";
	private const string LITTERBOX_API_URL  = $"{LITTERBOX_BASE_URL}/resources/internals/api.php";

	public override UploadEngineOptions Option => UploadEngineOptions.Litterbox;

	public override long? MaxLength => 1_000_000_000L;

	protected override CapturedMultipartContent BuildContent(CapturedMultipartContent mp, string file)
	{
		mp = base.BuildContent(mp, file);
		mp = mp.TrimQuotesFromContentTypeBoundary();
		return mp;
	}

	public LitterboxEngine() : base(LITTERBOX_API_URL)
	{
		Timeout = TimeSpan.FromSeconds(30);
	}

}