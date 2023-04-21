using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Kantan.Model;
using Kantan.Utilities;
using SmartImage.Lib.Engines;
using SmartImage.Lib.Engines.Impl.Search;

namespace SmartImage.Lib;

public sealed class SearchConfig : IDataTable, INotifyPropertyChanged
{
	#region Defaults

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

	#endregion

	/// <summary>
	/// Engines used to search.
	/// </summary>
	public SearchEngineOptions SearchEngines
	{
		get { return Configuration.ReadSetting(nameof(SearchEngines), SE_DEFAULT); }
		set
		{
			Configuration.AddUpdateSetting(nameof(SearchEngines), value.ToString());
			OnPropertyChanged();
		}
	}

	/// <summary>
	/// Engines whose results are opened in the default browser.
	/// </summary>
	public SearchEngineOptions PriorityEngines
	{
		get { return Configuration.ReadSetting(nameof(PriorityEngines), PE_DEFAULT); }
		set
		{
			Configuration.AddUpdateSetting(nameof(PriorityEngines), value.ToString());
			OnPropertyChanged();
		}
	}

	/// <summary>
	/// Keeps console window on-top.
	/// </summary>
	public bool OnTop
	{
		get { return Configuration.ReadSetting(nameof(OnTop), ON_TOP_DEFAULT); }
		set
		{
			Configuration.AddUpdateSetting(nameof(OnTop), value.ToString());
			OnPropertyChanged();
		}
	}

	/// <summary>
	/// <see cref="EHentaiEngine.Username"/>
	/// </summary>
	public string EhUsername
	{
		get { return Configuration.ReadSetting<string>(nameof(EhUsername)); }
		set
		{
			Configuration.AddUpdateSetting(nameof(EhUsername), value);
			OnPropertyChanged();
		}
	}

	/// <summary>
	/// <see cref="EHentaiEngine.Password"/>
	/// </summary>
	public string EhPassword
	{
		get { return Configuration.ReadSetting<string>(nameof(EhPassword)); }
		set
		{
			Configuration.AddUpdateSetting(nameof(EhPassword), value);
			OnPropertyChanged();
		}
	}

	public bool OpenRaw
	{
		get { return Configuration.ReadSetting(nameof(OpenRaw), false); }
		set
		{
			Configuration.AddUpdateSetting(nameof(OpenRaw), value.ToString());
			OnPropertyChanged();
		}
	}

	public static readonly SearchConfig Default = new();

	public SearchConfig()
	{
		PropertyChanged += (sender, args) =>
		{
			Trace.WriteLine($"{args.PropertyName}", nameof(SearchConfig));
		};
	}

	public static readonly Configuration Configuration =
		ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

	public void Save()
	{
		Configuration.Save(ConfigurationSaveMode.Full, true);

		Debug.WriteLine($"Saved to {Configuration.FilePath}", nameof(Save));
	}

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
		table.Rows.Add(Resources.S_OpenRaw, OpenRaw);
		table.Rows.Add(Resources.S_EhUsername, EhUsername);
		table.Rows.Add(Resources.S_EhPassword, EhPassword);

		// table.Rows.Add("Path", new FileInfo(Configuration.FilePath).Name);

		return table;
	}

	public event PropertyChangedEventHandler PropertyChanged;

	private void OnPropertyChanged([CallerMemberName] string propertyName = null)
	{
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}

	private bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
	{
		if (EqualityComparer<T>.Default.Equals(field, value)) return false;
		field = value;
		OnPropertyChanged(propertyName);
		return true;
	}

	public override string ToString()
	{
		return $"{SearchEngines}\n{PriorityEngines}";
	}
}