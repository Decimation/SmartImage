// Author: Deci | Project: SmartImage.Lib | Name: WebSearchEngine.cs
// Date: 2024/06/06 @ 14:06:00

using System.Diagnostics;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using AngleSharp.XPath;
using Flurl.Http;
using Kantan.Diagnostics;
using Kantan.Net.Utilities;
using Microsoft.Extensions.Logging;
using SmartImage.Lib.Results;

namespace SmartImage.Lib.Engines;

/// <summary>
/// <list type="number">
/// <item>Request</item>
/// <item>Response</item>
/// <item> → Document</item>
/// <item> → → Nodes</item>
/// 
/// </list>
/// </summary>
public abstract class ResultParser<TResult, TItem>
{

	public IFlurlRequest Request { get; }

	public IFlurlResponse Response { get; }


	protected ResultParser(IFlurlRequest request, IFlurlResponse response)
	{
		Request  = request;
		Response = response;
	}

	public abstract Task<TResult> ParseAsync(TItem item, SearchResult sr);

}

public abstract class ResultWebData : ResultParser<SearchResultItem, INode>
{

	public IDocument Document { get; }

	// public IEnumerable<INode> Nodes { get; }

	// public string NodeSelector {get;}

	public virtual Task<IEnumerable<INode>> GetItems(string nodeS)
	{
		var nodes = Document.Body.SelectNodes(nodeS);

		return Task.FromResult<IEnumerable<INode>>(nodes);
	}

	/*public virtual Task<IEnumerable<INode>> NodeToItem(INode node, TItem item)
	{
		var nodes = Document.Body.SelectNodes(nodeS);

		return Task.FromResult<IEnumerable<INode>>(nodes);
	}*/

	public ResultWebData(IFlurlRequest request, IFlurlResponse response)
		: base(request, response) { }


	public abstract override Task<SearchResultItem> ParseAsync(INode item, SearchResult sr);

}

public abstract class WebSearchEngine : BaseSearchEngine
{

	protected abstract string NodesSelector { get; }

	protected WebSearchEngine([NN] Url baseUrl) : base(baseUrl) { }


	public override async Task<SearchResult> GetResultAsync(SearchQuery query, CancellationToken token = default)
	{
		var res = await base.GetResultAsync(query, token);

		IDocument doc = null;

		if (res.Status == SearchResultStatus.IllegalInput) {
			goto ret;
		}

		try {
			doc = await GetDocumentAsync(res, query: query, token: token);
		}
		catch (Exception e) {
			Logger.LogError(e, "{Name} error", e);

		}

		if (!Validate(doc, res)) {
			goto ret;
		}

		var nodes = (await GetNodes(doc));

		foreach (INode node in nodes) {
			if (token.IsCancellationRequested) {
				break;
			}

			var sri = await ParseResultItem(node, res);

			if (sri is { }) {
				res.Results.Add(sri);
			}
		}

		Logger.LogInformation("{Name} :: {RawUrl} document {DocLength}", Name, res.RawUrl, doc?.TextContent?.Length);

		res.Status = SearchResultStatus.Success;

	ret:
		res.Update();
		doc?.Dispose();
		Logger.LogDebug("Disposing {Name} doc", Name);
		return res;
	}

	[ICBN]
	[MURV]
	protected virtual async Task<IDocument> GetDocumentAsync(SearchResult sr, SearchQuery query,
	                                                         CancellationToken token = default)
	{

		var parser = new HtmlParser();

		try {

			var res = await Client.Request(sr.RawUrl)
				          .WithCookies(out var cj)
				          .WithTimeout(Timeout)
				          .WithHeaders(new
				          {
					          User_Agent = HttpUtilities.UserAgent
				          })
				          /*.OnError(s =>
				          {
					          s.ExceptionHandled = true;
				          })*/
				          .GetAsync(cancellationToken: token);

			var str = await res.GetStreamAsync();

			var document = await parser.ParseDocumentAsync(str, token);

			return document;

		}
		catch (Exception e) {
			// return await Task.FromException<IDocument>(e);
			Logger.LogError(e, "{Name} failed to get doc", Name);
			return null;
		}
	}

	protected abstract ValueTask<SearchResultItem> ParseResultItem(INode n, SearchResult r);

	protected virtual ValueTask<IEnumerable<INode>> GetNodes(IDocument d)
	{
		return ValueTask.FromResult<IEnumerable<INode>>(d.Body.SelectNodes(NodesSelector));
	}

	protected bool Validate([CBN] IDocument doc, SearchResult sr)
	{
		if (doc is null or { Body: null }) {
			return false;
		}

		foreach (string s in ErrorBodyMessages) {
			if (doc.Body.TextContent.Contains(s)) {
				return false;
			}
		}

		return true;

	}

}