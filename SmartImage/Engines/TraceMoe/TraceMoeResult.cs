#region

using System.Collections.Generic;
using JetBrains.Annotations;

#endregion

namespace SmartImage.Engines.TraceMoe
{
	[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
	public class TraceMoeDoc
	{
		public double from       { get; set; }
		public double to         { get; set; }
		public int    anilist_id { get; set; }
		public double at         { get; set; }
		public string season     { get; set; }
		public string anime      { get; set; }
		public string filename   { get; set; }

		public int? episode { get; set; }

		public string       tokenthumb       { get; set; }
		public double       similarity       { get; set; }
		public string       title            { get; set; }
		public string       title_native     { get; set; }
		public string       title_chinese    { get; set; }
		public string       title_english    { get; set; }
		public string       title_romaji     { get; set; }
		public int          mal_id           { get; set; }
		public List<string> synonyms         { get; set; }
		public List<object> synonyms_chinese { get; set; }
		public bool         is_adult         { get; set; }
	}

	[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
	public class TraceMoeRootObject
	{
		public int               RawDocsCount      { get; set; }
		public long              RawDocsSearchTime { get; set; }
		public int               ReRankSearchTime  { get; set; }
		public bool              CacheHit          { get; set; }
		public int               trial             { get; set; }
		public int               limit             { get; set; }
		public int               limit_ttl         { get; set; }
		public int               quota             { get; set; }
		public int               quota_ttl         { get; set; }
		public List<TraceMoeDoc> docs              { get; set; }
	}
}