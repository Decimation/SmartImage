using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmartImage.Lib.Results;

namespace SmartImage.Lib.Utilities;

public static class SearchUtil
{

	public static bool IsSuccessful(this SearchResultStatus s)
		=> s is SearchResultStatus.Success || (!s.IsError() && !s.IsUnknown());

	public static bool IsUnknown(this SearchResultStatus s)
		=> s is SearchResultStatus.None;

	public static bool IsError(this SearchResultStatus s)
		=> s is SearchResultStatus.UnknownError or SearchResultStatus.IllegalInput
			   or SearchResultStatus.Unavailable or SearchResultStatus.Cooldown;

	public const SearchResultFlags ALT_STATUS =
		SearchResultFlags.NoResults | SearchResultFlags.Extraneous;

	public static bool HasFlagFast(this SearchResultFlags value, SearchResultFlags status)
		=> (value & status) != 0;

}