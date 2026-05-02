// Author: Deci | Project: SmartImage.Lib | Name: ArchiveMoeEngine.cs
// Date: 2024/06/06 @ 14:06:00

using System.Security.Cryptography;
using System.Text.RegularExpressions;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.XPath;
using SmartImage.Lib.Engines.Results;
using SmartImage.Lib.Engines.Search.Base;
using SmartImage.Lib.Utilities.Diagnostics;

// ReSharper disable UnusedVariable

#nullable disable

// ReSharper disable InconsistentNaming

namespace SmartImage.Lib.Engines.Search;

public partial class ArchiveMoeEngine : WebSearchEngine<ChanPost, IList<INode>>
{

	public const string URL_ENDPOINT = "https://archived.moe/_/search/";

	public override SearchEngineOptions Option => SearchEngineOptions.ArchiveMoe;

	protected string Base64MD5Hash { get; set; }

	public ArchiveMoeEngine() : this(URL_ENDPOINT) { }

	protected ArchiveMoeEngine(Url url) : base(url) { }

	protected override Url GetRawUrl(SearchQuery query)
	{
		Base64MD5Hash = GetBase64MD5Hash(query.Source.Bytes);

		return Url.Combine(Url, "image", Base64MD5Hash);
	}


	protected override ValueTask<IList<INode>> ParseDataAsync(IDocument src)
	{
		return ValueTask.FromResult<IList<INode>>(src.Body.SelectNodes("//article[contains(@class,'post')]"));
	}

	protected override ValueTask<IEnumerable<ChanPost>> ParseItemsAsync(IList<INode> source, SearchResult r)
	{
		var buf = new List<ChanPost>(source.Count);

		foreach (INode node in source) {
			buf.Add(ChanPost.ParseSource(node, r));
		}

		return ValueTask.FromResult<IEnumerable<ChanPost>>(buf);
	}

	public static string GetBase64MD5Hash(Span<byte> srcBytes)
	{
		//var digestBase64URL = digestBase64.replace('==', '').replace(/\//g, '_').replace(/\+/g, '-');
		var data = MD5.HashData(srcBytes);

		var b64 = Convert.ToBase64String(data).Replace("==", "");
		b64 = r_slashes().Replace(b64, "_");
		b64 = r_fwLook().Replace(b64, "-");

		return b64;
	}

	[GeneratedRegex(@"\//")]
	private static partial Regex r_slashes();

	[GeneratedRegex(@"\+")]
	private static partial Regex r_fwLook();

	public override void Dispose()
	{
		GC.SuppressFinalize(this);
	}

}

public record ChanPost : SearchResultItem, IParseableResult<INode, ChanPost>
{

	public string Board { get; private set; }

	public Url File { get; private set; }

	public string Filename { get; private set; }

	public long Id { get; private set; }

	public string SizeString { get; private set; }

	public string Text { get; private set; }

	// public string Time1 { get; private set; }

	// public DateTime Time2 { get; private set; }

	public string Tripcode { get; private set; }


	private ChanPost(SearchResult r) : base(r) { }

	public static ChanPost ParseSource(INode n, SearchResult r)
	{
		var e = n as HtmlElement;

		ArgumentNullException.ThrowIfNull(e);

		var pff = e.QuerySelector(".post_file_filename");
		var pfm = e.QuerySelector(".post_file_metadata").TextContent.Split(", ");

		ParseException.ThrowIfNull(pfm);

		var pd  = e.QuerySelector(".post_data");
		var pt  = pd.QuerySelector(".post_title").TextContent;
		var pa  = pd.QuerySelector(".post_author").TextContent;
		var ptc = pd.QuerySelector(".post_tripcode")?.TextContent;
		var tw  = pd.QuerySelector(".time_wrap");

		ParseException.ThrowIfNull(tw);

		var twC0    = tw.Children[0];
		var time2Ok = DateTime.TryParse(twC0.GetAttribute("datetime"), out var time2);
		var time    = twC0.TextContent;
		var text    = e.QuerySelector(".text")?.TextContent;

		var wh = pfm[1].Split('x');

		var file = Url.Parse(pff.GetAttribute("href"));

		var p = new ChanPost(r)
		{
			Id         = Int64.Parse(e.GetAttribute("id")),
			Board      = e.GetAttribute("data-board"),
			Filename   = pff.TextContent,
			File       = file,
			Width      = Int32.Parse(wh[0]),
			Height     = Int32.Parse(wh[1]),
			SizeString = pfm[0],
			Title      = pt,
			Artist     = pa,
			Site       = file.Host,
			Tripcode   = ptc,
			Time       = time2,
			Text       = text
		};

		return p;
	}

}