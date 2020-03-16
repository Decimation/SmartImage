using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
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
				Cli.WriteError("Image or command not specified!");
				Cli.WriteHelp();
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
				Cli.WriteInfo("Using configured Imgur auth");
			}

			var engines = Config.SearchEngines;
			var priority = Config.PriorityEngines;

			if (engines == SearchEngines.None) {
				Cli.WriteError("Please configure search engine preferences!");
				return;
			}

			Cli.WriteInfo("Engines: {0}", engines);
			Cli.WriteInfo("Priority engines: {0}", priority);
			
			var img = args[0];

			if (!File.Exists(img)) {
				Cli.WriteError("File does not exist: {0}", img);
				return;
			}

			Cli.WriteInfo("Source image: {0}", img);

			var imgUrl = Imgur.Value.Upload(img);

			Cli.WriteInfo("Temporary image url: {0}", imgUrl);

			Console.WriteLine();

			var results = RunSearches(imgUrl, engines);

			ConsoleKeyInfo cki;

			do {
				Console.Clear();

				for (int i = 0; i < results.Length; i++) {
					Console.WriteLine("[{0}] {1}", i + 1, results[i]);
				}

				Console.WriteLine();

				Cli.WriteSuccess("Enter the result number to open or escape to quit.");

				while (!Console.KeyAvailable) {
					// Block until input is entered.
				}

				// Key was read

				cki = Console.ReadKey(true);
				var keyChar = cki.KeyChar;

				if (Char.IsNumber(keyChar)) {
					var idx = (int) Char.GetNumericValue(cki.KeyChar) - 1;

					if (idx < results.Length) {
						var res = results[idx];
						Common.OpenUrl(res.Url);
					}
				}
			} while (cki.Key != ConsoleKey.Escape);
		}

		private static readonly ISearchEngine[] AllEngines =
		{
			SauceNao.Value,
			ImgOps.Value,
			GoogleImages.Value,
			TinEye.Value,
			Iqdb.Value,
		};

		private static SearchResult[] RunSearches(string imgUrl, SearchEngines engines)
		{
			var list = new List<SearchResult>();

			foreach (var idx in AllEngines) {
				if (engines.HasFlag(idx.Engine)) {
					var result = idx.GetResult(imgUrl);
					list.Add(result);

					if (Config.PriorityEngines.HasFlag(idx.Engine)) {
						Common.OpenUrl(result.Url);
					}
				}
			}

			return list.ToArray();
		}
	}
}