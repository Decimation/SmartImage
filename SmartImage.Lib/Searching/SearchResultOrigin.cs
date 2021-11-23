using System;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using RestSharp;

namespace SmartImage.Lib.Searching;

/// <summary>
/// Contains originating information of a <see cref="SearchResult"/>
/// </summary>
public class SearchResultOrigin
{
	public ImageQuery Query { get; init; }

	public IRestResponse InitialResponse { get; init; }

	public TimeSpan Retrieval { get; init; }

	public bool InitialSuccess { get; init; }

	public Uri RawUri { get; init; }
}