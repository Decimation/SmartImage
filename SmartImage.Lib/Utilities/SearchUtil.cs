using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Flurl.Http;
using Kantan.Text;
using Microsoft.Net.Http.Headers;
using SixLabors.ImageSharp;
using SmartImage.Lib.Engines;
using SmartImage.Lib.Engines.Results;
using SmartImage.Lib.Images;

namespace SmartImage.Lib.Utilities;

public static class SearchUtil
{

	/*public static bool IsSuccessful(this SearchResultStatus s)
		=> s is SearchResultStatus.Success || (!s.IsError() && !s.IsUnknown());*/

	public static bool IsSuccessful(this SearchResultStatus s)
		=> s is SearchResultStatus.Success;

	public static bool IsUnknown(this SearchResultStatus s)
		=> s is SearchResultStatus.None;

	public static bool IsError(this SearchResultStatus s)
		=> s is SearchResultStatus.UnknownError or SearchResultStatus.IllegalInput
			   or SearchResultStatus.Unavailable or SearchResultStatus.Cooldown;

	public const SearchResultFlags ALT_STATUS =
		SearchResultFlags.NoResults | SearchResultFlags.Extraneous;

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

	public static SizeS4N ParseResolution(string resText)
	{
		string[] resFull = resText.Split(Strings.Constants.MUL_SIGN);

		int? w = null, h = null;

		if (resFull.Length == 1 && resFull[0] == resText) {
			const string TIMES_DELIM = "&times;";

			if (resText.Contains(TIMES_DELIM)) {
				resFull = resText.Split(TIMES_DELIM);
			}
		}

		if (resFull.Length == 2) {
			w = int.Parse(resFull[0]);
			h = int.Parse(resFull[1]);
		}

		return new(w, h);
	}

	[CBN]
	public static T TryGetHeader<T>(this IFlurlResponse response, string name) where T : IParsable<T>
	{
		return response.Headers.TryGetFirst(name, out string cl) ? T.Parse(cl, CultureInfo.CurrentCulture) : default(T);
	}

	[CBN]
	public static long? TryGetContentLength(this IFlurlResponse response)
	{
		var cl = response.TryGetHeader<long>(HeaderNames.ContentLength);
		return cl == default ? null : cl;
	}

}