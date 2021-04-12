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
using SmartImage.Lib.Searching;

namespace Test
{
	public static class Program
	{
		public static async Task Main(string[] args)
		{
			Console.OutputEncoding = Encoding.Unicode;

			var q  = new ImageQuery(@"C:\Users\Deci\Pictures\fucking_epic.jpg");
			var q2 = new ImageQuery("https://i.imgur.com/QtCausw.jpg");
			var q3 = new ImageQuery(@"C:\Users\Deci\RiderProjects\SmartImage\SmartImage\Test4.png");


			var cfg = new SearchConfig() {Query = q, SearchEngines = SearchEngineOptions.All};

			var cl = new SearchClient(cfg);

			Console.WriteLine("Search 1");

			var r = cl.RunSearchAsync();
			await r;

			foreach (var result in cl.Results) {
				Console.WriteLine(result);
			}

			Console.WriteLine("--");

			var i  = new IqdbEngine();
			var i2 = i.GetResultAsync(q2);
			var r2 = await i2;

			Console.WriteLine(r2);

			cl.Reset();

			Console.WriteLine("Search 2");

			r = cl.RunSearchAsync();
			await r;
			foreach (var result in cl.Results)
			{
				Console.WriteLine(result);
			}

		}
	}
}