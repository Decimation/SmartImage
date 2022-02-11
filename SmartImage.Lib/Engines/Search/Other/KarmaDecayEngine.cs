using System;
using SmartImage.Lib.Engines.Search.Base;
using SmartImage.Lib.Searching;

namespace SmartImage.Lib.Engines.Search.Other;

[Obsolete]
public sealed class KarmaDecayEngine : BaseSearchEngine
{
	public KarmaDecayEngine() : base("http://karmadecay.com/search/?q=") { }

	public override EngineSearchType SearchType => EngineSearchType.External | EngineSearchType.Metadata;

	public override SearchEngineOptions EngineOption => SearchEngineOptions.KarmaDecay;
}