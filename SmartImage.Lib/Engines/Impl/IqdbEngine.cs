// ReSharper disable UnusedMember.Global

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using AngleSharp.XPath;
using RestSharp;
using SimpleCore.Diagnostics;
using SimpleCore.Net;
using SimpleCore.Utilities;
using SmartImage.Lib.Searching;
using SmartImage.Lib.Utilities;

// ReSharper disable StringLiteralTypo

namespace SmartImage.Lib.Engines.Impl
{
	public sealed class IqdbEngine : ClientSearchEngine
	{
		public IqdbEngine() : base("https://iqdb.org/?url=", "https://iqdb.org/") { }

		public override SearchEngineOptions EngineOption => SearchEngineOptions.Iqdb;

		public override string Name => "IQDB";


		private static ImageResult ParseResult(IHtmlCollection<IElement> tr)
		{
			var caption = tr[0];
			var img     = tr[1];
			var src     = tr[2];

			string url = null!;

			//img.ChildNodes[0].ChildNodes[0].TryGetAttribute("href")


			try {
				//url = src.FirstChild.ChildNodes[2].ChildNodes[0].TryGetAttribute("href");

				url = img.ChildNodes[0].ChildNodes[0].TryGetAttribute("href");

				// Links must begin with http:// in order to work with "start"

			}
			catch {
				// ignored
			}


			int w = 0, h = 0;

			if (tr.Length >= 4) {
				var res = tr[3];

				var wh = res.TextContent.Split(StringConstants.MUL_SIGN);

				var wStr = wh[0].SelectOnlyDigits();
				w = Int32.Parse(wStr);

				// May have NSFW caption, so remove it

				var hStr = wh[1].SelectOnlyDigits();
				h = Int32.Parse(hStr);
			}

			float? sim;

			if (tr.Length >= 5) {
				var simNode = tr[4];
				var simStr  = simNode.TextContent.Split('%')[0];
				sim = Single.Parse(simStr);
				sim = MathF.Round(sim.Value, 2);
			}
			else {
				sim = null;
			}

			Uri uri;

			if (url != null) {
				if (url.StartsWith("//")) {
					url = "http:" + url;
				}

				uri = new Uri(url);
			}
			else {
				uri = null;
			}


			var result = new ImageResult
			{
				Url         = uri,
				Similarity  = sim,
				Width       = w,
				Height      = h,
				Source      = src.TextContent,
				Description = caption.TextContent,
			};


			return result;
		}


		private IDocument GetDocument(ImageQuery query)
		{
			var rq = new RestRequest(Method.POST);

			const int MAX_FILE_SIZE = 8388608;

			rq.AddParameter("MAX_FILE_SIZE", MAX_FILE_SIZE, ParameterType.GetOrPost);
			rq.AddHeader("Content-Type", "multipart/form-data");

			byte[] fileBytes = Array.Empty<byte>();
			object uri       = string.Empty;

			if (query.IsFile) {
				fileBytes = File.ReadAllBytes(query.Value);
			}
			else if (query.IsUri) {
				uri = query.Value;
			}
			else {
				throw new SmartImageException();
			}

			rq.AddFile("file", fileBytes, "image.jpg");
			rq.AddParameter("url", uri, ParameterType.GetOrPost);

			//rq.AddParameter("service[]", new[] {1, 2, 3, 4, 5, 6, 11, 13}, ParameterType.GetOrPost);

			var response = Client.Execute(rq);

			var parser = new HtmlParser();
			return parser.ParseDocument(response.Content);
		}


		[DebuggerHidden]
		public override SearchResult GetResult(ImageQuery query)
		{
			//var sr = base.GetResult(query);
			var sr = new SearchResult(this);

			try {

				sr = Process(query, sr);
			}
			catch (Exception e) {
				sr.Status = ResultStatus.Failure;
				Trace.WriteLine($"{Name}: {e.Message}", LogCategories.C_ERROR);
			}

			return sr;
		}

		protected override SearchResult Process(ImageQuery query, SearchResult sr)
		{
			// Don't select other results

			var doc = GetDocument(query);

			var pages  = doc.Body.SelectSingleNode("//div[@id='pages']");
			var tables = ((IHtmlElement) pages).SelectNodes("div/table");

			// No relevant results?

			var ns = doc.Body.QuerySelector("#pages > div.nomatch");

			if (ns != null) {

				sr.Status = ResultStatus.NoResults;

				return sr;
			}

			var select =
				tables.Select(table => ((IHtmlElement) table).QuerySelectorAll("table > tbody > tr:nth-child(n)"));


			var images = select.Select(ParseResult).ToList();


			// First is original image
			images.RemoveAt(0);

			var best = images[0];
			sr.PrimaryResult.UpdateFrom(best);
			sr.OtherResults.AddRange(images);

			return sr;
		}
	}
}