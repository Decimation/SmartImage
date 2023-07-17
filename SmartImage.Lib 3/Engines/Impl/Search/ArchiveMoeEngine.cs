using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using JetBrains.Annotations;
using SmartImage.Lib.Model;
using SmartImage.Lib.Results;

namespace SmartImage.Lib.Engines.Impl.Search;

public class ArchiveMoeEngine : WebSearchEngine
{
	public ArchiveMoeEngine() : this("https://archived.moe/_/search/") { }

	protected ArchiveMoeEngine(string baseUrl) : base(baseUrl) { }

	public override SearchEngineOptions EngineOption => SearchEngineOptions.ArchiveMoe;

	public override void Dispose() { }

	protected string Base64Hash { get; set; }

	protected static string GetHash(SearchQuery q)
	{
		//var digestBase64URL = digestBase64.replace('==', '').replace(/\//g, '_').replace(/\+/g, '-');
		var b64 = Convert.ToBase64String(q.Hash.Span).Replace("==", "");
		b64 = Regex.Replace(b64, @"\//", "_");
		b64 = Regex.Replace(b64, @"\+", "-");

		return b64;
	}

	protected override async ValueTask<Url> GetRawUrlAsync(SearchQuery query)
	{
		Base64Hash = GetHash(query);

		return BaseUrl.AppendPathSegments("image").AppendPathSegment(Base64Hash);
	}

	protected override async ValueTask<SearchResultItem> ParseResultItem(INode n, SearchResult r)
	{
		// ReSharper disable PossibleNullReferenceException

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

		var p = new ChanPost()
		{
			Id       = long.Parse(e.GetAttribute("id")),
			Board    = e.GetAttribute("data-board"),
			Filename = pff.TextContent,
			File     = pff.GetAttribute("href"),
			Width    = long.Parse(wh[0]),
			Height   = long.Parse(wh[1]),
			Size     = pfm[0],
			Title    = pt,
			Author   = pa,
			Tripcode = ptc,
			Time1    = time,
			Time2    = time2,
			Text     = text
		};

		return p.Convert(r);

		// ReSharper restore PossibleNullReferenceException

	}

	protected override string NodesSelector => "//article[contains(@class,'post')]";
}

internal record ChanPost : IResultConvertable
{
	public long     Id;
	public string   Board;
	public string   Filename;
	public Url      File;
	public long     Width;
	public long     Height;
	public string   Size;
	public string   Title;
	public string   Author;
	public string   Tripcode;
	public string   Time1;
	public DateTime Time2;
	public string   Text;

	public SearchResultItem Convert(SearchResult sr)
	{
		var sri = new SearchResultItem(sr)
		{
			Url         = File,
			Width       = Width,
			Height      = Height,
			Artist      = Author,
			Description = Title,
			Time        = Time2,
			Metadata    = this
		};

		return sri;
	}
}