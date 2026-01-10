// Author: Deci | Project: SmartImage.Rdx | Name: SearchServerResponse.cs
// Date: 2025/02/04 @ 12:02:39

using System.Text.Json.Serialization;
using SmartImage.Lib.Engines.Results;
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

namespace SmartImage.Rdx.Commands.Server;

public class SearchServerResponse
{

	[MN]
	[JsonPropertyOrder(0)]
	public SearchResultItem Best { get; internal set; }

	[JsonPropertyOrder(1)]
	public SearchResult[] Results { get; internal set; }

	[MN]
	[JsonPropertyOrder(2)]
	public string Message { get; internal set; }

	public SearchServerResponse() { }

	public SearchServerResponse(SearchResult[] results, SearchResultItem best)
	{
		Best = best;
		Results = results;
	}

}