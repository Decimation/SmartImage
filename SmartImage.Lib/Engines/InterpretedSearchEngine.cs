using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;
using RestSharp;
using SimpleCore.Net;
using SmartImage.Lib.Searching;
using SmartImage.Lib.Utilities;

namespace SmartImage.Lib.Engines
{
	public abstract class InterpretedSearchEngine : BaseSearchEngine
	{
		public abstract override SearchEngineOptions Engine { get; }

		public abstract override string Name { get; }

		protected InterpretedSearchEngine(string baseUrl) : base(baseUrl) { }


		protected virtual HtmlDocument GetDocument(SearchResult sr)
		{
			/*if (!Network.TryGetString(sr.RawUri.ToString()!, out var html))
			{
				sr.RawUri            = null;
				sr.PrimaryResult.Url = null;

				//sr.AddErrorMessage("Unavailable");
				throw new SmartImageException();
			}*/


			//string response = Network.GetString(sr.RawUri.ToString()!);

			var doc = new HtmlWeb().Load(sr.RawUri);


			//var response = Network.GetSimpleResponse(sr.RawUri.ToString()!);

			//var req = new RestRequest(BaseUrl);
			//req.AddQueryParameter("url", sr.RawUri.ToString());
			//var rc  = new RestClient();
			//var res =rc.Execute(req);
			//var response   = res.Content;

			//var doc = new HtmlDocument();
			//doc.LoadHtml(response.Content);
			//doc.LoadHtml(response);
			

			return doc;
		}

		protected abstract SearchResult Process(HtmlDocument doc, SearchResult sr);

		[DebuggerHidden]
		public override SearchResult GetResult(ImageQuery query)
		{

			var sr = base.GetResult(query);

			if (!sr.IsSuccessful) {
				return sr;
			}

			try {

				var doc = GetDocument(sr);
				sr = Process(doc, sr);
			}
			catch (Exception e) {
				sr.Status = ResultStatus.Failure;
				Trace.WriteLine($"{Name}: {e.Message} {e.Source} {e.StackTrace}");

			}

			return sr;
		}
	}
}