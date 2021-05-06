using System;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Xsl;
using NUnit.Framework;
using SmartImage.Lib.Engines.Impl;
using SmartImage.Lib.Searching;
using SmartImage.Lib.Utilities;

namespace UnitTest
{
	public class Tests
	{
		[SetUp]
		public void Setup() { }

		private static readonly string[] TestImages =
		{
			@"C:\Users\Deci\Pictures\Test Images\Test1.jpg",
			@"C:\Users\Deci\Pictures\Test Images\Test2.jpg",
			@"C:\Users\Deci\Pictures\Test Images\Test3.png",
			@"C:\Users\Deci\Pictures\Test Images\Test4.png",

			@"C:\Users\Deci\Pictures\fucking_epic.jpg",

			"https://i.imgur.com/QtCausw.jpg",

			@"C:\Users\Deci\Pictures\Test Images\Small1.png",
			@"C:\Users\Deci\Pictures\Test Images\Small2.png"
		};

		[Test]
		public void TestImageQuery()
		{
			//Assert.That(() => new ImageQuery("https://imgur.com/QtCausw"), Throws.Exception);

			Assert.Throws<ArgumentException>(() => new ImageQuery("https://imgur.com/QtCausw"));
			Assert.DoesNotThrow(() => new ImageQuery("https://i.imgur.com/QtCausw.png"));
		}

		[Test]
		public void TestResolutionType()
		{
			var (w, h) = ImageUtilities.GetResolution(@"C:\Users\Deci\Pictures\Test Images\Test1.jpg");
			Assert.AreEqual(ImageUtilities.GetDisplayResolution(w,h), DisplayResolutionType.HD);
			Assert.AreEqual(ImageUtilities.GetDisplayResolution(1920, 1080), DisplayResolutionType.FHD);
			Assert.AreEqual(ImageUtilities.GetDisplayResolution(640,360), DisplayResolutionType.nHD);
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


			Assert.True(t.OtherResults.Any(r => r.Artist.Contains(name)));


		}

		[Test]
		[TestCase(@"C:\Users\Deci\Pictures\Test Images\Test4.png", "Serial Experiments")]
		[TestCase(@"C:\Users\Deci\Pictures\Test Images\Test3.png", "Neon Genesis")]
		public async Task TestAnime(string screenshot, string name)
		{
			var q  = new ImageQuery(screenshot);
			var i  = new TraceMoeEngine();
			var rt = i.GetResultAsync(q);
			var t  = await rt;

			Assert.True(t.OtherResults.Any(r => r.Source.Contains(name)));


		}
	}
}