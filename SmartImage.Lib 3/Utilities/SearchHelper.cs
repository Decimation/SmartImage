// Read S SmartImage.Lib Extensions.cs
// 2023-07-23 @ 4:29 PM

using SmartImage.Lib.Results;

namespace SmartImage.Lib.Utilities;

public static class SearchHelper
{
	public static bool IsSuccessful(this SearchResultStatus s)
	{
		return !s.IsError();
	}

	public static bool IsUnknown(this SearchResultStatus s)
	{
		return s is SearchResultStatus.NoResults or SearchResultStatus.None;
	}

	public static bool IsError(this SearchResultStatus s)
	{
		return s is SearchResultStatus.Failure or SearchResultStatus.IllegalInput 
			       or SearchResultStatus.Unavailable or SearchResultStatus.Cooldown;
	}
}