using SmartImage.Lib.Searching;

namespace SmartImage_3;

public static partial class Gui
{
	public static class Constants
	{
		public static readonly string[] EngineNames = Enum.GetNames<SearchEngineOptions>();

		public const string SYM_NA      = "-";
		public const string SYM_ERR     = "!";
		public const string SYM_OK      = "*";
		public const string SYM_PROCESS = "^";
		public const string NAME        = "SmartImage";
	}
}