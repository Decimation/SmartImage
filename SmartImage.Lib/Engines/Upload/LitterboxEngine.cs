

// ReSharper disable StringLiteralTypo

// ReSharper disable UnusedMember.Global

namespace SmartImage.Lib.Engines.Upload;

public sealed class LitterboxEngine : BaseCatboxEngine
{

	public override UploadEngineOptions Option => UploadEngineOptions.Litterbox;


	public override long? MaxLength => 1_000_000_000L;

	public LitterboxEngine() : base("https://litterbox.catbox.moe/resources/internals/api.php") { }
}