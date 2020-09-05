#region

using System;
using System.Net;
using RestSharp;
using SmartImage.Searching;

#endregion

namespace SmartImage.Engines.TraceMoe
{
	public sealed class TraceMoe : QuickSearchEngine
	{
		public TraceMoe() : base("https://trace.moe/?url=") { }

		public override string Name => "trace.moe";

		public override SearchEngines Engine => SearchEngines.TraceMoe;

		private TraceMoeRootObject GetApiResults(string url, out HttpStatusCode code)
		{
			// https://soruly.github.io/trace.moe/#/

			var rc = new RestClient("https://trace.moe/api/");

			var rq = new RestRequest("search");
			rq.AddQueryParameter("url", url);
			rq.OnBeforeDeserialization = resp => { resp.ContentType = "application/json"; };
			rq.RequestFormat           = DataFormat.Json;

			IRestResponse<TraceMoeRootObject> re = rc.Execute<TraceMoeRootObject>(rq, Method.GET);

			code = re.StatusCode;
			

			// todo: null sometimes
			return re.Data;
		}

		public override SearchResult GetResult(string url)
		{
			var r = base.GetResult(url);

			var tm = GetApiResults(url, out var code);

			r.ExtendedInfo.Add(string.Format("Code: {0}",code));
			
			if (tm?.docs != null) {
				// Most similar to least similar
				var mostSimilarDoc = tm.docs[0];

				r.Similarity = (float?) mostSimilarDoc.similarity;

				r.ExtendedInfo.Add(String.Format("Name: {0}", mostSimilarDoc.title_english));
				
			}
			else {
				r.ExtendedInfo.Add("API returned null (possible timeout)");
			}


			return r;
		}
	}
}