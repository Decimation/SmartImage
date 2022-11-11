using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Resources;
using Kantan.Model;
using Kantan.Utilities;
using SmartImage.Lib.Engines;

namespace SmartImage.Lib;

public sealed class SearchConfig : IDataTable
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
		get => Configuration.ReadSetting(nameof(SearchEngines), SE_DEFAULT);
		set => Configuration.AddUpdateSetting(nameof(SearchEngines), value.ToString());
	}

	/// <summary>
	/// Engines whose results are opened in the default browser.
	/// </summary>
	public SearchEngineOptions PriorityEngines
	{
		get => Configuration.ReadSetting(nameof(PriorityEngines), PE_DEFAULT);
		set => Configuration.AddUpdateSetting(nameof(PriorityEngines), value.ToString());
	}

	/// <summary>
	/// Keeps console window on-top.
	/// </summary>
	public bool OnTop
	{
		get => Configuration.ReadSetting(nameof(OnTop), ON_TOP_DEFAULT);
		set => Configuration.AddUpdateSetting(nameof(OnTop), value.ToString());
	}

	public static readonly Configuration Configuration =
		ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

	public void Save()
	{
		Configuration.Save(ConfigurationSaveMode.Full, true);

		Debug.WriteLine($"Saved to {Configuration.FilePath}", nameof(Save));
	}

	#region Implementation of IDataTable

	public DataTable ToTable()
	{
		var table = new DataTable("Configuration");

		table.Columns.AddRange(new DataColumn[]
		{
			new("Setting", typeof(string)),
			new("Value", typeof(object)),
		});

		table.Rows.Add(Resources.S_SearchEngines, SearchEngines);
		table.Rows.Add(Resources.S_PriorityEngines, PriorityEngines);
		table.Rows.Add(Resources.S_OnTop, OnTop);

		// table.Rows.Add("Path", new FileInfo(Configuration.FilePath).Name);

		return table;
	}

	#endregion
}