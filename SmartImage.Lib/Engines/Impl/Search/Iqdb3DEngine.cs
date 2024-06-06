// Read S SmartImage.Lib IqdbEngine.cs
// 2023-01-13 @ 11:21 PM

// ReSharper disable UnusedMember.Global

#region

using System.Diagnostics;
using System.Net;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using AngleSharp.XPath;
using Flurl.Http;
using Kantan.Net.Utilities;
using Kantan.Text;
using SmartImage.Lib.Results;
using SmartImage.Lib.Utilities;

#endregion

// ReSharper disable StringLiteralTypo

namespace SmartImage.Lib.Engines.Impl.Search;

#nullable disable

public sealed class Iqdb3DEngine : IqdbEngine
{

	private const string URL_BASE  = "https://3d.iqdb.org/";
	private const string URL_QUERY = "https://3d.iqdb.org/?url=";

	public override SearchEngineOptions EngineOption => SearchEngineOptions.Iqdb3D;

	public Iqdb3DEngine() : base(URL_QUERY, URL_BASE) { }
}