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
using SmartImage.Lib.Results.Data;

namespace SmartImage.Lib.Engines;

abstract class ParsedSearchEngine<T> : BaseSearchEngine
{

	//todo

	protected ParsedSearchEngine([NN] Url baseUrl) : base(baseUrl) { }

	protected abstract ValueTask<IEnumerable<T>> GetRawItems();

}

public abstract class WebSearchEngine : BaseSearchEngine
{

	protected abstract string NodesSelector { get; }

	protected WebSearchEngine([NN] Url baseUrl) : base(baseUrl) { }


	public override async Task<SearchResult> GetResultAsync(SearchQuery query, CancellationToken token = default)
	{
		var res = await base.GetResultAsync(query, token);

		IDocument doc = null;

		if (res.Status == SearchResultStatus.IllegalInput)
		{
			goto ret;
		}

		try
		{
			doc = await GetDocumentAsync(res, query: query, token: token);
		}
		catch (Exception e)
		{
			Logger.LogError(e, "{Name} error", e);

		}

		if (!Validate(doc, res))
		{
			goto ret;
		}

		var nodes = (await GetNodes(doc));

		var sri = await GetItems(nodes, res);
		res.Results.AddRange(sri);


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

		try
		{

			using var res = await Client.Request(sr.RawUrl)
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
		catch (Exception e)
		{
			// return await Task.FromException<IDocument>(e);
			Logger.LogError(e, "{Name} failed to get doc", Name);
			return null;
		}
	}

	protected abstract ValueTask<IEnumerable<SearchResultItem>> GetItems(IEnumerable<INode> n, SearchResult r);


	protected virtual ValueTask<IEnumerable<INode>> GetNodes(IDocument d)
	{
		return ValueTask.FromResult<IEnumerable<INode>>(d.Body.SelectNodes(NodesSelector));
	}

#region Overrides of BaseSearchEngine

	protected override Url GetRawUrl(SearchQuery query)
	{
		return base.GetRawUrl(query);
	}

#endregion

	protected bool Validate([CBN] IDocument doc, SearchResult sr)
	{
		if (doc is null or { Body: null })
		{
			return false;
		}

		foreach (string s in ErrorBodyMessages)
		{
			if (doc.Body.TextContent.Contains(s))
			{
				return false;
			}
		}

		return true;

	}

}