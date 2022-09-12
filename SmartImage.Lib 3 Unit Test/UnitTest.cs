using System.Diagnostics;
using NUnit.Framework;
using SmartImage.Lib.Engines.Impl;
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
public class UnitTest
{
	[Test]
	[TestCase(@"C:\Users\Deci\Pictures\Test Images\Test1.jpg")]
	[TestCase(@"https://i.imgur.com/QtCausw.png")]
	public async Task SauceNao_Test(string s)
	{
		var sq = await SearchQuery.TryCreateAsync(s);
		var u  = await sq.UploadAsync();
		var se = new SauceNaoEngine();
		var r  = await se.GetResultAsync(sq);
		Assert.True(r.Results.Any());

		foreach (var x in r.Results) {
			TestContext.WriteLine(x);
		}
	}

	[Test]
	[TestCase(@"C:\Users\Deci\Pictures\Test Images\Test1.jpg")]
	[TestCase(@"https://i.imgur.com/QtCausw.png")]
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
	[TestCase(@"C:\Users\Deci\Pictures\Test Images\Test1.jpg")]
	[TestCase(@"https://i.imgur.com/QtCausw.png")]
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
	[TestCase(@"C:\Users\Deci\Pictures\Test Images\Test1.jpg")]
	[TestCase(@"https://i.imgur.com/QtCausw.png")]
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
	[TestCase(@"C:\Users\Deci\Pictures\Test Images\Test3.png")]
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