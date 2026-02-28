using System.Globalization;
using System.Net.Http.Headers;
using System.Text.Json;
using Flurl.Http;
using Flurl.Http.Content;
using Kantan.Net.Utilities;
using Microsoft.Net.Http.Headers;
using SmartImage.Lib.Engines.Results;

namespace SmartImage.Lib.Utilities;

internal static class SearchUtil
{

	/*public static bool IsSuccessful(this SearchResultStatus s)
		=> s is SearchResultStatus.Success || (!s.IsError() && !s.IsUnknown());*/

	extension(SearchResultStatus s)
	{

		public bool IsSuccessful()
			=> s is SearchResultStatus.Success;

		public bool IsUnknown()
			=> s is SearchResultStatus.None;

		public bool IsError()
			=> s is SearchResultStatus.UnknownError or SearchResultStatus.IllegalInput
				   or SearchResultStatus.Unavailable or SearchResultStatus.Cooldown;

	}

	public static bool HasFlagFast(this SearchResultFlags value, SearchResultFlags status)
		=> (value & status) != 0;

	internal static bool TryParseIndex<T>(this IList<T> col, string s, out int i, out T val)
	{
		val = default;

		if (Int32.TryParse(s, out i) && (i < col.Count && i >= 0)) {
			val = col[i];
			return true;
		}

		return false;
	}

	extension(IFlurlResponse response)
	{

		[CBN]
		public bool TryParseHeader<T>(string name, out T t) where T : IParsable<T>
		{
			t = default;

			if (response.Headers.TryGetFirst(name, out string cl)) {
				t = T.Parse(cl, CultureInfo.CurrentCulture);
				return true;
			}

			return false;
		}

		[CBN]
		public long? TryGetContentLength()
		{
			var cl = response.TryParseHeader<long>(HeaderNames.ContentLength, out var l);
			return cl ? null : l;
		}

	}

	/*public static CapturedMultipartContent TrimQuotesFromContentTypeBoundary(this CapturedMultipartContent content)
	{
		return content;
	}

	public static HttpContentHeaders TrimQuotesFromContentTypeBoundary(this HttpContentHeaders headers)
	{
		if (headers.ContentType is { } ct) {
			headers.Remove(HeaderNames.ContentType);
			var ctStr = ct.ToString();

			// var fixedContentType = new string(ctStr.Where(static x => x != '\"').Select(static x => x).ToArray());
			headers.TryAddWithoutValidation(HeaderNames.ContentType, ctStr.Trim('\"'));
		}

		// content.Headers.TryAddWithoutValidation("Content-Type", fixedContentType);
		return headers;
	}*/

	public static CapturedMultipartContent TrimQuotesFromContentTypeBoundary(this CapturedMultipartContent content)
	{
		var contentType = content.Headers.ContentType.ToString();
		content.Headers.Remove(HeaderNames.ContentType);
		var fixedContentType = new string(contentType.Where(x => x != '\"').Select(x => x).ToArray());
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