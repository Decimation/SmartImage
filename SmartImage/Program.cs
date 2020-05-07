#region

using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using RapidSelenium;
using SmartImage.Engines.SauceNao;
using SmartImage.Model;
using SmartImage.Searching;
using CommandLine;
using SimpleCore;
using SimpleCore.Utilities;

#endregion

namespace SmartImage
{
	public static class Program
	{
		// @"C:\Users\Deci\Desktop\test.jpg";

		// C:\Users\Deci\RiderProjects\SmartImage\SmartImage\bin\Release\netcoreapp3.0\win10-x64
		// C:\Users\Deci\RiderProjects\SmartImage\SmartImage\bin\Release\netcoreapp3.0\win10-x64\publish

		// C:\Users\Deci\RiderProjects\SmartImage\SmartImage\bin\Debug\netcoreapp3.0\win10-x64


		
		// dotnet publish -c Release -r win10-x64

		// copy SmartImage.exe C:\Library /Y
		// copy SmartImage.exe C:\Users\Deci\Desktop /Y

		// Computer\HKEY_CLASSES_ROOT\*\shell\SmartImage
		// Computer\HKEY_CURRENT_USER\Software\SmartImage

		// Computer\HKEY_CLASSES_ROOT\*\shell\SmartImage
		// "HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Environment"

		public static void Cleanup()
		{
			if (RuntimeInfo.Config.UpdateConfig) {
				CliOutput.WriteInfo("Updating cfg");
				RuntimeInfo.Config.WriteToFile();
			}
		}
		
		private static void Main(string[] args)
		{
			if (args == null || args.Length == 0) {
				return;
			}

			RuntimeInfo.Setup();

			CliParse.ReadArguments(args);

			bool run = RuntimeInfo.Config.Image != null;

			if (run) {
				/*
                 * Run 
                 */

				var  auth     = RuntimeInfo.Config.ImgurAuth;
				bool useImgur = !string.IsNullOrWhiteSpace(auth);

				var engines  = RuntimeInfo.Config.Engines;
				var priority = RuntimeInfo.Config.PriorityEngines;

				if (engines == SearchEngines.None) {
					CliOutput.WriteError("Please configure search engine preferences!");
					return;
				}


				string img = RuntimeInfo.Config.Image;

				// Exit
				if (!Search.IsFileValid(img)) {
					Cleanup();
					return;
				}

				CliOutput.WriteInfo(RuntimeInfo.Config);

				string imgUrl = Search.Upload(img, useImgur);

				CliOutput.WriteInfo("Temporary image url: {0}", imgUrl);

				Console.WriteLine();

				//Console.ReadLine();

				//
				// 
				//

				// Where the actual searching occurs
				SearchResult[] results = Search.RunSearches(imgUrl, engines);

				ConsoleKeyInfo cki;

				do {
					Console.Clear();

					for (int i = 0; i < results.Length; i++) {
						var    r   = results[i];
						string str = r.Format((i + 1).ToString());

						Console.Write(str);
					}

					Console.WriteLine();

					// Exit
					if (RuntimeInfo.Config.AutoExit) {
						Cleanup();
						return;
					}

					CliOutput.WriteSuccess("Enter the result number to open or escape to quit.");

					while (!Console.KeyAvailable) {
						// Block until input is entered.
					}

					// Key was read

					cki = Console.ReadKey(true);
					char keyChar = cki.KeyChar;

					if (Char.IsNumber(keyChar)) {
						int idx = (int) Char.GetNumericValue(cki.KeyChar) - 1;

						if (idx < results.Length) {
							var res = results[idx];
							WebAgent.OpenUrl(res.Url);
						}
					}
				} while (cki.Key != ConsoleKey.Escape);

				// Exit
				Cleanup();
			}
			else {
				//CliOutput.WriteInfo("Exited");
			}
		}
	}
}