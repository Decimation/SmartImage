using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using AngleSharp.Html.Parser;
using Flurl.Http;
using Kantan.Net.Utilities;
using Microsoft.ClearScript.V8;
using SmartImage.Lib;
using SmartImage.Lib.Engines.Search;

#pragma warning disable IDE0079
#pragma warning disable CS0168, CS1998
#pragma warning disable IDE0060, IDE1006, IDE0051,IDE0059

namespace SmartImage.Lib_Test;
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
	public static async Task Main(string[] args)
	{
		var f = @"C:\Users\Deci\Pictures\Test Images\Test6.jpg";
		var e = new EHentaiEngine();

		//test cookies
		var dict = new Dictionary<string, string>()
		{
			["igneous"] = "388bd84ac",
			["ipb_member_id"]= "3200336",
			["ipb_pass_hash"]= "52e494963cba3c6f072a2d2be88a18a8",
			["sk"]= "utrq4k3ddevkgnj4fc8163qzq6gz"
		};

		/*
		 *
		 */

		var res = await e.Search(File.OpenRead(f), dict);

	}

	private static async Task test5()
	{
		var q = await SearchQuery.TryCreateAsync("https://i.imgur.com/QtCausw.png");
		await q.UploadAsync();
		var e = new RepostSleuthEngine();
		var r = await e.GetResultAsync(q);
		Console.WriteLine($"{r}");

		foreach (var r1 in r.Results) {
			Console.WriteLine(r1);
		}
	}

	private static async Task print(IFlurlResponse r)
	{
		CookieJar j;

		foreach ((string Name, string Value) header in r.Headers) {
			Console.WriteLine($"{header}");
		}

		Console.WriteLine(await r.GetStringAsync());

		foreach (FlurlCookie flurlCookie in r.Cookies) {
			Console.WriteLine(flurlCookie);
		}
	}

	private static async Task test4()
	{
		var q = await SearchQuery.TryCreateAsync("https://i.imgur.com/QtCausw.png");
		await q.UploadAsync();
		var e = new IqdbEngine();
		var r = await e.GetResultAsync(q);
		Console.WriteLine($"{r}");

		foreach (var r1 in r.Results) {
			Console.WriteLine(r1);
		}
	}

	private static async Task Test1()
	{
		var rg = new[] { "https://i.imgur.com/QtCausw.png", @"C:\Users\Deci\Pictures\Test Images\Test2.jpg" };

		foreach (string s in rg) {
			using var q = await SearchQuery.TryCreateAsync(s);

			var cfg = new SearchConfig() { };
			var sc  = new SearchClient(cfg);

			// Console.WriteLine(ImageQuery.TryCreate(u,out var q));

			var u = await q.UploadAsync();
			Console.WriteLine($"{q}");

			var res = await sc.RunSearchAsync(q);

			foreach (SearchResult searchResult in res) {

				Console.WriteLine($"{searchResult}");

				foreach (var sri in searchResult.Results) {
					Console.WriteLine($"\t{sri}");
				}
			}
		}
	}

	static async Task test3()
	{
		var u1 = @"C:\Users\Deci\Pictures\Test Images\Test6.jpg";
		var q  = await SearchQuery.TryCreateAsync(u1);
		await q.UploadAsync();

		var engine = new SauceNaoEngine() { };
		engine.Authentication = "362e7e82bc8cf7f6025431fbf3006510057298c3";
		var task = engine.GetResultAsync(q);

		var engine2 = new IqdbEngine();
		var task2   = engine2.GetResultAsync(q);

		var tasks = new[] { task, task2 };
		Task.WaitAny(tasks);

		Console.WriteLine("waiting");
		var result = await task;

		Console.WriteLine(">> {0}", result);
		var result2 = await task2;
		Console.WriteLine(">> {0}", result2);
	}
}