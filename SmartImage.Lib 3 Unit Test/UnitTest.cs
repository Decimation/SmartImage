using System.Diagnostics;
using Flurl.Http;
using NUnit.Framework;
using SmartImage.Lib.Engines.Search;
using Assert = NUnit.Framework.Assert;
using TestContext = NUnit.Framework.TestContext;

namespace SmartImage.Lib.Unit_Test;

[SetUpFixture]
public class SetupTrace
{
	[OneTimeSetUp]
	public void StartTest()
	{
		Trace.Listeners.Add(new ConsoleTraceListener());
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
		@"https://i.imgur.com/QtCausw.png",
		@"C:\Users\Deci\Pictures\Test Images\Test3.png",
		@"https://data19.kemono.party/data/cd/ef/cdef8267d679a9ee1869d5e657f81f7e971f0f401925594fb76c8ff8393db7bd.png?f=Yelan2.png"
	};

	public static object[] _rg2 =
	{
		@"C:\Users\Deci\Pictures\Test Images\Test3.png"
	};

	public static object[] _rg3 =
	{
		@"https://imgur.com/QtCausw"
	};

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
		var r  = await se.GetResultAsync(sq);
		Assert.True(r.Results.Any());

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
}

[TestFixture]
public class UnitTest2
{
	public static object[] _rg = UnitTest._rg;
	public static object[] _rg3 = UnitTest._rg3;

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
		catch (Exception e) {
			Assert.Pass();
		}

	}
}