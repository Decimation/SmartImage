using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Win32;
using SmartImage.Indexers;
using SmartImage.Indexers.SauceNao;
using SmartImage.Model;
using SmartImage.Utilities;

namespace SmartImage
{
	public static class Program
	{
		// @"C:\Users\Deci\Desktop\test.jpg";
		//dotnet publish -c Release -r win10-x64
		//var client_id     = "6c97880bf8754c5";
		//var client_secret = "fe1bed3047828fed3ce67bf2ae923282f0a9a558";
		// copy SmartImage.exe C:\Library /Y

		private static void Main(string[] args)
		{
			Cli.Init();

			if (args == null || args.Length < 1) {
				Cli.Error("Image or command not specified!");
				Cli.Help();
				return;
			}


			// Run the command if one was parsed
			var cmd = Cli.ReadCommand(args[0]);

			if (cmd != null) {
				cmd.Action(args);
				return;
			}


			var (id, secret) = Config.ImgurAuth;

			if (id != null && secret != null) {
				Cli.Info("Using configured Imgur auth");
			}

			var oo = Config.SearchEngines;

			if (oo == SearchEngines.None) {
				Cli.Error("Please configure search engine preferences!");
				return;
			}

			Cli.Info("Engines: {0}", oo);

			var img = args[0];

			if (!File.Exists(img)) {
				Cli.Error("File does not exist: {0}", img);
				return;
			}

			Cli.Info("Source image: {0}", img);

			var imgUrl = Imgur.Value.Upload(img);

			Cli.Info("Temporary image url: {0}", imgUrl);

			Console.WriteLine();

			RunSearches(imgUrl, oo);

			Console.WriteLine();

			Cli.Success("Complete! Press any ESC to exit.");

			do {
				while (!Console.KeyAvailable) {
					// Do something
				}

				var cki = Console.ReadKey(true);

				
				
			} while (Console.ReadKey(true).Key != ConsoleKey.Escape);
		}

		private static readonly ISearchEngine[] AllEngines =
			{SauceNao.Value, ImgOps.Value, GoogleImages.Value, TinEye.Value, Iqdb.Value,};

		private static void RunSearches(string imgUrl, SearchEngines engines)
		{
			foreach (var idx in AllEngines) {
				RunSearch(idx, imgUrl, engines);
			}
		}

		private static void RunSearch(ISearchEngine engine, string imgUrl, SearchEngines oo)
		{
			var imgOps = engine.GetResult(imgUrl);
			Cli.Result(imgOps);

			if (oo.HasFlag(engine.Engine)) {
				Common.OpenUrl(imgOps.Url);
			}
		}
	}
}