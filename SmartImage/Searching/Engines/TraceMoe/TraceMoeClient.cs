#region

using System;
using System.Drawing;
using System.Net;
using RestSharp;
using SmartImage.Searching.Model;

#endregion

namespace SmartImage.Searching.Engines.TraceMoe
{
	public sealed class TraceMoeClient : BasicSearchEngine
	{
		public TraceMoeClient() : base("https://trace.moe/?url=") { }

		public override string Name => "trace.moe";

		public override SearchEngineOptions Engine => SearchEngineOptions.TraceMoe;

		private static TraceMoeRootObject GetApiResults(string url, out HttpStatusCode code, out ResponseStatus status, out string msg)
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

			// todo: null sometimes
			return re.Data;
		}

		private ISearchResult[] ConvertResults(TraceMoeRootObject obj)
		{
			var docs = obj.docs;
			var results = new ISearchResult[docs.Count];

			for (int i = 0; i < results.Length; i++) {
				var doc = docs[i];
				var sim = (float?) doc.similarity * 100;

				var malurl = MAL_URL + doc.mal_id;

				results[i] = new SearchResult(this,malurl,sim );
				results[i].Caption = doc.title_english;
			}

			return results;
		}

		//https://anilist.co/anime/{id}/
		private const string ANILIST_URL = "https://anilist.co/anime/";

		//https://myanimelist.net/anime/{id}/
		private const string MAL_URL = "https://myanimelist.net/anime/";

		


		public override SearchResult GetResult(string url)
		{
			SearchResult r;
			//var r = base.GetResult(url);

			var tm = GetApiResults(url, out var code, out var res, out var msg);
			
			if (tm?.docs != null) {
				// Most similar to least similar
				var results = ConvertResults(tm);
				var best = results[0];

				r = new SearchResult(this,best.Url, best.Similarity);
				r.Caption = best.Caption;

				r.AddExtendedResults(results);



			}
			else {
				r = base.GetResult(url);
				r.ExtendedInfo.Add(string.Format("API: Returned null (possible timeout) [{0} {1} {2}]", code,res,msg));
			}


			return r;
		}

		public override Color Color => Color.Cyan;
	}
}