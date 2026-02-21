// Read S SmartImage.Lib IqdbEngine.cs
// 2023-01-13 @ 11:21 PM

// ReSharper disable UnusedMember.Global

#region

#endregion

// ReSharper disable StringLiteralTypo

namespace SmartImage.Lib.Engines.Search;

#nullable disable

public sealed class Iqdb3DEngine : IqdbEngine
{

	private const string URL_BASE  = "https://3d.iqdb.org/";
	private const string URL_QUERY = "https://3d.iqdb.org/?url=";

	public override SearchEngineOptions Option => SearchEngineOptions.Iqdb3D;

	public Iqdb3DEngine() : base(URL_QUERY) { }

	public override Url Endpoint => URL_BASE;

}