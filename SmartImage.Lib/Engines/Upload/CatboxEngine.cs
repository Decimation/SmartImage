#pragma warning disable CS0618 // Type or member is obsolete
namespace SmartImage.Lib.Engines.Upload;

public sealed class CatboxEngine : BaseCatboxEngine
{
	/// <summary>
	/// <c>200MB</c>
	/// </summary>
	public override long? MaxLength => 200_000_000L;

	public CatboxEngine() : base("https://catbox.moe/user/api.php") { }

}