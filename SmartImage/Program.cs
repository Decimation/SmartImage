using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
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

			var sauceNao = res.OrderByDescending(r => r.Similarity).First().Url[0];
			Cli.Success("SauceNao: {0}", sauceNao);

			if (oo.HasFlag(OpenOptions.SauceNao)) {
				Common.OpenUrl(sauceNao);
			}


			var imgOps = ImgOps.Value.GetResult(imgUrl);
			Cli.Success("ImgOps: {0}", imgOps);

			if (oo.HasFlag(OpenOptions.ImgOps)) {
				Common.OpenUrl(imgOps);
			}

			var googleImages = GoogleImages.Value.GetResult(imgUrl);
			Cli.Success("Google Images: {0}", googleImages);

			if (oo.HasFlag(OpenOptions.GoogleImages)) {
				Common.OpenUrl(googleImages);
			}


			Cli.Success("\nComplete!");
		}
	}
}