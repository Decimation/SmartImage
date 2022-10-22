using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using ConfigurationManager = System.Configuration.ConfigurationManager;
using ConfigurationSection = System.Configuration.ConfigurationSection;

namespace SmartImage.Lib;

public sealed class SearchConfig
{
	/// <summary>
	/// Default value for <see cref="SearchEngines"/>
	/// </summary>
	public const SearchEngineOptions SE_DEFAULT = SearchEngineOptions.All;

	/// <summary>
	/// Default value for <see cref="PriorityEngines"/>
	/// </summary>
	public const SearchEngineOptions PE_DEFAULT = SearchEngineOptions.Auto;

	/// <summary>
	/// Default value for <see cref="OnTop"/>
	/// </summary>
	public const bool ON_TOP_DEFAULT = true;

	/// <summary>
	/// Engines used to search.
	/// </summary>
	[ConfigurationProperty(nameof(SearchEngines), DefaultValue = SE_DEFAULT)]
	public SearchEngineOptions SearchEngines
	{
		get => ReadSetting<SearchEngineOptions>(nameof(SearchEngines), SE_DEFAULT);
		set => AddUpdateAppSettings(nameof(SearchEngines), value.ToString());
	}

	/// <summary>
	/// Engines whose results are opened in the default browser.
	/// </summary>
	[ConfigurationProperty(nameof(PriorityEngines), DefaultValue = PE_DEFAULT)]
	public SearchEngineOptions PriorityEngines
	{
		get => ReadSetting<SearchEngineOptions>(nameof(PriorityEngines), PE_DEFAULT);
		set => AddUpdateAppSettings(nameof(PriorityEngines), value.ToString());
	}

	/// <summary>
	/// Keeps console window on-top.
	/// </summary>
	[ConfigurationProperty(nameof(OnTop), DefaultValue = ON_TOP_DEFAULT)]
	public bool OnTop
	{
		get => ReadSetting<bool>(nameof(OnTop), ON_TOP_DEFAULT);
		set => AddUpdateAppSettings(nameof(OnTop), value.ToString());
	}

	public SearchConfig() { }

	public static readonly Configuration Configuration =
		ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

	public void Save()
	{
		Configuration.Save(ConfigurationSaveMode.Full, true);
	}

	[CBN]
	static T ReadSetting<T>(string key, [CBN] T def)
	{
		try {
			var appSettings = Configuration.AppSettings.Settings;
			var result      = appSettings[key] ?? null;

			if (result  == null) {
				AddUpdateAppSettings(key, def);
				result = appSettings[key];
			}

			var value = result.Value;

			var type = typeof(T);

			if (type.IsEnum) {
				return (T) Enum.Parse(type, value);
			}
			else if (type == typeof(bool)) {
				return (T) (object) bool.Parse(value);
			}

			return (T) (object) value;
		}
		catch (ConfigurationErrorsException) {
			return default;
		}
	}

	static void AddUpdateAppSettings(string key, string value)
	{
		try {
			var settings = Configuration.AppSettings.Settings;

			if (settings[key] == null) {
				settings.Add(key, value);
			}
			else {
				settings[key].Value = value;
			}

			Configuration.Save(ConfigurationSaveMode.Modified);
			ConfigurationManager.RefreshSection(Configuration.AppSettings.SectionInformation.Name);
		}
		catch (ConfigurationErrorsException) {
			Debug.WriteLine("Error writing app settings");
		}

		Debug.WriteLine($"{Configuration.FilePath}");
	}

	public override string ToString()
	{
		return $"{nameof(SearchEngines)}: {SearchEngines}, \n" +
		       $"{nameof(PriorityEngines)}: {PriorityEngines}";
	}
}