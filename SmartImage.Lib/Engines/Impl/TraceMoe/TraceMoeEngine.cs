using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Net;
using RestSharp;
using SmartImage.Lib.Searching;

namespace SmartImage.Lib.Engines.Impl.TraceMoe
{
	public sealed class TraceMoeEngine : SearchEngine
	{
		public TraceMoeEngine() : base("https://trace.moe/?url=") { }

		public override string Name => "trace.moe";

		public override SearchEngineOptions Engine => SearchEngineOptions.TraceMoe;

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
					Debug.WriteLine($"{e.Message}");
					//r.AddErrorMessage(e.Message);
					return r;
				}


			}
			else {
				r = base.GetResult(url);

				//r.AddErrorMessage(msg);
			}


			return r;
		}
	}
}