using Kantan.Model;
using Kantan.Model.MemberIndex;
using Kantan.Utilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SmartImage.Lib.Clients;
using SmartImage.Lib.Cookies;
using SmartImage.Lib.Engines;
using SmartImage.Lib.Engines.Search;
using SmartImage.Lib.Engines.Search.Other;
using SmartImage.Lib.Engines.Upload;
using SmartImage.Lib.Utilities;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json;
using SmartImage.Lib.Utilities.Integration;
using Configuration = System.Configuration.Configuration;
using ConfigurationManager = System.Configuration.ConfigurationManager;
using ConfigurationSection = System.Configuration.ConfigurationSection;

namespace SmartImage.Lib;

public sealed class SearchConfig : INotifyPropertyChanged
{

#region

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
	public const bool FLARESOLVERR_DEFAULT = false;

	public const string FLARE_SOLVERR_API_URL_DEFAULT = "http://localhost:8191";

	public const UploadEngineOptions UPLOAD_ENGINE_DEFAULT = UploadEngineOptions.Pomf;

#endregion

#region

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

#endregion

#region Cookies

	/// <summary>
	/// Parse browser cookies automatically whenever necessary
	/// </summary>
	/// <remarks>
	/// <see cref="ICookiesReceiver"/>
	/// <see cref="ICookiesSource"/>
	/// </remarks>
	public bool ReadCookies
	{
		get => Get(READCOOKIES_DEFAULT);
		set => Set(value);
	}

	public ICookiesSource GetCookiesSource()
	{
		if (ReadCookies) {
			return BrowserCookiesSource.Default.Value;
		}

		return ListCookiesSource.Default;
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

		if (FlareSolverr && !FlareSolverrClient.Value.IsInitialized) {

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
					FlareSolverr = ok;
					FlareSolverrClient.Value.Dispose();
				}
			}
		}

		return ok;
	}

#endregion

	public IEnumerable<BaseSearchEngine> GetSelectedEngines() => GetSelectedEngines(SearchEngines);

	public static IEnumerable<BaseSearchEngine> GetSelectedEngines(SearchEngineOptions options)
	{
		if (options.HasFlag(SearchEngineOptions.SauceNao))
			yield return new SauceNaoEngine();

		if (options.HasFlag(SearchEngineOptions.ImgOps))
			yield return new ImgOpsEngine();

		if (options.HasFlag(SearchEngineOptions.GoogleImages))
			yield return new GoogleImagesEngine();

		if (options.HasFlag(SearchEngineOptions.TinEye))
			yield return new TinEyeEngine();

		if (options.HasFlag(SearchEngineOptions.Iqdb))
			yield return new IqdbEngine();

		if (options.HasFlag(SearchEngineOptions.TraceMoe))
			yield return new TraceMoeEngine();

		if (options.HasFlag(SearchEngineOptions.KarmaDecay))
			yield return new KarmaDecayEngine();

		if (options.HasFlag(SearchEngineOptions.Yandex))
			yield return new YandexEngine();

		if (options.HasFlag(SearchEngineOptions.Bing))
			yield return new BingEngine();

		if (options.HasFlag(SearchEngineOptions.Ascii2D))
			yield return new Ascii2DEngine();

		if (options.HasFlag(SearchEngineOptions.RepostSleuth))
			yield return new RepostSleuthEngine();

		if (options.HasFlag(SearchEngineOptions.EHentai))
			yield return new EHentaiEngine();

		if (options.HasFlag(SearchEngineOptions.ArchiveMoe))
			yield return new ArchiveMoeEngine();

		if (options.HasFlag(SearchEngineOptions.Iqdb3D))
			yield return new Iqdb3DEngine();

		if (options.HasFlag(SearchEngineOptions.Fluffle))
			yield return new FluffleEngine();

		if (options.HasFlag(SearchEngineOptions.GoogleLens))
			yield return new GoogleLensEngine();
	}

#endregion

#region

	public static readonly Configuration Configuration =
		ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

	private bool Set<T>(T s = default, [CMN] string name = null)
	{
		bool b = Configuration.AddUpdateSetting(name, s.ToString());
		OnPropertyChanged(name);
		return b;
	}

	private T Get<T>(T t = default, [CMN] string name = null)
	{
		T v = Configuration.ReadSetting(name, t);
		return v;
	}

	public void Save()
	{
		Configuration.Save(ConfigurationSaveMode.Full, true);

		s_logger.LogTrace("Saved to {CfgPath}", Configuration.FilePath);
	}

#endregion

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


	private static readonly ILogger s_logger = AppSupport.Factory.CreateLogger(nameof(SearchConfig));

	public static readonly SearchConfig Default = new();


	public SearchConfig()
	{
		PropertyChanged += static (sender, args) =>
		{
			//
			s_logger.LogTrace("{Sender} Changed {PropName}", sender, args.PropertyName);
		};
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

	/*private IEnumerable<BaseSearchEngine> m_engines;

	public IEnumerable<BaseSearchEngine> Engines
	{
		get => m_engines;
		private set
		{
			if (Equals(value, m_engines))
				return;

			m_engines = value;
			OnPropertyChanged();
		}
	}*/

	public async ValueTask<bool> ApplyEnginesAsync(IEnumerable<BaseSearchEngine> engines,CancellationToken token = default)
	{
		s_logger.LogTrace("Loading engines");

		var loadFlareSolverr = TryLoadFlareSolverrAsync(token);
		await loadFlareSolverr;

		foreach (var engine in engines) {
			s_logger.LogTrace("Applying config to {Engine}", engine.Name);
			await engine.ApplyConfigAsync(this, token);

			if (engine is ICookiesReceiver ck) {
				s_logger.LogTrace("Applying cookies to {Engine}", engine.Name);
				await ck.ApplyCookiesAsync(GetCookiesSource(), token);
			}
		}

		// CookiesManager.Instance.Dispose();

		s_logger.LogDebug("Loaded engines");

		// ConfigApplied = true;

		return true;
	}

}