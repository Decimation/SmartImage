using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.Win32;
using SmartImage.Indexers;
using SmartImage.Utilities;

namespace SmartImage
{
	public static class Program
	{
		// @"C:\Users\Deci\Desktop\test.jpg";
		//dotnet publish -c Release -r win10-x64
		//var client_id     = "6c97880bf8754c5";
		//var client_secret = "fe1bed3047828fed3ce67bf2ae923282f0a9a558";

		private static void Main(string[] args)
		{
			Console.Title = "SmartImage";

			string userprofile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

			Directory.SetCurrentDirectory(userprofile);

			if (args == null || args.Length < 1) {
				Console.WriteLine("Invalid arguments");
				return;
			}

			switch (args[0]) {
				case "--setup":
				{
					var newId     = args[1];
					var newSecret = args[2];

					Console.WriteLine("New client ID and secret: ({0}, {1})", newId, newSecret);

					Config.ImgurAuth = (newId, newSecret);

					return;
				}
				case "--open-options":
				{
					var newOptions = args[1];

					Console.WriteLine("New options: {0}", newOptions);

					Config.OpenOptions = Enum.Parse<OpenOptions>(newOptions);

					return;
				}
				case "--ctx-menu":
					Config.AddToContextMenu();

					Console.WriteLine("Added to context menu!");
					
					return;
			}


			var (id, secret) = Config.ImgurAuth;

			if (id != null && secret != null) {
				Console.WriteLine(">> Using configured Imgur auth");
			}

			Console.WriteLine(">> Open options: {0}", Config.OpenOptions);

			var img = args[0];
			Console.WriteLine(">> Source: {0}", img);


			var imgUrl = Imgur.Value.Upload(img);

			Console.WriteLine(">> Temporary image: {0}", imgUrl);


			var res = SauceNao.Value.GetResults(imgUrl);

			Console.WriteLine("SauceNao results: {0}", res.Length);

			/*foreach (var re in res) {
				Console.WriteLine("\t{0}", re);
			}*/

			var oo = Config.OpenOptions;

			var sauceNao = res.OrderByDescending(r => r.Similarity).First().Url[0];
			Console.WriteLine("SauceNao: {0}", sauceNao);

			if (oo.HasFlag(OpenOptions.SauceNao)) {
				Common.OpenUrl(sauceNao);
			}

			// You can also insert  http://imgops.com/  in front of any image URL.
			var imgOps = "http://imgops.com/" + imgUrl;
			Console.WriteLine("ImgOps: {0}", imgOps);

			if (oo.HasFlag(OpenOptions.ImgOps)) {
				Common.OpenUrl(imgOps);
			}


			Console.WriteLine("Complete! Press any key to exit.");

			Console.ReadLine();
		}
	}
}