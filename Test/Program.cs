using System;
using System.Diagnostics;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading.Tasks;
using RestSharp;
using SimpleCore.Utilities;
using SmartImage.Lib;
using SmartImage.Lib.Engines;
using SmartImage.Lib.Engines.Impl;
using SmartImage.Lib.Engines.Impl.Other;
using SmartImage.Lib.Searching;

namespace Test
{
	public static class Program
	{
		public static void OnResult(object _, SearchClient.SearchResultEventArgs e)
		{
			Console.WriteLine(">>" + e.Result);
		}

		public static async Task Main(string[] args)
		{
			Console.OutputEncoding = Encoding.Unicode;

			var q  = new ImageQuery(@"C:\Users\Deci\Pictures\Test Images\Small1.png");
			var q2 = new ImageQuery("https://i.imgur.com/QtCausw.jpg");
			var i  = new Ascii2DEngine();
			var i2 = i.GetResultAsync(q2);
			var r2 = await i2;

			Console.WriteLine(">> {0}", r2);

			foreach (var result in r2.OtherResults) {
				Console.WriteLine($">{result}");
			}
			// var cfg = new SearchConfig() {Query = q, SearchEngines = SearchEngineOptions.All};
			//
			// var cl = new SearchClient(cfg);
			//
			// Console.WriteLine("Search 1");
			// cl.ResultCompleted += OnResult;
			// var r = cl.RunSearchAsync();
			// await r;

			/*foreach (var result in cl.Results) {
				Console.WriteLine(result);
			}

			Console.WriteLine("--");

			var i  = new IqdbEngine();
			var i2 = i.GetResultAsync(q2);
			var r2 = await i2;

			Console.WriteLine(">> {0}",r2);

			cl.Reset();

			Console.WriteLine("Search 2");

			r = cl.RunSearchAsync();
			await r;
			foreach (var result in cl.Results)
			{
				Console.WriteLine(result);
			}*/

		}
	}
}