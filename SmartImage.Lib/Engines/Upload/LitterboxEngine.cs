

// ReSharper disable StringLiteralTypo

// ReSharper disable UnusedMember.Global

namespace SmartImage.Lib.Engines.Upload;

public sealed class LitterboxEngine : BaseCatboxEngine
{

	public override UploadEngineOptions UploadOption => UploadEngineOptions.Litterbox;


	public override long? MaxSize => 1_000_000_000L;

	public LitterboxEngine() : base("https://litterbox.catbox.moe/resources/internals/api.php") { }
}