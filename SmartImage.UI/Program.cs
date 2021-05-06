using System;
using System.Text;
using System.Threading.Tasks;
using SmartImage.Lib;
using SmartImage.Lib.Engines;
using SmartImage.Lib.Searching;

namespace SmartImage.UI
{
	class Program
	{
		public static void OnResult(object _, SearchClient.SearchResultEventArgs e)
		{
			Console.WriteLine(e.Result);

			if (e.Result.IsSuccessful) { }
		}

		static async Task Main(string[] args)
		{


			var i = Console.ReadLine();


			Console.OutputEncoding = Encoding.Unicode;
			Console.InputEncoding  = Encoding.Unicode;


			var q = new ImageQuery(i);


			var cfg = new SearchConfig
				{Query = q, SearchEngines = SearchEngineOptions.SauceNao | SearchEngineOptions.Iqdb};

			var cl = new SearchClient(cfg);

			//var r  = cl.Maximize(r => r.PrimaryResult.Similarity);
			//var r2 = await r;

			cl.ResultCompleted += OnResult;

			var r = cl.RunSearchAsync();
			await r;
		}
	}
}