using System.Net.Http.Headers;
using System.Text.Json;
using Flurl.Http.Content;
using Kantan.Net.Utilities;
using Microsoft.Net.Http.Headers;
using SmartImage.Lib.Engines.Results;

namespace SmartImage.Lib.Utilities;

public static class SearchUtil
{

	extension(SearchResponseFlags s)
	{

		public bool IsError() => s is SearchResponseFlags.Unknown or SearchResponseFlags.IllegalInput
			                         or SearchResponseFlags.Unavailable or SearchResponseFlags.Cooldown;

	}

	internal static bool TryParseIndex<T>(this IList<T> col, string s, out int i, out T val)
	{
		val = default;

		if (Int32.TryParse(s, out i) && (i < col.Count && i >= 0)) {
			val = col[i];
			return true;
		}

		return false;
	}

	public static CapturedMultipartContent TrimQuotesFromContentTypeBoundary(this CapturedMultipartContent content)
	{
		var contentType = content.Headers.ContentType?.ToString();

		if (contentType == null) {
			return content;
		}

		content.Headers.Remove(HeaderNames.ContentType);
		var fixedContentType = new string(contentType.Where(static x => x != '\"').Select(static x => x).ToArray());
		content.Headers.TryAddWithoutValidation(HeaderNames.ContentType, fixedContentType);

		return content;
	}

	internal static readonly JsonSerializerOptions DefaultSerializerOptions = new()
	{
		Converters =
		{
			new UrlTypeConverter()
		},
		PropertyNameCaseInsensitive = true
	};

}