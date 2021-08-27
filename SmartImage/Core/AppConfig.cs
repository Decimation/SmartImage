using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Kantan.Collections;
using Kantan.Utilities;
using SmartImage.Lib.Engines;
using static Kantan.Diagnostics.LogCategories;

namespace SmartImage.Core
{
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
					{K_ENGINES, Program.Config.SearchEngines.ToString()},
					{K_PRIORITY_ENGINES, Program.Config.PriorityEngines.ToString()},
					{K_FILTER, Program.Config.Filtering.ToString()},
					{K_NOTIFICATION, Program.Config.Notification.ToString()},
					{K_NOTIFICATION_IMAGE, Program.Config.NotificationImage.ToString()},
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

			Program.Config.SearchEngines     = Enum.Parse<SearchEngineOptions>(map[K_ENGINES]);
			Program.Config.PriorityEngines   = Enum.Parse<SearchEngineOptions>(map[K_PRIORITY_ENGINES]);
			Program.Config.Filtering         = Boolean.Parse(map[K_FILTER]);
			Program.Config.Notification      = Boolean.Parse(map[K_NOTIFICATION]);
			Program.Config.NotificationImage = Boolean.Parse(map[K_NOTIFICATION_IMAGE]);

			SaveConfigFile();

			Program.Client.Reload();

			Debug.WriteLine($"Updated config from {ConfigFile.Name}", C_INFO);
		}

		private static void SaveConfigFile()
		{
			var map = ConfigMap;

			EnumerableHelper.WriteDictionary(map, ConfigFile.ToString());

			Debug.WriteLine($"Saved to {ConfigFile.Name}", C_INFO);
		}

		private const string K_ENGINES            = "engines";
		private const string K_PRIORITY_ENGINES   = "priority-engines";
		private const string K_FILTER             = "filter";
		private const string K_NOTIFICATION       = "notification";
		private const string K_NOTIFICATION_IMAGE = "notification-image";

		public static void UpdateConfig()
		{
			Program.Client.Reload();
			SaveConfigFile();
		}
	}
}
