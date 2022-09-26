using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using ConfigurationManager = System.Configuration.ConfigurationManager;
using ConfigurationSection = System.Configuration.ConfigurationSection;

namespace SmartImage.Lib;

public sealed class SearchConfig : ConfigurationSection
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
		get => Enum.Parse<SearchEngineOptions>(this[nameof(SearchEngines)].ToString());
		set => this[nameof(SearchEngines)] = value;
	}

	/// <summary>
	/// Engines whose results are opened in the default browser.
	/// </summary>
	[ConfigurationProperty(nameof(PriorityEngines), DefaultValue = PE_DEFAULT)]
	public SearchEngineOptions PriorityEngines
	{
		get => Enum.Parse<SearchEngineOptions>(this[nameof(PriorityEngines)].ToString());
		set => this[nameof(PriorityEngines)] = value;
	}

	/// <summary>
	/// Keeps console window on-top.
	/// </summary>
	[ConfigurationProperty(nameof(OnTop), DefaultValue = ON_TOP_DEFAULT)]
	public bool OnTop
	{
		get =>(bool) this[nameof(OnTop)];
		set => this[nameof(OnTop)] = value;

	}

	public SearchConfig()
	{
		var c=Configuration.Sections["Config"];

		if (c == null) {
			Configuration.Sections.Add("Config", this);
		}

		this.SectionInformation.ForceSave = true;
		
	}

	public static readonly Configuration Configuration =
		ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

	public void Save()
	{
		Configuration.Save(ConfigurationSaveMode.Modified);
		ConfigurationManager.RefreshSection(Configuration.AppSettings.SectionInformation.Name);
	}

	public override string ToString()
	{
		return $"{nameof(SearchEngines)}: {SearchEngines}, \n" +
		       $"{nameof(PriorityEngines)}: {PriorityEngines}";
	}
}