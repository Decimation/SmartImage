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

			Cli.Info("Open options: {0}", Config.OpenOptions);

			var img = args[0];

			if (!File.Exists(img)) {
				Cli.Error("File does not exist: {0}", img);
				return;
			}

			Cli.Info("Source: {0}", img);


			var imgUrl = Imgur.Value.Upload(img);

			Cli.Info("Temporary image: {0}", imgUrl);

			Console.WriteLine();

			var res = SauceNao.Value.GetSNResults(imgUrl);

			Cli.Success("SauceNao results: {0}", res.Length);

			
			
			var oo = Config.OpenOptions;

			
			HandleIndexer(SauceNao.Value, imgUrl, oo);
			HandleIndexer(ImgOps.Value, imgUrl, oo);
			HandleIndexer(GoogleImages.Value,imgUrl, oo);
			HandleIndexer(TinEye.Value, imgUrl, oo);

			Console.WriteLine();

			Cli.Success("Complete! Press any key to exit.");
			Console.ReadLine();
		}

		public static void HandleIndexer(IIndexer indexer, string imgUrl, OpenOptions oo)
		{
			var imgOps = indexer.GetResult(imgUrl);
			Cli.Result(imgOps);

			if (oo.HasFlag(indexer.Options)) {
				Common.OpenUrl(imgOps.Url);
			}
		}
	}
}