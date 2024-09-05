global using ISImage = SixLabors.ImageSharp.Image;
global using Assert = NUnit.Framework.Legacy.ClassicAssert;
global using CollectionAssert = NUnit.Framework.Legacy.CollectionAssert;
global using StringAssert = NUnit.Framework.Legacy.StringAssert;
global using DirectoryAssert = NUnit.Framework.Legacy.DirectoryAssert;
global using FileAssert = NUnit.Framework.Legacy.FileAssert;

// using SearchQuery = SmartImage.Lib.SearchQuery2;
using TestContext = NUnit.Framework.TestContext;
using System.Diagnostics;
using System.Json;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text.Json;
using AngleSharp.Html.Parser;
using Kantan.Net;
using Kantan.Text;
using Novus.FileTypes;
using Novus.FileTypes.Uni;
using NUnit.Framework;
using SixLabors.ImageSharp.Processing;
using SmartImage.Lib.Engines;
using SmartImage.Lib.Engines.Impl.Search;
using SmartImage.Lib.Results;
using Flurl.Http;
using Kantan.Utilities;
using Novus.Streams;
using SmartImage.Lib.Clients;
using SmartImage.Lib.Utilities;
using SmartImage.Rdx;
using SmartImage.Rdx.Shell;
using SmartImage.Lib.Images;
using Spectre.Console.Cli;
using Spectre.Console.Testing;
using SmartImage.Lib.Images.Uni;

// ReSharper disable AccessToStaticMemberViaDerivedType

// using Assert = NUnit.Framework.Assert;

#pragma warning disable CS8602 // Dereference of a possibly null reference.
#nullable disable

// ReSharper disable InconsistentNaming
#pragma warning disable IDE0059, CA2211, IDE0002
namespace SmartImage.Lib.Unit_Test;

// # Test values
/*
 *
 * https://i.imgur.com/QtCausw.png
 */

[SetUpFixture]
public class SetupTrace
{

	[OneTimeSetUp]
	public void StartTest()
	{
		Trace.Listeners.Add(new ConsoleTraceListener());
		Trace.Listeners.Add(new DefaultTraceListener());
	}

	[OneTimeTearDown]
	public void EndTest()
	{
		Trace.Flush();
	}

}

[TestFixture]
[Parallelizable]
public class UnitTest
{

	public static object[] _rg =
	{
		@"C:\Users\Deci\Pictures\Test Images\Test1.jpg",
		@"C:\Users\Deci\Pictures\Test Images\Test2.jpg",
		@"C:\Users\Deci\Pictures\Test Images\Test3.png",
		@"C:\Users\Deci\Pictures\Test Images\Test4.png",
		@"C:\Users\Deci\Pictures\Test Images\Test6.jpg",
		@"https://i.imgur.com/QtCausw.png",
		@"https://files.catbox.moe/fcpe1e.jpg",

		// @"https://data19.kemono.party/data/cd/ef/cdef8267d679a9ee1869d5e657f81f7e971f0f401925594fb76c8ff8393db7bd.png?f=Yelan2.png",
		"https://i.imgur.com/zoBIh8t.jpg",

		// @"https://litter.catbox.moe/ieafze.png",
		@"C:\Users\Deci\Pictures\Epic anime\__sashou_mihiro_original_drawn_by_infinote__ec1ebb276d934ba6ce9d03a08d02f7d9.png",
	};

	public static BaseSearchEngine[] _rg2x = BaseSearchEngine.GetSelectedEngines(SearchEngineOptions.All)
		.Where(x => x.IsAdvanced).ToArray();

	public static object[] _rg2 =
	{
		@"C:\Users\Deci\Pictures\Test Images\Test3.png"
	};

	public static object[] _rg4 =
	{
		@"C:\Users\Deci\Pictures\01d72650be90e83a8a68ab51330271669f793295b1e7ff4cc459af7ed3fb3962.png",
		@"C:\Users\Deci\Pictures\Test Images\Test1.jpg",
		@"C:\Users\Deci\Pictures\Test Images\Test2.jpg",
		@"C:\Users\Deci\Pictures\Test Images\Test3.png",
		@"C:\Users\Deci\Pictures\Test Images\Test4.png",
		@"C:\Users\Deci\Pictures\Test Images\Test6.jpg",
	};

	public static object[] _rg3 =
	{
		@"https://imgur.com/QtCausw",
		@"https://i.redd.it/9h88i5kue9z31.jpg"
	};

	public const string Username = "Decimation001x";
	public const string Password = "minecraft!";

	/*[Test]
	public async Task Test1x([ValueSource(nameof(_rg))] string s, [ValueSource(nameof(_rg2x))] BaseSearchEngine se)
	{
		var sq = await SearchQuery.TryCreateAsync(s);

		var u  = await sq.UploadAsync();

		var r  = await se.GetResultAsync(sq);

		if (r.Status is SearchResultStatus.IllegalInput or SearchResultStatus.NoResults) {
			Assert.Inconclusive();
		}

		Assert.True(r.Results.Any());

		foreach (var x in r.Results) {
			TestContext.WriteLine(x);
		}
	}*/

	[Test]
	[TestCaseSource(nameof(_rg))]
	public async Task SauceNao_Test(string s)
	{
		var sq = await SearchQuery.TryCreateAsync(s);
		var u  = await sq.UploadAsync();
		var se = new SauceNaoEngine();
		var r  = await se.GetResultAsync(sq);

		if (r.Status == SearchResultStatus.Cooldown) {
			Assert.Inconclusive();
		}

		Assert.True(r.Results.Any());

		foreach (var x in r.Results) {
			TestContext.WriteLine(x);
		}
	}

	[Test]
	[TestCaseSource(nameof(_rg))]
	public async Task Iqdb_Test(string s)
	{
		var sq = await SearchQuery.TryCreateAsync(s);

		var u  = await sq.UploadAsync();
		var se = new IqdbEngine();
		var r  = await se.GetResultAsync(sq);

		if (r.Status is SearchResultStatus.IllegalInput or SearchResultStatus.NoResults) {
			Assert.Inconclusive();
		}

		Assert.True(r.Results.Any());

		foreach (var x in r.Results) {
			TestContext.WriteLine(x);
		}
	}

	[Test]
	[TestCaseSource(nameof(_rg))]
	public async Task Ascii2D_Test(string s)
	{
		var sq = await SearchQuery.TryCreateAsync(s);

		var u  = await sq.UploadAsync();
		var se = new Ascii2DEngine();

		// se.Timeout = TimeSpan.MaxValue;

		SearchResult r = null;

		Assert.DoesNotThrowAsync(async () =>
		{
			r = await se.GetResultAsync(sq);

		});

		if (r.Status is SearchResultStatus.IllegalInput or SearchResultStatus.NoResults) {
			Assert.Inconclusive();

		}
		else {
			Assert.True(r.Results.Any());

		}

		foreach (var x in r.Results) {
			TestContext.WriteLine(x);
		}
	}

	[Test]
	[TestCaseSource(nameof(_rg))]
	public async Task Yandex_Test(string s)
	{
		var sq = await SearchQuery.TryCreateAsync(s);
		var u  = await sq.UploadAsync();
		var se = new YandexEngine();
		var r  = await se.GetResultAsync(sq);

		if (r.Status is SearchResultStatus.IllegalInput or SearchResultStatus.NoResults) {
			Assert.Inconclusive();
		}

		Assert.True(r.Results.Any());

		foreach (var x in r.Results) {
			TestContext.WriteLine(x);
		}
	}

	[Test]
	[TestCaseSource(nameof(_rg4))]
	public async Task Fluffle_Test(string s)
	{
		var sq = await SearchQuery.TryCreateAsync(s);
		var u  = await sq.UploadAsync();
		var se = new FluffleEngine();
		var r  = await se.GetResultAsync(sq);

		if (r.Status is SearchResultStatus.IllegalInput or SearchResultStatus.NoResults) {
			Assert.Inconclusive();
		}

		Assert.True(r.Results.Any());

		foreach (var x in r.Results) {
			TestContext.WriteLine(x);
		}
	}

	[Test]
	[TestCaseSource(nameof(_rg))]
	public async Task RP_Test(string s)
	{
		var sq = await SearchQuery.TryCreateAsync(s);
		var u  = await sq.UploadAsync();
		var se = new RepostSleuthEngine();
		var r  = await se.GetResultAsync(sq);

		if (r.Status is SearchResultStatus.IllegalInput or SearchResultStatus.NoResults) {
			Assert.Inconclusive();
		}

		Assert.True(r.Results.Any());

		foreach (var x in r.Results) {
			TestContext.WriteLine(x);
		}
	}

	[Test]
	[TestCaseSource(nameof(_rg2))]
	public async Task TraceMoe_Test(string s)
	{
		var sq = await SearchQuery.TryCreateAsync(s);
		var u  = await sq.UploadAsync();
		var se = new TraceMoeEngine();
		var r  = await se.GetResultAsync(sq);
		Assert.True(r.Results.Any());

		foreach (var x in r.Results) {
			TestContext.WriteLine(x);
		}
	}

	[Test]
	[TestCaseSource(nameof(_rg))]
	public async Task ArchiveMoe_Test(string s)
	{
		var sq = await SearchQuery.TryCreateAsync(s);
		var u  = await sq.UploadAsync();
		var se = new ArchiveMoeEngine();
		var r  = await se.GetResultAsync(sq);

		if (r.Status == SearchResultStatus.NoResults) {
			Assert.Inconclusive();
		}

		Assert.True(r.Results.Any());

		foreach (var x in r.Results) {
			TestContext.WriteLine(x);
		}
	}

	/*[Test]
	[TestCaseSource(nameof(_rg))]
	public async Task RepostSleuth_Test(string s)
	{
		var sq = await SearchQuery.TryCreateAsync(s);
		var u  = await sq.UploadAsync();
		var se = new RepostSleuthEngine();
		var r  = await se.GetResultAsync(sq);

		if (r.Status == SearchResultStatus.NoResults) {
			Assert.Inconclusive();
		}

		Assert.True(r.Results.Any());

		foreach (var x in r.Results) {
			TestContext.WriteLine(x);
		}
	}*/

	[Test]
	public async Task EHentai_Test()
	{
		var sq = await SearchQuery.TryCreateAsync("C:\\Users\\Deci\\Pictures\\Art\\2020_08_2B_Nier_Automata_1_03c.jpg");
		var u  = await sq.UploadAsync();

		var e = new EHentaiEngine()
			{ };
		await e.ApplyCookiesAsync();

		var r = await e.GetResultAsync(sq);
		Assert.True(r.Results.Any());

		foreach (var x in r.Results) {
			TestContext.WriteLine(x);
		}
	}

}

[TestFixture]
public class UnitTest2
{

	public static readonly object[] _rg  = UnitTest._rg;
	public static readonly object[] _rg3 = UnitTest._rg3;

	[Test]
	[TestCaseSource(nameof(_rg))]
	public async Task SearchQuery_Test(string s)
	{
		var sq = await SearchQuery.TryCreateAsync(s);
		Assert.IsNotNull(sq);
		var u = await sq.UploadAsync();

		Assert.IsNotNull(u);
		TestContext.WriteLine(sq);
	}

	[Test]
	[TestCaseSource(nameof(_rg3))]
	public async Task SearchQuery_Test2(string s)
	{
		try {
			var sq = await SearchQuery.TryCreateAsync(s);
			Assert.Fail();
		}
		catch (Exception) {
			Assert.Pass();
		}

	}

	[Test]
	[TestCaseSource(nameof(_rg))]
	public async Task Test3(string s)
	{
		var o  = await UniSource.TryGetAsync(s);
		var o2 = await SearchQuery.TryCreateAsync(s);
		Assert.Pass();
	}

	[TestCase(
		"https://cdn.donmai.us/original/c7/c0/__akagi_and_akagi_azur_lane_drawn_by_sciamano240__c7c077986787625112bb19a912d1b7a8.jpg?download=1")]
	public async Task Test4(string s)
	{
		var o = await SearchQuery.TryCreateAsync(s);
		TestContext.WriteLine($"{o}");
	}

}

[TestFixture]
public class UnitTest3
{

	[Test]
	public async Task Eh()
	{
		var e = new EHentaiEngine();
		Assert.IsFalse(e.IsLoggedIn);
		Assert.AreEqual(e.BaseUrl, EHentaiEngine.EHentaiBase);

		var ok = await e.ApplyCookiesAsync();
		Assert.IsTrue(ok);
		Assert.IsTrue(e.IsLoggedIn);

		Assert.AreEqual(e.BaseUrl, EHentaiEngine.ExHentaiBase);

		// Assert.IsFalse(e.IsLoggedIn);
	}

}

[TestFixture]
public class UnitTest4
{

	static UnitTest4()
	{
		RuntimeHelpers.RunClassConstructor(typeof(UnitTest).TypeHandle);
	}

	[SetUp]
	public void Setup() { }

	private static IEnumerable<FieldInfo> getfields(Type type)
	{
		return type.GetFields(BindingFlags.GetField | BindingFlags.Static | BindingFlags.NonPublic |
		                      BindingFlags.Public).Where(e => e.IsInitOnly);
	}

	private static readonly object[] _rg  = UnitTest._rg;
	private static readonly object[] _rg2 = UnitTest._rg2;
	private static readonly object[] _rg3 = UnitTest._rg3;
	private static readonly string[] _rg4 = _rg.Cast<string>().Union(_rg2).Cast<string>().ToArray();

	[Test]
	public void TestConfig()
	{

		var cfg = new SearchConfig()
		{
			ReadCookies     = true,
			SearchEngines   = SearchConfig.SE_DEFAULT,
			PriorityEngines = SearchConfig.PE_DEFAULT,
		};

		TestContext.WriteLine($"{SearchConfig.Configuration.AppSettings.File}");

		// cfg.Timeout = TimeSpan.MaxValue;

		// SearchConfig.Save();
		TestContext.WriteLine(cfg);

	}

	[Test]
	[TestCaseSource(nameof(_rg))]
	public async Task Start(string q)
	{

		var cfg = new SearchConfig()
		{
			ReadCookies     = true,
			SearchEngines   = SearchConfig.SE_DEFAULT,
			PriorityEngines = SearchConfig.PE_DEFAULT,
		};
		TestContext.WriteLine($"{SearchConfig.Configuration.FilePath}");

		var cl = new SearchClient(cfg);
		var sq = await SearchQuery.TryCreateAsync(q);

		/*var image = ISImage.Load(sq.Uni.Stream);
		var fmt = image.Metadata.DecodedImageFormat;
		TestContext.WriteLine(fmt);

		var fixedHeight = 256;

		image.Mutate(x =>
		{
			int newWidth = (int)Math.Floor((image.Width / (float)image.Height) * fixedHeight);

			// Resize the image to the new width and fixed height.
			x.Resize(newWidth, fixedHeight);
		});
		TestContext.WriteLine($"{image.Width} {image.Height}");*/

		await sq.UploadAsync();

		if (!sq.IsUploaded) {
			Assert.Inconclusive();
		}

		await cl.LoadEnginesAsync();

		cl.OnSearchComplete += (sender, results) => { };

		var s = new CancellationTokenSource();
		TestContext.WriteLine($"{sq.Uni.Image?.Metadata.DecodedImageFormat}");
		var r = await cl.RunSearchAsync(sq, token: s.Token);

		Assert.IsNotEmpty(r);

		// var e = (EHentaiEngine) cl.TryGetEngine(SearchEngineOptions.EHentai)!;
		// Assert.IsTrue(e.IsLoggedIn);

	}

	/*
	[Test]
	[TestCaseSource(nameof(_rg))]
	public async Task Start2(string q)
	{

		var cfg = new SearchConfig()
		{
			EhPassword      = UnitTest.Password,
			EhUsername      = UnitTest.Username,
			SearchEngines   = SearchConfig.SE_DEFAULT,
			PriorityEngines = SearchConfig.PE_DEFAULT,
		};

		TestContext.WriteLine($"{SearchConfig.Configuration.FilePath}");

		var cl = new SearchClient(cfg);
		var sq = await SearchQuery.TryCreateAsync(q);

		await sq.UploadAsync();

		await cl.LoadEnginesAsync();

		cl.OnSearchComplete += (sender, results) => { };

		var s = new CancellationTokenSource();

		var r = await cl.RunSearchAsync(sq, token: s.Token);

		Assert.IsNotEmpty(r);
		var r2 = await SearchClient.GetDirectImagesAsync(r.SelectMany(e => e.Results));
		Assert.IsNotEmpty(r2);

	}
*/

}

[TestFixture]
public class UnitTest5
{

	public static object[] _rg =
	{
		@"https://www.zerochan.net/2750747",
		@"https://i.imgur.com/QtCausw.png",
		@"https://files.catbox.moe/fcpe1e.jpg",
		"https://i.imgur.com/zoBIh8t.jpg",
		@"https://litter.catbox.moe/ieafze.png",
	};

	[Test]
	[TestCaseSource(nameof(_rg))]
	public async Task Get(string u)
	{
		var img = await ImageScanner.ScanImagesAsync(u);

		foreach (UniImage source in img) {
			TestContext.WriteLine($"{source}");
		}
	}

	[Test]
	[TestCaseSource(nameof(_rg))]
	public async Task Get2(string u)
	{
		var str = await u.AllowAnyHttpStatus().OnError(r =>
		{
			r.ExceptionHandled = true;
		}).GetStringAsync();
		var parser = new HtmlParser();
		var docc   = await parser.ParseDocumentAsync(str);
		var img    = ImageScanner.GetImageUrls(docc, new GenericImageFilter());

		foreach (var source in img) {
			TestContext.WriteLine($"{source}");
		}
	}

}

[TestFixture]
public class UnitTest6
{

	[Test]
	[TestCase(@"C:\Users\Deci\Pictures\Epic anime\59530998_p1.jpg")]
	public async Task Test1(string f)
	{
		string api = "http://127.0.0.1:45871/";
		var    key = "7b00a1959028c6269e8f1a97a38c8a449802ec85215ea072555e95861f2b7185";

		var sq = await SearchQuery.TryCreateAsync(f);

		var h    = new HydrusClient(api, key);
		var hash = HashHelper.Sha256.ToString(await SHA256.HashDataAsync(sq.Uni.Stream));

		sq.Uni.Stream.TrySeek();

		var jsonValue = await h.GetFileHashesAsync(hash);
		Assert.NotNull(jsonValue);

		// Debugger.Break();

		var jsonValue2 = await h.GetFileMetadataAsync(hash);
		Assert.NotNull(jsonValue2);

		// Debugger.Break();

		var jsonValue3 = await h.GetFileRelationshipsAsync(hash);
		Assert.NotNull(jsonValue3);

		// Debugger.Break();

		var vs = ((JsonObject) jsonValue3)["file_relationships"];

		var re = JsonSerializer.Deserialize<Dictionary<string, HydrusFileRelationship>>(vs.ToString());

		var deserialize = HydrusFileRelationship.Deserialize(jsonValue3);

		Assert.NotNull(deserialize);

	}

}

[TestFixture]
public class UnitTest7
{

	[Test]
	[TestCase(SearchEngineOptions.All)]
	[TestCase(SearchEngineOptions.Artwork)]
	[TestCase(SearchEngineOptions.EHentai | SearchEngineOptions.TinEye)]
	public void Test1(SearchEngineOptions options)
	{
		var engines = BaseSearchEngine.GetSelectedEngines(options).ToArray();
		var second  = BaseSearchEngine.GetSelectedEngines(options);

		Assert.True(engines.All(x => second.Contains(x)));
	}

}

[TestFixture]
public class UnitTest8
{

	[Test]
	public void Test1()
	{
		var e     = OutputFields.Url | OutputFields.Similarity | OutputFields.Artist;
		var res   = new SearchResultItem(null) { Url = UnitTest._rg3[0].ToString(), Similarity = 1, Artist = "butt" };
		var vals  = FieldValueMap.Find(res, e);
		var vals2 = vals.Select(x => x.Name);

		foreach (object val in vals) {
			TestContext.WriteLine(val);
		}

		foreach (OutputFields fields in e.GetSetFlags()) {
			Assert.True(vals2.Contains(fields.ToString()));
		}
	}

	[Test]
	public async Task Test()
	{
		var c = new CommandAppTester();
		c.SetDefaultCommand<SearchCommand>();

		c.Configure(x =>
		{
			x.PropagateExceptions();
		});
		var code = await c.RunAsync(new[] { @"C:\Users\Deci\Pictures\Test Images\Test1.jpg" });

	}

}