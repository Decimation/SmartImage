using System.Net.Http.Headers;
using System.Text.Json;
using Flurl.Http.Content;
using Kantan.Net.Utilities;
using Microsoft.Net.Http.Headers;

namespace SmartImage.Lib.Utilities;

public static class SearchUtil
{

	internal static bool TryParseIndex<T>(this IList<T> col, string s, out int i, out T val)
	{
		val = default;

		if (Int32.TryParse(s, out i) && (i < col.Count && i >= 0)) {
			val = col[i];
			return true;
		}

		return false;
	}

	extension(CapturedMultipartContent content)
	{

		public CapturedMultipartContent TrimQuotesFromContentTypeBoundary()
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