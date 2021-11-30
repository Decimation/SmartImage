using System;
using System.Diagnostics;
using System.Net.Http;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using RestSharp;

namespace SmartImage.Lib.Searching;

/// <summary>
/// Contains originating information of a <see cref="SearchResult"/>
/// </summary>
public class SearchResultOrigin : IDisposable
{
	public ImageQuery Query { get; init; }

	public HttpResponseMessage InitialResponse { get; init; }

	public string Content { get; init; }
		
	public TimeSpan Retrieval { get; init; }

	public bool InitialSuccess { get; init; }

	public Uri RawUri { get; init; }

	public void Dispose()
	{
		InitialResponse?.Dispose();

	}
}