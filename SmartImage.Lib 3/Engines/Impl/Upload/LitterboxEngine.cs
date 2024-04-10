using Flurl.Http;

// ReSharper disable StringLiteralTypo

// ReSharper disable UnusedMember.Global

namespace SmartImage.Lib.Engines.Impl.Upload;

public sealed class LitterboxEngine : BaseCatboxEngine
{
	public override string Name => "Litterbox";

	public override long? MaxSize => 1_000_000_000L;

	public static readonly BaseCatboxEngine Instance = new LitterboxEngine();

	public LitterboxEngine() : base("https://litterbox.catbox.moe/resources/internals/api.php") { }
}