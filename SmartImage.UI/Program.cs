using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using SmartImage.Lib;
using SmartImage.Lib.Engines;
using SmartImage.Lib.Searching;
using SmartImage.Lib.Utilities;

namespace SmartImage.UI
{
	public static class Program
	{
		public static void OnResult(object _, SearchClient.SearchResultEventArgs e)
		{

			if (e.Result.IsSuccessful) {
				Console.WriteLine(e.Result);

				if (e.Result.PrimaryResult.Url!=null) {
					ImageUtilities.scan(e.Result.PrimaryResult.Url.ToString());
				}
			}
		}

		private static async Task Main(string[] args)
		{
			//var process = Process.GetCurrentProcess();
			//process.PriorityBoostEnabled = true;
			//process.PriorityClass        = ProcessPriorityClass.High;

			var i = Console.ReadLine();

			Console.OutputEncoding = Encoding.Unicode;
			Console.InputEncoding  = Encoding.Unicode;

			var sw = Stopwatch.StartNew();

			ImageQuery q = (i);


			var cfg = new SearchConfig
				{Query = q, SearchEngines = SearchEngineOptions.All};

			var cl = new SearchClient(cfg);

			//var r  = cl.Maximize(r => r.PrimaryResult.Similarity);
			//var r2 = await r;

			cl.ResultCompleted += OnResult;

			var r = cl.RunSearchAsync();
			await r;


			var r2 = cl.RefineSearchAsync();
			await r2;

			sw.Stop();

			Console.WriteLine(sw.Elapsed.TotalSeconds);
		}
	}
}