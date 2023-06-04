using Flurl.Http;

namespace SmartImage.Lib.Engines.Impl.Upload;

public sealed class CatboxEngine : BaseCatboxEngine
{

	public override string Name => "Catbox";

	public override long MaxSize => 1 * 1000 * 1000 * 200;

	public CatboxEngine() : base("https://catbox.moe/user/api.php")
	{
		Paranoid = true;
	}

}