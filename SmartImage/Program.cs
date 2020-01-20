using System;
using SmartImage.Indexers;

namespace SmartImage
{
	public static class Program
	{
		//6c97880bf8754c5
		//fe1bed3047828fed3ce67bf2ae923282f0a9a558
		private static void Main(string[] args)
		{
			var client_id     = "6c97880bf8754c5";
			var client_secret = "fe1bed3047828fed3ce67bf2ae923282f0a9a558";
			var img           = @"C:\Users\Deci\Desktop\amazing.jpg";

			var imgUrl = Imgur.Upload(img, client_id);
			
			var sn = new SauceNao("https://saucenao.com/search.php");
			var res=sn.GetResults(imgUrl, "c1f946bb2003c92fa8a25ce7fa923e0f213a0db8");

			foreach (var re in res) {
				Console.WriteLine(re);
			}
		}
	}
}