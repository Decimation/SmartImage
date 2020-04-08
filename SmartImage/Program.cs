using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.Win32;
using RestSharp;
using SmartImage.Engines;
using SmartImage.Engines.SauceNao;
using SmartImage.Model;
using SmartImage.Utilities;

namespace SmartImage
{
	public static class Program
	{
		// @"C:\Users\Deci\Desktop\test.jpg";
		
		// C:\Users\Deci\RiderProjects\SmartImage\SmartImage\bin\Release\netcoreapp3.0\win10-x64\publish
		// dotnet publish -c Release -r win10-x64
		
		// copy SmartImage.exe C:\Library /Y

		// Computer\HKEY_CLASSES_ROOT\*\shell\SmartImage
		// Computer\HKEY_CURRENT_USER\Software\SmartImage

		// Computer\HKEY_CLASSES_ROOT\*\shell\SmartImage
		// "HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Environment"

		private static readonly string[] ImageExtensions =
		{
			".jpg", ".jpeg", ".png", ".gif", ".tga", ".jfif"
		};

		private static bool IsFileValid(string img)
		{
			if (!File.Exists(img)) {
				Cli.WriteError("File does not exist: {0}", img);
				return false;
			}

			bool extOkay = ImageExtensions.Any(img.EndsWith);

			if (!extOkay) {
				Cli.WriteInfo("File extension is not recognized as a common image format. Continue? (y/n)");
				Console.WriteLine();

				var key = char.ToLower(Console.ReadKey().KeyChar);

				if (key == 'n') {
					return false;
				}
				else if (key == 'y') {
					Console.Clear();
					return true;
				}
			}


			return true;
		}

		private static string Upload(string img, bool useImgur)
		{
			string imgUrl;

			if (useImgur) {
				Cli.WriteInfo("Using Imgur for image upload");
				var imgur = new Imgur();
				imgUrl = imgur.Upload(img);
			}
			else {
				Cli.WriteInfo("Using ImgOps for image upload (2 hour cache)");
				var imgOps = new ImgOps();
				imgUrl = imgOps.UploadTempImage(img, out _);
			}


			return imgUrl;
		}

		


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
			bool useImgur = !String.IsNullOrWhiteSpace(id) && !String.IsNullOrWhiteSpace(secret);

			var engines  = Config.SearchEngines;
			var priority = Config.PriorityEngines;

			if (engines == SearchEngines.None) {
				Cli.WriteError("Please configure search engine preferences!");
				return;
			}
			
			Cli.WriteInfo("Engines: {0}", engines);
			Cli.WriteInfo("Priority engines: {0}", priority);

			var img = args[0];

			if (!IsFileValid(img)) {
				return;
			}

			Cli.WriteInfo("Source image: {0}", img);

			string imgUrl = Upload(img, useImgur);

			Cli.WriteInfo("Temporary image url: {0}", imgUrl);

			Console.WriteLine();

			//
			// 
			//
			
			// Where the actual searching occurs
			var results = Search.RunSearches(imgUrl, engines);

			ConsoleKeyInfo cki;

			do {
				Console.Clear();

				for (int i = 0; i < results.Length; i++) {
					var r   = results[i];
					var str = r.Format((i+1).ToString());

					Console.WriteLine(str);
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
	}
}