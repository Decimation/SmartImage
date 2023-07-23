// Read S SmartImage.Lib Extensions.cs
// 2023-07-23 @ 4:29 PM

using SmartImage.Lib.Results;

namespace SmartImage.Lib.Utilities;

public static class SearchHelper
{
	public static bool IsError(this SearchResultStatus s)
	{
		return s is not (SearchResultStatus.Cooldown
			       or SearchResultStatus.Extraneous
			       or SearchResultStatus.Failure
			       or SearchResultStatus.IllegalInput
			       or SearchResultStatus.Unavailable);
	}
}