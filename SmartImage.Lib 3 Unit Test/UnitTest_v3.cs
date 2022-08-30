using NUnit.Framework;
using SmartImage_3.Lib;
using System.Diagnostics;
using SmartImage_3.Lib.Engines;
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

		foreach (var x in r.Results)
		{
			TestContext.WriteLine(x);
		}
	}
}