// Read S SmartImage.Lib IqdbEngine.cs
// 2023-01-13 @ 11:21 PM

// ReSharper disable UnusedMember.Global

#region

using System.Diagnostics;
using System.Net;
using AngleSharp.Css.Values;
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
	public override string EndpointUrl => "https://3d.iqdb.org/";

	public override SearchEngineOptions EngineOption => SearchEngineOptions.Iqdb3D;

	public Iqdb3DEngine() : base("https://3d.iqdb.org/?url=") { }
}