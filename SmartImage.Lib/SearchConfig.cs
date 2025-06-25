using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Kantan.Model;
using Kantan.Model.MemberIndex;
using Kantan.Utilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SmartImage.Lib.Clients;
using SmartImage.Lib.Cookies;
using SmartImage.Lib.Engines;
using SmartImage.Lib.Engines.Impl.Search;
using SmartImage.Lib.Engines.Impl.Upload;
using SmartImage.Lib.Results.Data;
using SmartImage.Lib.Utilities.Diagnostics;
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

	/// <summary>
	/// Default value for <see cref="FlareSolverr"/>
	/// </summary>
	public const bool FLARESOLVERR_DEFAULT = true;

	public const string FLARE_SOLVERR_API_URL_DEFAULT = "http://localhost:8191";

	public const UploadEngineOptions UPLOAD_ENGINE_DEFAULT = UploadEngineOptions.Pomf;

#endregion

	/// <summary>
	/// Engines used to search.
	/// </summary>
	public SearchEngineOptions SearchEngines
	{
		get => Get(SE_DEFAULT);
		set => Set(value);
	}

	/// <summary>
	/// Engines whose results are opened in the default browser.
	/// </summary>
	public SearchEngineOptions PriorityEngines
	{
		get => Get(PE_DEFAULT);
		set => Set(value);
	}

	/// <summary>
	/// Keeps console window on-top.
	/// </summary>
	public bool OnTop
	{
		get => Get(ON_TOP_DEFAULT);
		set => Set(value);
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
		get => Get(false);
		set => Set(value);
	}

	/// <summary>
	/// Obsolete
	/// </summary>
	public bool Silent
	{
		get => Get(false);
		set => Set(value);
	}

	public bool Clipboard
	{
		get => Get(true);
		set => Set(value);
	}

	public bool AutoSearch
	{
		get => Get(false);
		set => Set(value);
	}

	/// <summary>
	/// <see cref="SauceNaoEngine.Authentication"/>
	/// </summary>
	public string SauceNaoKey
	{
		get => Get(String.Empty);
		set => Set(value);
	}

	#region Cookies 

	/// <summary>
	/// Parse browser cookies automatically whenever necessary
	/// </summary>
	/// <remarks>
	/// <see cref="ICookiesReceiver"/>
	/// <see cref="ICookiesProvider"/>
	/// </remarks>
	public bool ReadCookies
	{
		get => Get(READCOOKIES_DEFAULT);
		set => Set(value);
	}


	// TODO: cookies.txt support
	// TODO: specify cookies source

	private ICookiesProvider m_cookiesProvider;

	public ICookiesProvider CookiesProvider
	{
		get
		{
			if (ReadCookies && m_cookiesProvider == null) {
				m_cookiesProvider = ICookiesProvider.GetProvider();
			}

			return m_cookiesProvider;
		}
		set { m_cookiesProvider = value; }
	}

	#endregion

	#region FlareSolverr 

	/// <remarks>
	/// 
	/// </remarks>
	public bool FlareSolverr
	{
		get => Get(FLARESOLVERR_DEFAULT);
		set => Set(value);
	}


	/// <remarks>
	/// 
	/// </remarks>
	public string FlareSolverrApiUrl
	{
		get => Get(FLARE_SOLVERR_API_URL_DEFAULT);
		set => Set(value);
	}

	internal async ValueTask<bool> TryLoadFlareSolverrAsync(CancellationToken token)
	{
		bool ok = false;

		if (this.FlareSolverr && !FlareSolverrClient.Value.IsInitialized) {

			ok = await FlareSolverrClient.Value.ApplyConfigAsync(this, token);

			if (!ok) {
				Debugger.Break();
			}
			else {
				// Ensure FlareSolverr

				try {
					var idx = await FlareSolverrClient.Value.Clearance.Solverr.GetIndexAsync();
				}
				catch (Exception e) {
					s_logger.LogError(e, "FlareSolverr error");
					this.FlareSolverr = ok;
					FlareSolverrClient.Value.Dispose();
				}
			}
		}

		return ok;
	}

	#endregion

	/// <summary>
	/// <see cref="BaseUploadEngine"/>
	/// </summary>
	public UploadEngineOptions UploadEngine
	{
		get => Get(UPLOAD_ENGINE_DEFAULT);
		set => Set(value);
	}

	public static readonly SearchConfig Default = new();

	private static readonly ILogger s_logger = AppSupport.Factory.CreateLogger(nameof(SearchConfig));

	public static readonly Configuration Configuration =
		ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

	public SearchConfig()
	{
		PropertyChanged += static (sender, args) =>
		{
			//
			s_logger.LogTrace("Changed {PropName}", args.PropertyName);
		};
	}


	private bool Set<T>(T s = default, [CMN] string name = default)
	{
		bool b = Configuration.AddUpdateSetting(name, s.ToString());
		OnPropertyChanged(name);
		return b;
	}

	private T Get<T>(T t = default, [CMN] string name = default)
	{
		T v = Configuration.ReadSetting(name, t);
		return v;
	}

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

	private void OnPropertyChanged([CMN] string propertyName = null)
	{
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}

	public override string ToString()
	{
		return $"{SearchEngines}\n{PriorityEngines}";
	}

}