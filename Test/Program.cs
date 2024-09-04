using System.Buffers.Text;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Json;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Channels;
using AngleSharp;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using CoenM.ImageHash;
using CoenM.ImageHash.HashAlgorithms;
using Flurl;
using Flurl.Http;
using Flurl.Http.Content;
using Kantan.Net.Utilities;
using Kantan.Text;
using Kantan.Utilities;
using Novus.Streams;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.PixelFormats;
using SmartImage.Lib;
using SmartImage.Lib.Clients;
using SmartImage.Lib.Clients.Booru;
using SmartImage.Lib.Engines;
using SmartImage.Lib.Engines.Impl.Search;
using SmartImage.Lib.Engines.Impl.Upload;
using SmartImage.Lib.Images;
using SmartImage.Lib.Images.Uni;
using SmartImage.Lib.Results;
using SmartImage.Lib.Utilities;
using Configuration = AngleSharp.Configuration;

#pragma warning disable IDE0079
#pragma warning disable CS0168, CS1998, CS0219
#pragma warning disable IDE0060, IDE1006, IDE0051, IDE0059
#pragma warning disable CS8602, CS8604

namespace Test;

public static class Program
{

	public static async Task Main(string[] args)
	{
		/*var r   = new Rule34Booru();
		var res = await r.GetPostsAsync(new BaseGelbooruClient.PostsRequest() { Tags = "anal" });
		var s   = await res.GetStringAsync();
		Console.WriteLine(s);

		// await TestEh3();
		Debugger.Break();*/

		// await test9();

		// var u1 = @"C:\Users\Deci\Pictures\Test Images\Test6.jpg";
		var u1 = @"C:\Users\Deci\Pictures\Epic anime\en-jia-2077.jpg";
		var q  = await SearchQuery.TryCreateAsync(u1);
		var u  = await q.UploadAsync();
		Console.WriteLine(u);

		var sc      = new SearchClient(new SearchConfig());
		var results = await sc.RunSearchAsync(q);

		var results2 = results.SelectMany(x => x.Results)
			.Where(r => !r.IsRaw && r.Url != null);


		// var rr = r.SelectMany(x => x.Results);

		foreach (var sr in results2) {
			Console.WriteLine($"{sr}");
			/*var items = await ImageScanner.Highest(q, sr.Results);

			foreach (ImageScanner.Item2 item2 in items) {
				Console.WriteLine($"{item2.Image} {item2.Item} {item2.Url}");

			}*/

			var imgs2 = await ImageScanner.ScanImagesAsync2(sr.Url);

			while (imgs2.Count > 0) {
				var imgs3 = await Task.WhenAny(imgs2);
				imgs2.Remove(imgs3);

				var imgs4 = await imgs3;

				if ( imgs4 != null) {
					Console.WriteLine(imgs4);
				}
			}
		}
		/*foreach (var result in r) {
			var g = result.Results.GroupBy(x => x.Root);

			foreach (var gr in g) {
				foreach (SearchResultItem item in gr) {
					Console.WriteLine($"{gr.Key} = {item}");
				}
			}

			Console.ReadKey();
		}*/
	}

	#region

	static SearchQuery _sq;

	/*private static async Task test9()
	{
		var q = _sq = await SearchQuery.TryCreateAsync(@"C:\Users\Deci\Pictures\Epic anime\1654086015521.png");

		await q.UploadAsync();
		var e = new SearchClient(new SearchConfig());
		e.OnResultComplete += E_OnResultComplete;
		var r1 = e.RunSearchAsync(q);

		var r = await r1;
		Console.WriteLine($"\nCOMPLETE\n");

		foreach (var sr in r) {
			Console.WriteLine(sr);

		}

		var tasks = new List<Task>(r.Select(x =>
		{
			var task = x.Process(_sq);
			Console.WriteLine($">> {x}");
			return task;
		}));

		while (tasks.Count > 0) {
			var task = await Task.WhenAny(tasks);
			tasks.Remove(task);
		}
	}*/

	private static void E_OnResultComplete(object sender, SearchResult e)
	{
		Console.WriteLine(e);

	}

	private static async Task Test8()
	{
		var path = @"C:\Users\Deci\Pictures\Epic anime\0c4c80957134d4304538c27499d84dbe.jpeg";

		/*var img  = Image.Load<Rgba32>(path);
		var img2 = Image.Load<Rgba32>(@"C:\Users\Deci\Pictures\Test Images\Test1b.jpg");
		var alg  = new DifferenceHash();
		var cmp  = alg.Hash(img);
		var cmp2 = alg.Hash(img2);
		var sim  = CompareHash.Similarity(cmp, cmp2);
		Console.WriteLine($"{sim}");*/
		var fc = new FlurlClient()
		{
			Settings =
			{
				Redirects =
				{
					MaxAutoRedirects = 30, Enabled = true, ForwardAuthorizationHeader = true, ForwardHeaders = true
				}
			}
		};

		var u = await fc.Request("https://api.copyseeker.net/OnTriggerDiscoveryByFile/")
			        .WithHeaders(new { User_Agent = HttpUtilities.UserAgent })
			        .AllowAnyHttpStatus()
			        .WithAutoRedirect(true)
			        .WithTimeout(TimeSpan.FromSeconds(30))
			        .PostMultipartAsync(x =>
			        {
				        x.AddFile("file", path);
				        x.AddString("discoveryType", "ReverseImageSearch");
			        });

		Console.WriteLine(
			$"{u.StatusCode} {u.ResponseMessage.ReasonPhrase} {u.ResponseMessage.RequestMessage.RequestUri}");

		Console.WriteLine();

		// var s = await u.GetStringAsync();
		var j = JsonObject.Load(await u.GetStreamAsync());
		Console.WriteLine(j.ToString());
		var s = j["discoveryId"].ToString();
		Console.WriteLine(s);

		foreach (var header in u.ResponseMessage.Content.Headers) {
			Console.WriteLine(header);
		}

		u = await fc.Request("https://api.copyseeker.net/OnProvideDiscovery/")
			    .WithHeaders(new { User_Agent = HttpUtilities.UserAgent, Referer = "https://copyseeker.net" })
			    .AllowAnyHttpStatus()
			    .WithTimeout(TimeSpan.FromSeconds(30))
			    .WithAutoRedirect(true)
			    .PostJsonAsync(new
			    {
				    discoveryId = s,
				    hasBlocker  = false,
			    });

		Console.WriteLine(
			$"{u.StatusCode} {u.ResponseMessage.ReasonPhrase} {u.ResponseMessage.RequestMessage.RequestUri}");
		Console.WriteLine();
		Console.WriteLine(await u.GetStringAsync());

		foreach (var header in u.ResponseMessage.Content.Headers) {
			Console.WriteLine(header);
		}

		var bc = new BrowsingContext(Configuration.Default.WithCookies().WithJs().WithRequesters());
	}

	private static async Task Test7(string file)
	{
		var sq = await SearchQuery.TryCreateAsync(file);

		var hashrg = await SHA256.HashDataAsync(sq.Image.Stream);
		sq.Image.Stream.TrySeek();
		var hash = HashHelper.Sha256.ToString(hashrg);
		Console.WriteLine(sq);

		// await sq.UploadAsync();
		var eh = new EHentaiEngine();
		var sc = new SearchConfig() { ReadCookies = true };
		await eh.ApplyCookiesAsync();
		var res = await eh.GetResultAsync(sq);

		foreach (var re in res.Results) {
			Console.WriteLine(re);
		}

		string api = "http://127.0.0.1:45871/";
		var    key = "7b00a1959028c6269e8f1a97a38c8a449802ec85215ea072555e95861f2b7185";
		var    h   = new HydrusClient(api, key);

		var jsonValue = await h.GetFileHashesAsync(hash);

		Console.WriteLine(jsonValue.ToString());

		// Debugger.Break();

		var jsonValue2 = await h.GetFileMetadataAsync(hash);
		Console.WriteLine(jsonValue2.ToString());

		// Debugger.Break();

		var jsonValue3 = await h.GetFileRelationshipsAsync(hash);

		Console.WriteLine(jsonValue3.ToString());

		// Debugger.Break();

		var vs = ((JsonObject) jsonValue3)["file_relationships"];

		var re2 = JsonSerializer.Deserialize<Dictionary<string, HydrusFileRelationship>>(vs.ToString());

		foreach (var v in HydrusFileRelationship.Deserialize(jsonValue3)) {
			Console.WriteLine($"{v.Value.Duplicates}");
		}
	}

	private static async Task Test6()
	{
		var u1 = @"C:\Users\Deci\Pictures\Test Images\Test6.jpg";
		var q  = await SearchQuery.TryCreateAsync(u1);
		var u  = await q.UploadAsync();
		Console.WriteLine(u);

		var sc = new SearchClient(new SearchConfig());
		var r  = await sc.RunSearchAsync(q);

		foreach (var result in r) {
			var g = result.Results.GroupBy(x => x.Root);

			foreach (var gr in g) {
				foreach (SearchResultItem item in gr) {
					Console.WriteLine($"{gr.Key} = {item}");
				}
			}

			Console.ReadKey();
		}
	}

	private static async Task Testx()
	{
		var req = new HttpRequestMessage(HttpMethod.Post, BaseUploadEngine.Default.EndpointUrl)
		{
			Headers =
			{
				{ "Connection", "keep-alive" },
				{ "User-Agent", HttpUtilities.UserAgent }
			}
		};
		var cl = new HttpClient();

		req.Content = new MultipartFormDataContent()
		{
			{
				new FileContent(
						@"C:\Users\Deci\Pictures\Epic anime\__sashou_mihiro_original_drawn_by_infinote__ec1ebb276d934ba6ce9d03a08d02f7d9.png")
					{ },
				"fileToUpload"
			},
			{ new StringContent("fileupload"), "reqtype" },
			{ new StringContent("1h"), "time" },

			// { new StringContent(""), "userhash" }

		};
		var response2 = await cl.SendAsync(req, HttpCompletionOption.ResponseHeadersRead);

		Console.WriteLine(response2);
	}

	private static async Task TestHydrus2()
	{
		string api = "http://127.0.0.1:45871/";
		var    key = "7b00a1959028c6269e8f1a97a38c8a449802ec85215ea072555e95861f2b7185";

		string hash = "1b497547588892e076b40f83d6cae2372df2998785dbeff5685b4c0afc118ebb";
		var    sq   = await SearchQuery.TryCreateAsync(@"C:\Users\Deci\Pictures\Epic anime\59530998_p1.jpg");
		var    str  = await "https://i.imgur.com/QtCausw.png".GetStreamAsync();
		var    strs = ToString(await SHA256.HashDataAsync(str));

		var s = ToString(await SHA256.HashDataAsync(sq?.Image.Stream));
		Debug.Assert(s == hash);

		var h = new HydrusClient(api, key);

		var jsonValue = await h.GetFileHashesAsync(hash);

		Console.WriteLine(jsonValue.ToString());

		// Debugger.Break();

		var jsonValue2 = await h.GetFileMetadataAsync(hash);
		Console.WriteLine(jsonValue2.ToString());

		// Debugger.Break();

		var jsonValue3 = await h.GetFileRelationshipsAsync(hash);

		Console.WriteLine(jsonValue3.ToString());

		// Debugger.Break();

		var vs = ((JsonObject) jsonValue3)["file_relationships"];

		var re = JsonSerializer.Deserialize<Dictionary<string, HydrusFileRelationship>>(vs.ToString());

		foreach (var v in HydrusFileRelationship.Deserialize(jsonValue3)) {
			Console.WriteLine($"{v.Value.Duplicates}");
		}
	}

	private static async Task TestHydrus()
	{
		string api = "http://127.0.0.1:45871/";
		var    key = "7b00a1959028c6269e8f1a97a38c8a449802ec85215ea072555e95861f2b7185";

		// string hash = "ea1f15917821aad11b9c037500d97efc4054f3f624f52fdfbdaa25b7262b0127";
		var p =
			@"H:\Hydrus Network\db\client_files\fcd\cdaedc9769e6a768e5ac28f01d2d175ac897bf82e4b1ebf539979fbe0f9ea2ee.jpg";
		string hash = "cdaedc9769e6a768e5ac28f01d2d175ac897bf82e4b1ebf539979fbe0f9ea2ee";

		Debug.Assert(ToString(SHA256.HashData(File.OpenRead(p))) == hash);

		var h = new HydrusClient(api, key);

		var jsonValue = await h.GetFileHashesAsync(hash);

		Console.WriteLine(jsonValue.ToString());

		// Debugger.Break();

		var jsonValue2 = await h.GetFileMetadataAsync(hash);
		Console.WriteLine(jsonValue2.ToString());

		// Debugger.Break();

		var jsonValue3 = await h.GetFileRelationshipsAsync(hash);

		Console.WriteLine(jsonValue3.ToString());

		// Debugger.Break();

		var vs = ((JsonObject) jsonValue3)["file_relationships"];

		var re = JsonSerializer.Deserialize<Dictionary<string, HydrusFileRelationship>>(vs.ToString());

		foreach (var v in HydrusFileRelationship.Deserialize(jsonValue3)) {
			Console.WriteLine($"{v.Value.Duplicates}");
		}
	}

	public static string ToString(byte[] h, bool lower = true)
	{
		var stringBuilder = new StringBuilder();

		foreach (byte b in h)
			stringBuilder.AppendFormat("{0:X2}", b);

		string hashString = stringBuilder.ToString();

		if (lower) {
			hashString = hashString.ToLower();
		}

		return hashString;
	}

	public static byte[] GetHash(string inputString)
	{
		return SHA256.HashData(Encoding.UTF8.GetBytes(inputString));
	}

	private static async Task TestYandex2()
	{
		var c = await "https://yandex.com/images/".WithCookies(out var cj).GetAsync();
		Console.WriteLine(cj.Count);

		foreach (var vv in cj) {
			Console.WriteLine($"{vv.Name} {vv.Value}");
		}

		var s1 =
			@"C:\Users\Deci\Pictures\Epic anime\__sashou_mihiro_original_drawn_by_infinote__ec1ebb276d934ba6ce9d03a08d02f7d9.png";
		var s2 = @"C:\Users\Deci\Pictures\Test Images\Test1.jpg";

		var j = await "https://yandex.com/images-apphost/image-download"
			        .WithCookies(cj)
			        .WithAutoRedirect(true)
			        .SetQueryParams(new
			        {
				        cbird                    = 111,
				        images_avatars_size      = "preview",
				        images_avatars_namespace = "images-cbir"
			        })
			        .WithHeaders(new
			        {
				        User_Agent = HttpUtilities.UserAgent
			        })
			        .OnError(x =>
			        {
				        Console.WriteLine(x.Exception);
				        x.ExceptionHandled = true;
			        })
			        .AllowAnyHttpStatus()
			        .PostAsync(new FileContent(
				                   s2));
		Console.WriteLine(j.ResponseMessage);
		Console.WriteLine(j.StatusCode);
		Console.WriteLine(j.ResponseMessage.RequestMessage.RequestUri);
		var p   = new HtmlParser();
		var doc = await p.ParseDocumentAsync(await j.GetStringAsync());
		Console.WriteLine();
		Debugger.Break();
	}

	private static async Task TestArchiveMoe()
	{
		const string fs  = @"C:\Users\Deci\Downloads\1654086015521.png";
		string       fs2 = @"C:\Users\Deci\Downloads\1688485598527788.png";

		var ae = new ArchiveMoeEngine();
		var q  = await SearchQuery.TryCreateAsync(fs2);
		var u  = await q.UploadAsync();

		var r = await ae.GetResultAsync(q);

		Console.WriteLine(r);

		foreach (SearchResultItem searchResultItem in r.Results) {
			Console.WriteLine(searchResultItem);
		}
	}

	private static async Task TestSauceNao3()
	{
		var e = new SauceNaoEngine();

		var q = await SearchQuery.TryCreateAsync(
			        @"C:\Users\Deci\Pictures\Epic anime\__sashou_mihiro_original_drawn_by_infinote__ec1ebb276d934ba6ce9d03a08d02f7d9.png");
		var s = await q.UploadAsync();
		Console.WriteLine(s);
		var r = await e.GetResultAsync(q);

		Console.WriteLine(r);

		foreach (var g in r.Results) {
			Console.WriteLine($"{g}");

			Console.WriteLine();
		}
	}

	private static async Task TestSauceNao2()
	{
		var e = new SauceNaoEngine("362e7e82bc8cf7f6025431fbf3006510057298c3");

		var q = await SearchQuery.TryCreateAsync(
			        @"https://litter.catbox.moe/ew656b.jpg");
		await q.UploadAsync();
		var r = await e.GetResultAsync(q);

		Console.WriteLine(r);

		foreach (var g in r.Results) {
			Console.WriteLine($"{g}");

			Console.WriteLine();
		}
	}

	private static async Task TestSauceNao()
	{
		var e = new SauceNaoEngine();

		var q = await SearchQuery.TryCreateAsync(
			        @"C:\Users\Deci\Downloads\__nero_claudius_and_nero_claudius_fate_and_1_more_drawn_by_morii_shizuki__c62d403689e5f4654e068ae3ef7258f3.jpg");
		await q.UploadAsync();
		var r = await e.GetResultAsync(q);

		Console.WriteLine(r);
		var group = r.Results.GroupBy(r => r.Parent);

		foreach (IGrouping<SearchResultItem, SearchResultItem> g in group) {
			Console.WriteLine($"{g}");

			foreach (SearchResultItem gi in g) {
				Console.WriteLine($"{gi.Url} {gi.Parent}");

			}

			Console.WriteLine();
		}
	}

	private static async Task TestYandex()
	{
		var e = new YandexEngine();
		var q = await SearchQuery.TryCreateAsync(@"C:\Users\Deci\Downloads\662.cover2.png");
		await q.UploadAsync();
		var r = await e.GetResultAsync(q);
		Console.WriteLine(r);

		foreach (SearchResultItem searchResultItem in r.Results) {
			Console.WriteLine($"{searchResultItem} {searchResultItem.Metadata}");
		}
	}

	private static async Task TestEh3()
	{
		var e = new EHentaiEngine();

		var q = await SearchQuery.TryCreateAsync(
			        @"C:\Users\Deci\Pictures\Epic anime\2020_08_2B_Nier_Automata_1_03c.jpg");
		await q.UploadAsync();

		var sc = new SearchConfig()
		{
			ReadCookies = true
		};
		await e.ApplyConfigAsync(sc);

		Console.WriteLine(await e.ApplyCookiesAsync());
		Console.WriteLine($"{e.BaseUrl} {e.IsLoggedIn}");
		var r = await e.GetResultAsync(q);
		Console.WriteLine(r);

		foreach (SearchResultItem searchResultItem in r.Results) {
			Console.WriteLine(searchResultItem);
		}
	}

	private static async Task TestEh2()
	{
		var e = new EHentaiEngine();
		var q = await SearchQuery.TryCreateAsync(@"C:\Users\Deci\Pictures\Art\2020_11_Mai_Shiranui_01a.jpg");
		await q.UploadAsync();
		Console.WriteLine(await e.ApplyCookiesAsync());
		Console.WriteLine($"{e.BaseUrl} {e.IsLoggedIn}");
		var r = await e.GetResultAsync(q);
		Console.WriteLine(r);

		foreach (SearchResultItem searchResultItem in r.Results) {
			Console.WriteLine(searchResultItem);
		}
	}

	private static async Task TestAscii2D()
	{
		var a = new Ascii2DEngine();
		var f = ("C:\\Users\\Deci\\Pictures\\Art\\2020_08_2B_Nier_Automata_1_03c.jpg");
		var q = await SearchQuery.TryCreateAsync(f);
		await q.UploadAsync();
		var r = await a.GetResultAsync(q);

		foreach (SearchResultItem searchResultItem in r.Results) {
			Console.WriteLine(searchResultItem);
		}
	}

	static async Task<IFlurlResponse> send()
	{
		var now  = DateTime.Now.ToBinary();
		var rg   = BitConverter.GetBytes(now);
		var hash = SHA256.HashData(rg);

		var u = "https://api.koromo.xyz/top?offset=0&count=100&month=9".WithHeaders(new
		{
			v_token = now,
			v_hash  = hash,
		});

		return await u.GetAsync();
	}

	private static async Task test5()
	{
		var q = await SearchQuery.TryCreateAsync("https://i.imgur.com/QtCausw.png");
		await q.UploadAsync();
		var e = new RepostSleuthEngine();
		var r = await e.GetResultAsync(q);
		Console.WriteLine($"{r}");

		foreach (var r1 in r.Results) {
			Console.WriteLine(r1);
		}
	}

	private static async Task print(IFlurlResponse r)
	{
		CookieJar j;

		foreach ((string Name, string Value) header in r.Headers) {
			Console.WriteLine($"{header}");
		}

		Console.WriteLine(await r.GetStringAsync());

		foreach (FlurlCookie flurlCookie in r.Cookies) {
			Console.WriteLine(flurlCookie);
		}
	}

	private static async Task test2()
	{
		var q = await SearchQuery.TryCreateAsync(@"C:\Users\Deci\Pictures\Epic anime\1654086015521.png");
		await q.UploadAsync();
		var e = new SearchClient(new SearchConfig());
		var r = await e.RunSearchAsync(q);
		Console.WriteLine($"{r}");

		foreach (var r1 in r) {
			Console.WriteLine(r1);

			Console.ReadKey();

			foreach (SearchResultItem sri in r1.Results) {
				Console.WriteLine(sri);

			}
		}
	}

	private static async Task test4()
	{
		var q = await SearchQuery.TryCreateAsync("https://i.imgur.com/QtCausw.png");
		await q.UploadAsync();
		var e = new IqdbEngine();
		var r = await e.GetResultAsync(q);
		Console.WriteLine($"{r}");

		foreach (var r1 in r.Results) {
			Console.WriteLine(r1);
		}
	}

	private static async Task Test1()
	{
		var rg = new[] { "https://i.imgur.com/QtCausw.png", @"C:\Users\Deci\Pictures\Test Images\Test2.jpg" };

		foreach (string s in rg) {
			using var q = await SearchQuery.TryCreateAsync(s);

			var cfg = new SearchConfig() { };
			var sc  = new SearchClient(cfg);

			// Console.WriteLine(ImageQuery.TryCreate(u,out var q));

			var u = await q.UploadAsync();
			Console.WriteLine($"{q}");

			var res = await sc.RunSearchAsync(q);

			foreach (SearchResult searchResult in res) {

				Console.WriteLine($"{searchResult}");

				foreach (var sri in searchResult.Results) {
					Console.WriteLine($"\t{sri}");
				}
			}
		}
	}

	static async Task test3()
	{
		var u1 = @"C:\Users\Deci\Pictures\Test Images\Test6.jpg";
		var q  = await SearchQuery.TryCreateAsync(u1);
		await q.UploadAsync();

		var engine = new SauceNaoEngine() { };
		engine.Authentication = "362e7e82bc8cf7f6025431fbf3006510057298c3";
		var task = engine.GetResultAsync(q);

		var engine2 = new IqdbEngine();
		var task2   = engine2.GetResultAsync(q);

		var tasks = new[] { task, task2 };
		Task.WaitAny(tasks);

		Console.WriteLine("waiting");
		var result = await task;

		Console.WriteLine(">> {0}", result);
		var result2 = await task2;
		Console.WriteLine(">> {0}", result2);
	}

	#endregion

}