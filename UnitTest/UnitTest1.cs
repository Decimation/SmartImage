using System;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Xsl;
using NUnit.Framework;
using SmartImage.Lib.Engines.Impl;
using SmartImage.Lib.Searching;
using SmartImage.Lib.Utilities;

// ReSharper disable UnusedMember.Local
#pragma warning disable IDE0051, IDE0052
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

			if (t.Status == ResultStatus.Unavailable) {
				Assert.Inconclusive();
			}

			t.Consolidate();


			var a = t.PrimaryResult.Artist.Contains(name);


			var b = t.OtherResults.Any(r =>
			{
				if (r.Artist != null) {
					return r.Artist.Contains(name);

				}
				else {
					return false;
				}
			});
			Assert.True(a || b);


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
				return r.DetailScore >= 3 && r.Site!=null;
			});

			Assert.True(a || b);


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

			Assert.True(t.OtherResults.Any(r => r.Source.Contains(name, StringComparison.InvariantCultureIgnoreCase)));


		}
	}
}