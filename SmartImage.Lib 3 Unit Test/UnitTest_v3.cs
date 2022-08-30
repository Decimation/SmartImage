using NUnit.Framework;
using SmartImage_3.Lib;
using System.Diagnostics;
using SmartImage.Lib.Engines.Search;
using SmartImage_3.Lib.Engines;
using SmartImage_3.Lib.Engines.Impl;
using Assert = NUnit.Framework.Assert;
using TestContext = NUnit.Framework.TestContext;

namespace SmartImage.Lib_3_Unit_Test;

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
public class UnitTest_v3
{
	[Test]
	[TestCase(@"C:\Users\Deci\Pictures\Test Images\Test1.jpg")]
	[TestCase(@"https://i.imgur.com/QtCausw.png")]
	public async Task Test1(string s)
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
	public async Task Test2(string s)
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
	public async Task Test3(string s)
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
	public async Task Test5(string s)
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
	public async Task Test4(string s)
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