using Kantan.Utilities;
using Microsoft.Extensions.Logging;
using SmartImage.Lib.Clients;
using SmartImage.Lib.Cookies;
using SmartImage.Lib.Engines.Results;
using SmartImage.Lib.Engines.Search;
using SmartImage.Lib.Engines.Search.Base;
using SmartImage.Lib.Engines.Search.Other;
using SmartImage.Lib.Engines.Upload;
using SmartImage.Lib.Engines.Upload.Base;
using SmartImage.Lib.Utilities;
using System.ComponentModel;
using System.Configuration;
using Configuration = System.Configuration.Configuration;
using ConfigurationManager = System.Configuration.ConfigurationManager;

namespace SmartImage.Lib;

public sealed class SearchConfig : INotifyPropertyChanged
{

	private static readonly ILogger s_logger = AppSupport.Factory.CreateLogger(nameof(SearchConfig));

	public static readonly SearchConfig Default = new();

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

	/// <summary>
	/// Default value for <see cref="UploadEngine"/>
	/// </summary>
	public const UploadEngineOptions UE_DEFAULT = UploadEngineOptions.TmpFiles;

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
	/// Upload engine
	/// </summary>
	public UploadEngineOptions UploadEngine
	{
		get => Get(UE_DEFAULT);
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
		get => Get(FlareSolverrClient.FLARE_SOLVERR_API_URL_DEFAULT);
		set => Set(value);
	}

#endregion

	
	/*public IEnumerable<BaseSearchEngine> GetSelectedEngines()
	{
		// TODO
		if (SearchEngines.HasFlag(SearchEngineOptions.SauceNao))
			yield return new SauceNaoEngine(authentication: SauceNaoKey);

		if (SearchEngines.HasFlag(SearchEngineOptions.ImgOps))
			yield return new ImgOpsEngine();

		if (SearchEngines.HasFlag(SearchEngineOptions.GoogleImages))
			yield return new GoogleImagesEngine();

		if (SearchEngines.HasFlag(SearchEngineOptions.TinEye))
			yield return new TinEyeEngine();

		if (SearchEngines.HasFlag(SearchEngineOptions.Iqdb))
			yield return new IqdbEngine();

		if (SearchEngines.HasFlag(SearchEngineOptions.TraceMoe))
			yield return new TraceMoeEngine();

		if (SearchEngines.HasFlag(SearchEngineOptions.KarmaDecay))
			yield return new KarmaDecayEngine();

		if (SearchEngines.HasFlag(SearchEngineOptions.Yandex))
			yield return new YandexEngine();

		if (SearchEngines.HasFlag(SearchEngineOptions.Bing))
			yield return new BingEngine();

		if (SearchEngines.HasFlag(SearchEngineOptions.Ascii2D))
			yield return new Ascii2DEngine(GetCookiesSource());

		if (SearchEngines.HasFlag(SearchEngineOptions.RepostSleuth))
			yield return new RepostSleuthEngine();

		if (SearchEngines.HasFlag(SearchEngineOptions.EHentai))
			yield return new EHentaiEngine(cookiesSrc: GetCookiesSource());

		if (SearchEngines.HasFlag(SearchEngineOptions.ArchiveMoe))
			yield return new ArchiveMoeEngine();

		if (SearchEngines.HasFlag(SearchEngineOptions.Iqdb3D))
			yield return new Iqdb3DEngine();

		if (SearchEngines.HasFlag(SearchEngineOptions.Fluffle))
			yield return new FluffleEngine();

		if (SearchEngines.HasFlag(SearchEngineOptions.GoogleLens))
			yield return new GoogleLensEngine();
	}*/

	public async ValueTask LoadEngines(IEnumerable<BaseSearchEngine> engines2, CancellationToken ct)
	{
		foreach (BaseSearchEngine engine in engines2) {
			if (engine is ISearchConfigReceiver rcvr) {
				s_logger.LogTrace("Applying config to {Engine}", engine.Name);
				await rcvr.ApplyConfigAsync(this, ct);

			}

			if (engine is ICookiesReceiver ck) {
				s_logger.LogTrace("Applying cookies to {Engine}", engine.Name);
				ck.CookiesSource = GetCookiesSource();
			}
		}
	}

#region

	public static readonly Configuration Configuration =
		ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

	private bool Set<T>(T t = default, [CMN] string name = null)
	{
		bool b = Configuration.AddUpdateSetting(name, t.ToString());
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


	public SearchConfig() { }


	public event PropertyChangedEventHandler PropertyChanged;

	private void OnPropertyChanged([CMN] string propertyName = null)
	{
		s_logger.LogTrace("Changed {PropName}", propertyName);
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}

	public override string ToString()
	{
		return $"{SearchEngines}\n{PriorityEngines}";
	}

}