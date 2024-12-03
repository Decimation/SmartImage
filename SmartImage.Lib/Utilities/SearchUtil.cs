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
		=> (!s.IsError() && !s.IsUnknown()) || s is SearchResultStatus.Success;

	public static bool IsUnknown(this SearchResultStatus s)
		=> s is SearchResultStatus.NoResults or SearchResultStatus.None;

	public static bool IsError(this SearchResultStatus s)
		=> s is SearchResultStatus.Failure or SearchResultStatus.IllegalInput
			   or SearchResultStatus.Unavailable or SearchResultStatus.Cooldown;

}