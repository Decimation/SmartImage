using System;
using System.Diagnostics;
using System.Net.Http;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;

namespace SmartImage.Lib.Searching;

/// <summary>
/// Contains originating information of a <see cref="SearchResult"/>
/// </summary>
public class SearchResultOrigin : IDisposable
{
	public ImageQuery Query { get; init; }

	public HttpResponseMessage Response { get; init; }

	public string Content { get; init; }
		
	public TimeSpan Retrieval { get; init; }

	public bool Success { get; init; }

	public Uri RawUri { get; init; }

	public void Dispose()
	{
		Response?.Dispose();
		GC.SuppressFinalize(this);

	}
}