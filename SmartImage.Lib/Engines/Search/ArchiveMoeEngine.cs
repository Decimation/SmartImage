// Author: Deci | Project: SmartImage.Lib | Name: ArchiveMoeEngine.cs
// Date: 2024/06/06 @ 14:06:00

using System.Security.Cryptography;
using System.Text.RegularExpressions;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.XPath;
using Novus.Streams;
using SmartImage.Lib.Engines.Results;
using SmartImage.Lib.Engines.Results.Model;
using SmartImage.Lib.Images;

namespace SmartImage.Lib.Engines.Search;

public class ArchiveMoeEngine : WebSearchEngine<ChanPost, IList<INode>>
{

	public override SearchEngineOptions EngineOption => SearchEngineOptions.ArchiveMoe;

	protected string Base64Hash { get; set; }


	public ArchiveMoeEngine() : this("https://archived.moe/_/search/") { }

	protected ArchiveMoeEngine(string baseUrl) : base(baseUrl) { }

	protected override Url GetRawUrl(SearchQuery query)
	{
		Base64Hash = GetHash(query);

		var r = Url.Combine(BaseUrl, "image", Base64Hash);
		return r;

		// return (BaseUrl.AppendPathSegments("image").AppendPathSegment(Base64Hash));
	}


	protected static string GetHash(SearchQuery q)
	{
		//var digestBase64URL = digestBase64.replace('==', '').replace(/\//g, '_').replace(/\+/g, '-');
		using Stream stream = q.Source.Image.ToStream();
		var          data   = MD5.HashData(stream);
		var          b64    = Convert.ToBase64String(data).Replace("==", "");
		b64 = Regex.Replace(b64, @"\//", "_");
		b64 = Regex.Replace(b64, @"\+", "-");

		// q.Source.Stream.TrySeek();

		return b64;
	}

	public override ValueTask<bool> ApplyConfigAsync(SearchConfig cfg, CancellationToken ct = default)
	{
		return ValueTask.FromResult(true);

	}

	public override void Dispose()
	{
		GC.SuppressFinalize(this);
	}

	protected override ValueTask<IList<INode>> GetSource(IDocument d)
	{
		return ValueTask.FromResult<IList<INode>>(d.Body.SelectNodes("//article[contains(@class,'post')]"));
	}

	protected override ValueTask<IEnumerable<ChanPost>> GetItems(IList<INode> rs, SearchResult r)
	{
		var buf = new List<ChanPost>(rs.Count);

		foreach (INode node in rs)
		{
			buf.Add(ChanPost.ParseResultItem(node, r));
		}
		return ValueTask.FromResult<IEnumerable<ChanPost>>(buf);
	}

}

public record ChanPost : SearchResultItem, ISourceItemParseable<INode, ChanPost>
{

	public string Board { get; private set; }

	public Url File { get; private set; }

	public string Filename { get; private set; }

	public long Id { get; private set; }

	public string Size { get; private set; }

	public string Text { get; private set; }

	// public string Time1 { get; private set; }

	// public DateTime Time2 { get; private set; }

	public string Tripcode { get; private set; }


	private ChanPost(SearchResult r) : base(r) { }

	public static ChanPost ParseResultItem(INode n, SearchResult r)
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

		var file = Url.Parse(pff.GetAttribute("href"));

		var p = new ChanPost(r)
		{
			Id       = long.Parse(e.GetAttribute("id")),
			Board    = e.GetAttribute("data-board"),
			Filename = pff.TextContent,
			File     = file,
			Width    = int.Parse(wh[0]),
			Height   = int.Parse(wh[1]),
			Size     = pfm[0],
			Title    = pt,
			Artist   = pa,
			Site     = file.Host,
			Tripcode = ptc,
			Time     = time2,
			Text     = text
		};

		return p;
	}

}