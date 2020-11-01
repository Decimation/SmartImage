using System.Collections.Generic;
using JetBrains.Annotations;

#pragma warning disable IDE1006, HAA0502, HAA0601
namespace SmartImage.Searching.Engines.TraceMoe
{
	[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
	public class TraceMoeDoc
	{
		public double from { get; set; }
		public double to { get; set; }
		public long anilist_id { get; set; }
		public double at { get; set; }
		public string season { get; set; }
		public string anime { get; set; }
		public string filename { get; set; }

		public long? episode { get; set; }

		public string tokenthumb { get; set; }
		public double similarity { get; set; }
		public string title { get; set; }
		public string title_native { get; set; }
		public string title_chinese { get; set; }
		public string title_english { get; set; }
		public string title_romaji { get; set; }
		public long mal_id { get; set; }
		public List<string> synonyms { get; set; }
		public List<object> synonyms_chinese { get; set; }
		public bool is_adult { get; set; }
	}

	[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
	public class TraceMoeRootObject
	{
		public long RawDocsCount { get; set; }
		public long RawDocsSearchTime { get; set; }
		public long ReRankSearchTime { get; set; }
		public bool CacheHit { get; set; }
		public long trial { get; set; }
		public long limit { get; set; }
		public long limit_ttl { get; set; }
		public long quota { get; set; }
		public long quota_ttl { get; set; }
		public List<TraceMoeDoc> docs { get; set; }
	}
}