// Author: Deci | Project: SmartImage.Lib | Name: ArchiveMoeEngine.cs
// Date: 2024/06/06 @ 14:06:00

using System.Security.Cryptography;
using System.Text.RegularExpressions;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using Novus.Streams;
using SmartImage.Lib.Images;
using SmartImage.Lib.Results;
using SmartImage.Lib.Results.Data;

namespace SmartImage.Lib.Engines.Impl.Search;

public class ArchiveMoeEngine : WebSearchEngine
{

	public override SearchEngineOptions EngineOption => SearchEngineOptions.ArchiveMoe;

	protected string Base64Hash { get; set; }

	protected override string NodesSelector => "//article[contains(@class,'post')]";

	public ArchiveMoeEngine() : this("https://archived.moe/_/search/") { }

	protected ArchiveMoeEngine(string baseUrl) : base(baseUrl) { }

	protected override Url GetRawUrl(SearchQuery query)
	{
		Base64Hash = GetHash(query);

		var r = Url.Combine(BaseUrl, "image", Base64Hash);
		return r;

		// return (BaseUrl.AppendPathSegments("image").AppendPathSegment(Base64Hash));
	}

	protected override ValueTask<SearchResultItem> ParseResultItem(INode n, SearchResult r)
	{
		// ReSharper disable PossibleNullReferenceException

		var p = ChanPost.Parse(n);
		return p.ToItem(r);

		// ReSharper restore PossibleNullReferenceException
	}

	protected static string GetHash(SearchQuery q)
	{
		//var digestBase64URL = digestBase64.replace('==', '').replace(/\//g, '_').replace(/\+/g, '-');
		using Stream stream = q.Source.Image.ToStream();
		var data   = MD5.HashData(stream);
		var b64    = Convert.ToBase64String(data).Replace("==", "");
		b64 = Regex.Replace(b64, @"\//", "_");
		b64 = Regex.Replace(b64, @"\+", "-");

		// q.Source.Stream.TrySeek();

		return b64;
	}

	public override void Dispose()
	{
		GC.SuppressFinalize(this);
	}

}

public record ChanPost : ISearchResultItemConverter<ChanPost>
{

	public string Author;
	public string Board;
	public Url    File;
	public string Filename;
	public int    Height;

	public long     Id;
	public string   Size;
	public string   Text;
	public string   Time1;
	public DateTime Time2;
	public string   Title;
	public string   Tripcode;
	public int      Width;

	public static ChanPost Parse(INode n)
	{
		var e = n as HtmlElement;

		var pff     = e.QuerySelector(".post_file_filename");
		var pfm     = e.QuerySelector(".post_file_metadata").TextContent.Split(", ");
		var pd      = e.QuerySelector(".post_data");
		var pt      = pd.QuerySelector(".post_title").TextContent;
		var pa      = pd.QuerySelector(".post_author").TextContent;
		var ptc     = pd.QuerySelector(".post_tripcode").TextContent;
		var tw      = pd.QuerySelector(".time_wrap").Children[0];
		var time2Ok = DateTime.TryParse(tw.GetAttribute("datetime"), out var time2);
		var time    = tw.TextContent;
		var text    = e.QuerySelector(".text").TextContent;

		var wh = pfm[1].Split('x');

		var p = new ChanPost
		{
			Id       = Int64.Parse(e.GetAttribute("id")),
			Board    = e.GetAttribute("data-board"),
			Filename = pff.TextContent,
			File     = pff.GetAttribute("href"),
			Width    = Int32.Parse(wh[0]),
			Height   = Int32.Parse(wh[1]),
			Size     = pfm[0],
			Title    = pt,
			Author   = pa,
			Tripcode = ptc,
			Time1    = time,
			Time2    = time2,
			Text     = text
		};

		return p;
	}

	public ValueTask<SearchResultItem> ToItem(SearchResult sr)
	{

		var sri = new SearchResultItem(sr)
		{
			Url         = File,
			Width       = Width,
			Height      = Height,
			Artist      = Author,
			Description = Title,
			Time        = Time2,
			Site        = File.Host,
			Metadata    = this
		};

		return ValueTask.FromResult(sri);
	}

}