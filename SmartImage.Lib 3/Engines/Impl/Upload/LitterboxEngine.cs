using Flurl.Http;

// ReSharper disable StringLiteralTypo

// ReSharper disable UnusedMember.Global

namespace SmartImage.Lib.Engines.Impl.Upload;

public sealed class LitterboxEngine : BaseCatboxEngine
{
	public override string Name => "Litterbox";

	public override long MaxSize => 1 * 1000 * 1000 * 1000;

	public LitterboxEngine() : base("https://litterbox.catbox.moe/resources/internals/api.php") { }
}