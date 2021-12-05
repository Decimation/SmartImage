using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.XPath;
using Kantan.Net;
using SmartImage.Lib.Searching;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Kantan.Diagnostics;
using SmartImage.Lib.Engines.Model;

#nullable disable
// ReSharper disable CognitiveComplexity

// ReSharper disable IdentifierTypo
// ReSharper disable StringLiteralTypo

namespace SmartImage.Lib.Engines.Impl;

public sealed class Ascii2DEngine : WebSearchEngine
{
	public Ascii2DEngine() : base("https://ascii2d.net/search/url/") { }

	public override TimeSpan Timeout => TimeSpan.FromSeconds(5);

	public override SearchEngineOptions EngineOption => SearchEngineOptions.Ascii2D;

	public override string Name => EngineOption.ToString();

	/// <inheritdoc />
	public override EngineSearchType SearchType => EngineSearchType.Image | EngineSearchType.Metadata;

	protected override Uri GetRawUri(ImageQuery query)
	{
		var uri = base.GetRawUri(query);

		// var       request  = WebRequest.Create(uri);
		// using var response = request.GetResponse();

		var request = new HttpRequestMessage()
		{
			RequestUri = uri
		};

		var response = new HttpClient().Send(request);


		/*
		 * URL parameters
		 *
		 * color	https://ascii2d.net/search/color/<hash>
		 * detail	https://ascii2d.net/search/bovw/<hash>
		 *
		 */

		/*
		 * With Ascii2D, two requests need to be made in order to get the detail results
		 * as the color results are returned by default
		 *
		 */

		// var    responseUri = response.ResponseUri;

		Debug.Assert(response.RequestMessage?.RequestUri != null);

		var responseUri = response.RequestMessage.RequestUri;

		string detailUrl = responseUri.ToString().Replace("/color/", "/bovw/");

		return new Uri(detailUrl);

	}

	protected override SearchResultOrigin GetResultOrigin(ImageQuery query)
	{
		var now = Stopwatch.GetTimestamp();

		var rawUri = GetRawUri(query);

		using var client = new HttpClient();

		var response = client.Send(new HttpRequestMessage(HttpMethod.Get, rawUri));

		var task = response.Content.ReadAsStringAsync();
		task.Wait(Timeout);

		var content = task.Result;

		var diff = TimeSpan.FromTicks(Stopwatch.GetTimestamp() - now);

		var stub = new SearchResultOrigin()
		{
			InitialResponse = response,
			Content         = content,
			Retrieval       = diff,
			InitialSuccess  = true,
			RawUri          = rawUri
		};


		return stub;
	}


	protected override SearchResult Process(object obj, SearchResult sr)
	{
		var doc = (IDocument) obj;

		var nodes = doc.Body.SelectNodes("//*[contains(@class, 'info-box')]");

		var rg = new List<ImageResult>();

		foreach (var node in nodes) {
			var ir = new ImageResult();

			var info = node.ChildNodes.Where(n => !String.IsNullOrWhiteSpace(n.TextContent)).ToArray();

			string hash = info.First().TextContent;

			ir.OtherMetadata.Add("Hash", hash);

			string[] data = info[1].TextContent.Split(' ');

			string[] res = data[0].Split('x');
			ir.Width  = Int32.Parse(res[0]);
			ir.Height = Int32.Parse(res[1]);

			string fmt = data[1];

			string size = data[2];

			if (info.Length >= 3) {
				var node2 = info[2];
				var desc  = info.Last().FirstChild;
				var ns    = desc.NextSibling;

				if (node2.ChildNodes.Length >= 2 && node2.ChildNodes[1].ChildNodes.Length >= 2) {
					var node2Sub = node2.ChildNodes[1];

					if (node2Sub.ChildNodes.Length >= 8) {
						ir.Description = node2Sub.ChildNodes[3].TextContent.Trim();
						ir.Artist      = node2Sub.ChildNodes[5].TextContent.Trim();
						ir.Site        = node2Sub.ChildNodes[7].TextContent.Trim();
					}
				}

				if (ns.ChildNodes.Length >= 4) {
					var childNode = ns.ChildNodes[3];

					string l1 = ((IHtmlElement) childNode).GetAttribute("href");

					if (l1 is not null) {
						ir.Url = new Uri(l1);
					}
				}
			}

			rg.Add(ir);
		}

		// Skip original image

		rg = rg.Skip(1).ToList();

		sr.PrimaryResult = rg.First();

		//sr.PrimaryResult.UpdateFrom(rg[0]);

		sr.OtherResults.AddRange(rg);

		sr.PrimaryResult.Quality = sr.PrimaryResult.MegapixelResolution switch
		{
			null => ResultQuality.Indeterminate,
			>= 1 => ResultQuality.High,
			_    => ResultQuality.Low,
		};


		return sr;
	}
}