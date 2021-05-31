using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using JetBrains.Annotations;
using RestSharp;
using SmartImage.Lib.Searching;

// ReSharper disable InconsistentNaming

namespace SmartImage.Lib.Engines.Impl
{
	public sealed class TraceMoeEngine : BaseSearchEngine
	{
		public TraceMoeEngine() : base("https://api.trace.moe") { }


		/// <summary>
		/// Used to retrieve more information about results
		/// </summary>
		private readonly AnilistClient m_anilistClient = new();

		public override string Name => "trace.moe";

		public override SearchEngineOptions Engine => SearchEngineOptions.TraceMoe;

		[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
		internal class TraceMoeDoc
		{
			public double from     { get; set; }
			public double to       { get; set; }
			public long   anilist  { get; set; }
			public string filename { get; set; }

			public long episode { get; set; }

			public double similarity { get; set; }
			public string video      { get; set; }
			public string image      { get; set; }
		}

		[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
		internal class TraceMoeRootObject
		{
			public long              frameCount { get; set; }
			public string            error      { get; set; }
			public List<TraceMoeDoc> result     { get; set; }
		}

		private TraceMoeRootObject GetApiResults(string url)
		{
			// https://soruly.github.io/trace.moe/#/

			var rc = new RestClient(BaseUrl);


			var rq = new RestRequest("search");
			rq.AddQueryParameter("url", url);
			//rq.AddQueryParameter("anilistInfo", "");
			rq.OnBeforeDeserialization = resp => { resp.ContentType = "application/json"; };
			rq.RequestFormat           = DataFormat.Json;

			var re = rc.Execute<TraceMoeRootObject>(rq, Method.GET);

			return re.Data;
		}


		private IEnumerable<ImageResult> ConvertResults(TraceMoeRootObject obj)
		{
			var docs    = obj.result;
			var results = new ImageResult[docs.Count];

			for (int i = 0; i < results.Length; i++) {
				var doc = docs[i];
				var sim = MathF.Round((float) (doc.similarity * 100.0f), 2);


				var anilistUrl = ANILIST_URL + doc.anilist;

				var name = m_anilistClient.GetTitle((int) doc.anilist);

				results[i] = new ImageResult
				{
					Url         = new Uri(anilistUrl),
					Similarity  = sim,
					Source      = name,
					Description = $"Episode #{doc.episode} @ {TimeSpan.FromSeconds(doc.from)}"
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

			var tm = GetApiResults(url.Uri.ToString());

			if (tm?.result != null) {
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
					r.Status = ResultStatus.Failure;
					return r;
				}


			}
			else {
				r = base.GetResult(url);
				Debug.WriteLine($"[error] tracemoe: api error");
				//r.AddErrorMessage(msg);
			}


			return r;
		}
	}
}