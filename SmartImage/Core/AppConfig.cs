using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using Kantan.Collections;
using Kantan.Utilities;
using SmartImage.Lib;
using SmartImage.Lib.Engines;
using SmartImage.Lib.Searching;
using SmartImage.Properties;
using static Kantan.Diagnostics.LogCategories;

namespace SmartImage.Core;

public static class AppConfig
{

	public static FileInfo ConfigFile
	{
		get
		{
			string file = Path.Combine(AppInfo.AppFolder, AppInfo.NAME_CFG);

			if (!File.Exists(file)) {
				var f = File.Create(file);
				f.Close();
			}

			return new FileInfo(file);

		}
	}

	public static Dictionary<string, string> ConfigMap
	{
		get
		{
			var map = new Dictionary<string, string>()
			{
				{ Resources.K_Engines, Program.Config.SearchEngines.ToString() },
				{ Resources.K_PriorityEngines, Program.Config.PriorityEngines.ToString() },
				{ Resources.K_Filter, Program.Config.Filtering.ToString() },
				{ Resources.K_Notification, Program.Config.Notification.ToString() },
				{ Resources.K_NotificationImage, Program.Config.NotificationImage.ToString() },
				{ Resources.K_OutputOnly, Program.Config.OutputOnly.ToString() },

			};
			return map;
		}
	}

	public static void ReadConfigFile()
	{

		var map = EnumerableHelper.ReadDictionary(ConfigFile.ToString());

		foreach (var (key, value) in ConfigMap) {
			if (!map.ContainsKey(key)) {
				map.Add(key, value);
			}
		}

		Program.Config.SearchEngines     = Enum.Parse<SearchEngineOptions>(map[Resources.K_Engines]);
		Program.Config.PriorityEngines   = Enum.Parse<SearchEngineOptions>(map[Resources.K_PriorityEngines]);
		Program.Config.Filtering         = Boolean.Parse(map[Resources.K_Filter]);
		Program.Config.Notification      = Boolean.Parse(map[Resources.K_Notification]);
		Program.Config.NotificationImage = Boolean.Parse(map[Resources.K_NotificationImage]);
		Program.Config.OutputOnly        = Boolean.Parse(map[Resources.K_OutputOnly]);

		SaveConfigFile();

		Program.Reload();

		Debug.WriteLine($"Updated config from {ConfigFile.Name}", C_INFO);
	}

	private static void SaveConfigFile()
	{
		var map = ConfigMap;

		EnumerableHelper.WriteDictionary(map, ConfigFile.ToString());

		Debug.WriteLine($"Saved to {ConfigFile.Name}", C_INFO);

	}


	public static void UpdateConfig()
	{
		Program.Reload();
		SaveConfigFile();
	}
	
}