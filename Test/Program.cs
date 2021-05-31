﻿using System;
using System.Diagnostics;
using System.Linq;
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
	/* 
	 * >>> SmartImage.Lib <<<
	 *
	 *
	 * - SearchClient is used to run advanced searches and utilizes multiple engines
	 * - Individual engines can also be used
	 *
	 */


	public static class Program
	{
		public static void OnResult(object _, SearchResultEventArgs e)
		{

			if (e.Result.IsSuccessful) {
				Console.WriteLine(e.Result);
			}
		}

		public static async Task Main(string[] args)
		{
			Console.OutputEncoding = Encoding.Unicode;
			Console.InputEncoding  = Encoding.Unicode;


			//var q = new ImageQuery(@"C:\Users\Deci\Pictures\Test Images\Test4.png");
			//var q = new ImageQuery("https://i.imgur.com/QtCausw.jpg");
			var q = new ImageQuery("https://litter.catbox.moe/5yr86t.jpg");

			var cfg = new SearchConfig
			{ Query = q, SearchEngines = SearchEngineOptions.All };

			var cl = new SearchClient(cfg);

			await cl.RunSearchAsync();
			Console.WriteLine($"hi {cl.Results.Count}");
			var sw = Stopwatch.StartNew();
			var r2 = cl.FindBestResults(5);
			sw.Stop();
			Console.WriteLine(sw.Elapsed.TotalSeconds);
			foreach (var imageResult in r2) {
				Console.WriteLine(imageResult);
			}

			////var r  = cl.Maximize(r => r.PrimaryResult.Similarity);
			////var r2 = await r;

			//cl.ResultCompleted += OnResult;

			//var r  =  cl.RunSearchAsync();
			//await r;


			//var cfg = new SearchConfig {Query = q, SearchEngines = SearchEngineOptions.All};

			//var cl = new SearchClient(cfg);

			//cl.ResultCompleted += OnResult;
			//var r = cl.RunSearchAsync();
			//await r;

			//var i  = new YandexEngine();
			//var i2 = i.GetResultAsync(q);
			//var r2 = await i2;

			//Console.WriteLine(r2);


			//Console.WriteLine(">> {0}", r2);


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