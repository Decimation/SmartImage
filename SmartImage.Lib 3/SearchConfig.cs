using System.Configuration;
using System.Diagnostics;

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
	public SearchEngineOptions SearchEngines
	{
		get => ReadSetting(nameof(SearchEngines), SE_DEFAULT);
		set => AddUpdateAppSettings(nameof(SearchEngines), value.ToString());
	}

	/// <summary>
	/// Engines whose results are opened in the default browser.
	/// </summary>
	public SearchEngineOptions PriorityEngines
	{
		get => ReadSetting(nameof(PriorityEngines), PE_DEFAULT);
		set => AddUpdateAppSettings(nameof(PriorityEngines), value.ToString());
	}

	/// <summary>
	/// Keeps console window on-top.
	/// </summary>
	public bool OnTop
	{
		get => ReadSetting(nameof(OnTop), ON_TOP_DEFAULT);
		set => AddUpdateAppSettings(nameof(OnTop), value.ToString());
	}

	public static readonly Configuration Configuration =
		ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

	public void Save()
	{
		Configuration.Save(ConfigurationSaveMode.Full, true);

		Debug.WriteLine($"{Configuration.FilePath}");
	}

	[CBN]
	private static T ReadSetting<T>(string key, [CBN] T def)
	{
		try {
			def ??= default(T);

			var appSettings = Configuration.AppSettings.Settings;
			var result      = appSettings[key] ?? null;

			if (result == null) {
				AddUpdateAppSettings(key, def.ToString());
				result = appSettings[key];
			}

			var value = result.Value;

			var type = typeof(T);

			if (type.IsEnum) {
				return (T) Enum.Parse(type, value);
			}

			if (type == typeof(bool)) {
				return (T) (object) bool.Parse(value);
			}

			return (T) (object) value;
		}
		catch (ConfigurationErrorsException) {
			return default;
		}
	}

	private static void AddUpdateAppSettings(string key, string value)
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

	}

	public override string ToString()
	{
		return $"{nameof(SearchEngines)}: {SearchEngines}, \n" +
		       $"{nameof(PriorityEngines)}: {PriorityEngines}";
	}
}