using System;
using System.Drawing;
using System.Net;
using RestSharp;
using SmartImage.Searching;

namespace SmartImage.Engines.TraceMoe
{
	public sealed class TraceMoeEngine : BasicSearchEngine
	{
		public TraceMoeEngine() : base("https://trace.moe/?url=") { }

		public override string Name => "trace.moe";

		public override SearchEngineOptions Engine => SearchEngineOptions.TraceMoe;

		private static TraceMoeRootObject GetApiResults(string url, out HttpStatusCode code, out ResponseStatus status,
			out string msg)
		{
			// https://soruly.github.io/trace.moe/#/

			var rc = new RestClient("https://trace.moe/api/");

			var rq = new RestRequest("search");
			rq.AddQueryParameter("url", url);
			rq.OnBeforeDeserialization = resp => { resp.ContentType = "application/json"; };
			rq.RequestFormat = DataFormat.Json;

			IRestResponse<TraceMoeRootObject> re = rc.Execute<TraceMoeRootObject>(rq, Method.GET);

			code = re.StatusCode;
			status = re.ResponseStatus;
			msg = re.ErrorMessage;
			
			return re.Data;
		}

		private ISearchResult[] ConvertResults(TraceMoeRootObject obj)
		{
			var docs = obj.docs;
			var results = new ISearchResult[docs.Count];

			for (int i = 0; i < results.Length; i++) {
				var doc = docs[i];
				var sim = (float?) doc.similarity * 100;

				var malUrl = MAL_URL + doc.mal_id;

				results[i] = new FullSearchResult(this, malUrl, sim)
				{
					Source = doc.title_english
				};
			}

			return results;
		}

		//https://anilist.co/anime/{id}/
		private const string ANILIST_URL = "https://anilist.co/anime/";

		//https://myanimelist.net/anime/{id}/
		private const string MAL_URL = "https://myanimelist.net/anime/";

		

		public override float? FilterThreshold => 87.00F;

		public override FullSearchResult GetResult(string url)
		{
			FullSearchResult r;
			//var r = base.GetResult(url);

			var tm = GetApiResults(url, out var code, out var res, out var msg);

			if (tm?.docs != null) {
				// Most similar to least similar

				try {
					var results = ConvertResults(tm);
					var best    = results[0];

					r         = new FullSearchResult(this, best.Url, best.Similarity);
					r.Source = best.Source;
					r.Filter  = r.Similarity < FilterThreshold;
					

					r.AddExtendedResults(results);
				}
				catch (Exception e) {
					r = base.GetResult(url);
					r.ExtendedInfo.Add(e.Message);
					return r;
				}
				


			}
			else {
				r = base.GetResult(url);

				r.ExtendedInfo.Add(string.Format("API: Returned null (possible timeout) [{0} {1} {2}]", code, res,
					msg));
			}


			return r;
		}

		public override Color Color => Color.Cyan;
	}
}