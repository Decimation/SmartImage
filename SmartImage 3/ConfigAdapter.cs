using Novus.Win32;
using SmartImage.Lib;
using Spectre.Console;

namespace SmartImage;

public static class ConfigAdapter
{
	public static void RootHandler(SearchEngineOptions t2, SearchEngineOptions t3, bool t4)
	{

		Program.Config.SearchEngines   = t2;
		Program.Config.PriorityEngines = t3;

		Program.Config.OnTop = t4;

		if (Program.Config.OnTop) {
			Native.KeepWindowOnTop(Cache.HndWindow);
		}
	}
}