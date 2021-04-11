using System;
using SimpleCore.Utilities;
using SmartImage.Lib;

namespace Test
{
	public static class Program
	{
		public static void Main(string[] args)
		{
			Console.WriteLine("Hello World!");


			var q = new ImageQuery("https://i.imgur.com/QtCausw.jpg");


			var q2 = new ImageQuery(@"C:\Users\Deci\Pictures\fucking_epic.jpg");

			var cfg = new SearchConfig() {Query = q, SearchEngines = SearchEngineOptions.All};

			var cl  = new SearchClient(cfg);


			while (!cl.IsComplete) {
				Console.WriteLine(cl.Next().Result);
			}
		}
	}
}