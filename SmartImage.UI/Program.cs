using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using SmartImage.Lib;
using SmartImage.Lib.Engines;
using SmartImage.Lib.Searching;

namespace SmartImage.UI
{
	public static class Program
	{
		public static void OnResult(object _, SearchClient.SearchResultEventArgs e)
		{

			if (e.Result.IsSuccessful) {
				Console.WriteLine(e.Result);
			}
		}
		
		private static async Task Main(string[] args)
		{
			string i;

			do {
				i = Console.ReadLine();

			} while (string.IsNullOrWhiteSpace(i));

			Console.OutputEncoding = Encoding.Unicode;
			Console.InputEncoding  = Encoding.Unicode;


			ImageQuery q = (i);


			var cfg = new SearchConfig
				{Query = q, SearchEngines = SearchEngineOptions.Artwork | SearchEngineOptions.Yandex};

			var cl = new SearchClient(cfg);

			//var r  = cl.Maximize(r => r.PrimaryResult.Similarity);
			//var r2 = await r;

			cl.ResultCompleted += OnResult;

			var r = cl.RunSearchAsync();
			await r;

			Console.ReadLine();
			var r2 = cl.RefineSearchAsync();
			await r2;
		}
	}
}