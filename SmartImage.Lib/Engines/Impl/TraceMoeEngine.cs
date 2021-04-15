using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using JetBrains.Annotations;
using RestSharp;
using SmartImage.Lib.Searching;

namespace SmartImage.Lib.Engines.Impl
{
	public sealed class TraceMoeEngine : BaseSearchEngine
	{
		public TraceMoeEngine() : base("https://trace.moe/?url=") { }

		public override string Name => "trace.moe";

		public override SearchEngineOptions Engine => SearchEngineOptions.TraceMoe;

		[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
		internal class TraceMoeDoc
		{
			public double from       { get; set; }
			public double to         { get; set; }
			public long   anilist_id { get; set; }
			public double at         { get; set; }
			public string season     { get; set; }
			public string anime      { get; set; }
			public string filename   { get; set; }

			public long? episode { get; set; }

			public string       tokenthumb       { get; set; }
			public double       similarity       { get; set; }
			public string       title            { get; set; }
			public string       title_native     { get; set; }
			public string       title_chinese    { get; set; }
			public string       title_english    { get; set; }
			public string       title_romaji     { get; set; }
			public long         mal_id           { get; set; }
			public List<string> synonyms         { get; set; }
			public List<object> synonyms_chinese { get; set; }
			public bool         is_adult         { get; set; }
		}

		[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
		internal class TraceMoeRootObject
		{
			public long              RawDocsCount      { get; set; }
			public long              RawDocsSearchTime { get; set; }
			public long              ReRankSearchTime  { get; set; }
			public bool              CacheHit          { get; set; }
			public long              trial             { get; set; }
			public long              limit             { get; set; }
			public long              limit_ttl         { get; set; }
			public long              quota             { get; set; }
			public long              quota_ttl         { get; set; }
			public List<TraceMoeDoc> docs              { get; set; }
		}

		private static TraceMoeRootObject GetApiResults(string url,
			out HttpStatusCode code, out ResponseStatus status, out string msg)
		{
			// https://soruly.github.io/trace.moe/#/

			var rc = new RestClient("https://trace.moe/api/");


			var rq = new RestRequest("search");
			rq.AddQueryParameter("url", url);
			rq.OnBeforeDeserialization = resp => { resp.ContentType = "application/json"; };
			rq.RequestFormat           = DataFormat.Json;

			var re = rc.Execute<TraceMoeRootObject>(rq, Method.GET);

			code   = re.StatusCode;
			status = re.ResponseStatus;
			msg    = re.ErrorMessage;

			return re.Data;
		}

		private ImageResult[] ConvertResults(TraceMoeRootObject obj)
		{
			var docs    = obj.docs;
			var results = new ImageResult[docs.Count];

			for (int i = 0; i < results.Length; i++) {
				var doc = docs[i];
				var sim = (float?) doc.similarity * 100;

				var malUrl = MAL_URL + doc.mal_id;

				results[i] = new ImageResult()
				{
					Url         = new Uri(malUrl),
					Similarity  = sim,
					Source      = doc.title_english,
					Description = $"Episode #{doc.episode} @ {TimeSpan.FromSeconds(doc.at)}"
				};
			}

			return results;
		}

		//https://anilist.co/anime/{id}/
		private const string ANILIST_URL = "https://anilist.co/anime/";

		//https://myanimelist.net/anime/{id}/
		private const string MAL_URL = "https://myanimelist.net/anime/";


		public const float FilterThreshold = 87.00F;

		public override SearchResult GetResult(ImageQuery url)
		{
			SearchResult r;
			//var r = base.GetResult(url);

			var tm = GetApiResults(url.Uri.ToString(), out var code, out var res, out var msg);

			if (tm?.docs != null) {
				// Most similar to least similar

				try {
					var results = ConvertResults(tm).ToList();
					var best    = results[0];

					r = new SearchResult(this)
					{
						PrimaryResult = best
					};

					r.OtherResults.AddRange(results);
				}
				catch (Exception e) {
					r = base.GetResult(url);
					Debug.WriteLine($"tracemoe: {e.Message}");
					//r.AddErrorMessage(e.Message);
					return r;
				}


			}
			else {
				r = base.GetResult(url);
				Debug.WriteLine($"tracemoe: api null {code} {res} {msg}");
				//r.AddErrorMessage(msg);
			}


			return r;
		}
	}
}