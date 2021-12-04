using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Xsl;
using NUnit.Framework;
using SmartImage.Lib.Engines.Impl;
using SmartImage.Lib.Searching;
using SmartImage.Lib.Utilities;

// ReSharper disable UnusedMember.Local
#pragma warning disable IDE0051, IDE0052
namespace UnitTest;

public class Tests
{
	[SetUp]
	public void Setup()
	{
		var pictures   = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
		var path = Path.Combine(pictures, "Test Images");

		TestImages = Directory.GetFiles(path).Union(new[]
		{
			"https://i.imgur.com/QtCausw.jpg"
		}).ToList();
	}

	private static List<string> TestImages { get; set; }

	[Test]
	public void TestImageQuery()
	{
		//Assert.That(() => new ImageQuery("https://imgur.com/QtCausw"), Throws.Exception);

		Assert.Throws<ArgumentException>(() => new ImageQuery("https://imgur.com/QtCausw"));
		Assert.DoesNotThrow(() => new ImageQuery("https://i.imgur.com/QtCausw.png"));
	}

	[Test]
	[TestCase("https://i.imgur.com/QtCausw.png", true)]
	[TestCase("https://twitter.com/sciamano240/status/1186775807655587841", false)]
	public void TestImageHelper(string s, bool b)
	{
		Assert.AreEqual(ImageHelper.IsImage(s, out _), b);

	}

	[Test]
	public void TestResolutionType()
	{
		var i = Image.FromFile(@"C:\Users\Deci\Pictures\Test Images\Test1.jpg");
		var (w, h) = (i.Width, i.Height);
		Assert.AreEqual(ImageHelper.GetDisplayResolution(w, h), DisplayResolutionType.HD);
		Assert.AreEqual(ImageHelper.GetDisplayResolution(1920, 1080), DisplayResolutionType.FHD);
		Assert.AreEqual(ImageHelper.GetDisplayResolution(640, 360), DisplayResolutionType.nHD);
	}

	[Test]
	[TestCase(@"C:\Users\Deci\Pictures\Test Images\Test1.jpg", "sciamano240")]
	[TestCase(@"C:\Users\Deci\Pictures\Test Images\Test2.jpg", "koyoriin")]
	public async Task TestSauceNao(string art, string name)
	{
		var q  = new ImageQuery(art);
		var i  = new SauceNaoEngine();
		var rt = i.GetResultAsync(q);
		var t  = await rt;

		if (t.Status == ResultStatus.Cooldown) {
			Assert.Inconclusive();
		}

		


		var b = t.AllResults.Any(r =>
		{
			if (r.Artist != null) {
				return r.Artist.Contains(name);

			}
			else {
				return false;
			}
		});

		Assert.True( b);


	}

	[Test]
	[TestCase(@"C:\Users\Deci\Pictures\Test Images\Test1.jpg")]
	[TestCase(@"C:\Users\Deci\Pictures\Test Images\Test2.jpg")]
	public async Task TestIqdb(string art)
	{
		var q  = new ImageQuery(art);
		var i  = new IqdbEngine();
		var rt = i.GetResultAsync(q);
		var t  = await rt;

		if (t.Status == ResultStatus.Unavailable) {
			Assert.Inconclusive();
		}

		//t.Consolidate();


		var a = t.IsNonPrimitive;


		var b = t.OtherResults.Any(r =>
		{
			return r.DetailScore >= 3 && r.Site != null;
		});

		Assert.True(a || b);


	}

	[Test]
	[TestCase(@"C:\Users\Deci\Pictures\Test Images\Test1.jpg")]
	[TestCase(@"C:\Users\Deci\Pictures\Test Images\Test2.jpg")]
	public async Task TestAscii2D(string art)
	{
		var q  = new ImageQuery(art);
		var i  = new Ascii2DEngine();
		var rt = i.GetResultAsync(q);
		var t  = await rt;


		var a = t.IsNonPrimitive;


		var b = t.OtherResults.Any(r =>
		{
			return r.DetailScore >= 3 && r.Site != null;
		});

		Assert.True(a || b);


	}

	[Test]
	[TestCase(@"C:\Users\Deci\Pictures\Test Images\Test4.png", "Serial Experiments")]
	[TestCase(@"C:\Users\Deci\Pictures\Test Images\Test3.png", "Neon Genesis")]
	public async Task TestTraceMoe(string screenshot, string name)
	{
		var q  = new ImageQuery(screenshot);
		var i  = new TraceMoeEngine();
		var rt = i.GetResultAsync(q);
		var t  = await rt;

		Assert.True(t.OtherResults.Any(r => r.Source.Contains(name, StringComparison.InvariantCultureIgnoreCase)));


	}
}