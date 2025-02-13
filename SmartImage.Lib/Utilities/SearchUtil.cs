using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using SmartImage.Lib.Engines;
using SmartImage.Lib.Engines.Impl.Search;
using SmartImage.Lib.Engines.Impl.Search.Other;
using SmartImage.Lib.Results;

namespace SmartImage.Lib.Utilities;

public static class SearchUtil
{

	public static bool IsSuccessful(this SearchResultStatus s)
		=> s is SearchResultStatus.Success || (!s.IsError() && !s.IsUnknown());

	public static bool IsUnknown(this SearchResultStatus s)
		=> s is SearchResultStatus.None;

	public static bool IsError(this SearchResultStatus s)
		=> s is SearchResultStatus.UnknownError or SearchResultStatus.IllegalInput
			   or SearchResultStatus.Unavailable or SearchResultStatus.Cooldown;

	public const SearchResultFlags ALT_STATUS =
		SearchResultFlags.NoResults | SearchResultFlags.Extraneous;

	public static bool HasFlagFast(this SearchResultFlags value, SearchResultFlags status)
		=> (value & status) != 0;

	public static IServiceCollection AddEngine<TSearchEngine>(this IServiceCollection svc, object seo = null)
		where TSearchEngine : class, IBaseSearchEngine
	{
		// todo: verify
		return svc.AddKeyedSingleton<IBaseSearchEngine, TSearchEngine>(seo);
	}

	public static IServiceCollection AddEngines(this IServiceCollection svc)
	{
		svc.AddEngine<SauceNaoEngine>(SearchEngineOptions.SauceNao);
		svc.AddEngine<ImgOpsEngine>(SearchEngineOptions.ImgOps);
		svc.AddEngine<GoogleImagesEngine>(SearchEngineOptions.GoogleImages);
		svc.AddEngine<TinEyeEngine>(SearchEngineOptions.TinEye);
		svc.AddEngine<IqdbEngine>(SearchEngineOptions.Iqdb);
		svc.AddEngine<Iqdb3DEngine>(SearchEngineOptions.Iqdb3D);
		svc.AddEngine<TraceMoeEngine>(SearchEngineOptions.TraceMoe);
		svc.AddEngine<KarmaDecayEngine>(SearchEngineOptions.KarmaDecay);
		svc.AddEngine<YandexEngine>(SearchEngineOptions.Yandex);
		svc.AddEngine<BingEngine>(SearchEngineOptions.Bing);
		svc.AddEngine<Ascii2DEngine>(SearchEngineOptions.Ascii2D);
		svc.AddEngine<RepostSleuthEngine>(SearchEngineOptions.RepostSleuth);
		svc.AddEngine<EHentaiEngine>(SearchEngineOptions.EHentai);
		svc.AddEngine<ArchiveMoeEngine>(SearchEngineOptions.ArchiveMoe);
		svc.AddEngine<FluffleEngine>(SearchEngineOptions.Fluffle);

		return svc;
	}

}