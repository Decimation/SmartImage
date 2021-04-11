using System;
using SimpleCore.Utilities;
using SmartImage.Lib;
using SmartImage.Lib.Engines;
using SmartImage.Lib.Searching;

namespace Test
{
	public static class Program
	{
		public static void Main(string[] args)
		{

			//var q = new ImageQuery("https://i.imgur.com/QtCausw.jpg");


			var q = new ImageQuery(@"C:\Users\Deci\Pictures\fucking_epic.jpg");

			var cfg = new SearchConfig() {Query = q, SearchEngines = SearchEngineOptions.All};

			var cl = new SearchClient(cfg);


			while (!cl.IsComplete) {
				var value = cl.Next().Result;

				Console.WriteLine(value);
			}


		}
	}
}