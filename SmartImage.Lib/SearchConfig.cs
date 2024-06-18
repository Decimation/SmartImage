using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Kantan.Model;
using Kantan.Model.MemberIndex;
using Kantan.Utilities;
using Microsoft.Extensions.Configuration;
using SmartImage.Lib.Engines;
using SmartImage.Lib.Engines.Impl.Search;
using SmartImage.Lib.Model;
using Configuration = System.Configuration.Configuration;
using ConfigurationManager = System.Configuration.ConfigurationManager;
using ConfigurationSection = System.Configuration.ConfigurationSection;

namespace SmartImage.Lib;

public sealed class SearchConfig : INotifyPropertyChanged
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

	/// <summary>
	/// Default value for <see cref="AutoSearch"/>
	/// </summary>
	public const bool AUTOSEARCH_DEFAULT = true;

	/// <summary>
	/// Default value for <see cref="ReadCookies"/>
	/// </summary>
	public const bool READCOOKIES_DEFAULT = false;

	private static readonly string STR_DEFAULT = String.Empty;

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

	/*
	/// <summary>
	/// <see cref="HydrusClient.EndpointUrl"/>
	/// </summary>
	public string HydrusEndpoint
	{
		get { return Configuration.ReadSetting(nameof(HydrusEndpoint), STR_DEFAULT); }
		set
		{
			Configuration.AddUpdateSetting(nameof(HydrusEndpoint), value);
			OnPropertyChanged();
		}
	}

	/// <summary>
	/// <see cref="HydrusClient.Key"/>
	/// </summary>
	public string HydrusKey
	{
		get { return Configuration.ReadSetting(nameof(HydrusKey), STR_DEFAULT); }
		set
		{
			Configuration.AddUpdateSetting(nameof(HydrusKey), value);
			OnPropertyChanged();
		}
	}
	*/

	public bool OpenRaw
	{
		get { return Configuration.ReadSetting(nameof(OpenRaw), false); }
		set
		{
			Configuration.AddUpdateSetting(nameof(OpenRaw), value.ToString());
			OnPropertyChanged();
		}
	}

	public bool Silent
	{
		get { return Configuration.ReadSetting(nameof(Silent), false); }
		set
		{
			Configuration.AddUpdateSetting(nameof(Silent), value.ToString());
			OnPropertyChanged();
		}
	}

	public bool Clipboard
	{
		get { return Configuration.ReadSetting(nameof(Clipboard), true); }
		set
		{
			Configuration.AddUpdateSetting(nameof(Clipboard), value.ToString());
			OnPropertyChanged();
		}
	}

	public bool AutoSearch
	{
		get { return Configuration.ReadSetting(nameof(AutoSearch), false); }
		set
		{
			Configuration.AddUpdateSetting(nameof(AutoSearch), value.ToString());
			OnPropertyChanged();
		}
	}

	/// <summary>
	/// <see cref="SauceNaoEngine.Authentication"/>
	/// </summary>
	public string SauceNaoKey
	{
		get { return Configuration.ReadSetting(nameof(SauceNaoKey), STR_DEFAULT); }
		set
		{
			Configuration.AddUpdateSetting(nameof(SauceNaoKey), value);
			OnPropertyChanged();
		}
	}

	/// <summary>
	/// Parse browser cookies automatically whenever necessary
	/// </summary>
	/// <remarks>
	/// <see cref="ICookieEngine"/>
	/// </remarks>
	public bool ReadCookies
	{
		get { return Configuration.ReadSetting(nameof(ReadCookies), READCOOKIES_DEFAULT); }
		set
		{
			Configuration.AddUpdateSetting(nameof(ReadCookies), value.ToString());
			OnPropertyChanged();
		}
	}

	/// <summary>
	/// 
	/// </summary>
	// todo
	public string CookiesFile
	{
		get { return Configuration.ReadSetting(nameof(CookiesFile), STR_DEFAULT); }
		set
		{
			Configuration.AddUpdateSetting(nameof(CookiesFile), value);
			OnPropertyChanged();
		}
	}

	public static readonly SearchConfig Default = new();

	public SearchConfig()
	{
		PropertyChanged += (sender, args) =>
		{
			Trace.WriteLine($"Changed {args.PropertyName}", nameof(SearchConfig));
		};
	}

	public static readonly Configuration Configuration =
		ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

	public void Save()
	{
		Configuration.Save(ConfigurationSaveMode.Full, true);

		Debug.WriteLine($"Saved to {Configuration.FilePath}", nameof(Save));
	}

	/*public DataTable ToTable()
	{
		var table = new DataTable("Configuration");

		table.Columns.AddRange([
			new("Setting", typeof(string)),
			new("Value", typeof(object))
		]);

		table.Rows.Add(Resources.S_SearchEngines, SearchEngines);
		table.Rows.Add(Resources.S_PriorityEngines, PriorityEngines);
		table.Rows.Add(Resources.S_OnTop, OnTop);
		table.Rows.Add(Resources.S_OpenRaw, OpenRaw);
		table.Rows.Add(Resources.S_Silent, Silent);
		table.Rows.Add(Resources.S_EhUsername, EhUsername);
		table.Rows.Add(Resources.S_EhPassword, EhPassword);
		table.Rows.Add(Resources.S_Clipboard, Clipboard);
		table.Rows.Add(Resources.S_AutoSearch, AutoSearch);
		table.Rows.Add(Resources.S_SauceNaoKey, SauceNaoKey);
		/*table.Rows.Add(Resources.S_HydrusEndpoint, HydrusEndpoint);
		table.Rows.Add(Resources.S_HydrusKey, HydrusKey);#1#

		// table.Rows.Add("Path", new FileInfo(Configuration.FilePath).Name);

		return table;
	}*/

	public event PropertyChangedEventHandler PropertyChanged;

	private void OnPropertyChanged([CallerMemberName] string propertyName = null)
	{
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}

	public override string ToString()
	{
		return $"{SearchEngines}\n{PriorityEngines}";
	}

}