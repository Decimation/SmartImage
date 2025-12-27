using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Flurl.Http;
using Kantan.Net.Utilities;
using Kantan.Text;
using Microsoft.Net.Http.Headers;
using SixLabors.ImageSharp;
using SmartImage.Lib.Engines;
using SmartImage.Lib.Engines.Results;
using SmartImage.Lib.Images;

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

	public const SearchResultFlags ALT_STATUS =
		SearchResultFlags.NoResults | SearchResultFlags.Extraneous;

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
		public T TryGetHeader<T>(string name) where T : IParsable<T>
		{
			return response.Headers.TryGetFirst(name, out string cl) ? T.Parse(cl, CultureInfo.CurrentCulture) : default(T);
		}

		[CBN]
		public long? TryGetContentLength()
		{
			var cl = response.TryGetHeader<long>(HeaderNames.ContentLength);
			return cl == default ? null : cl;
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