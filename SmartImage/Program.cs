#region

using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using Neocmd;
using RapidSelenium;
using SmartImage.Engines.SauceNao;
using SmartImage.Model;
using SmartImage.Searching;
using SmartImage.Utilities;
using CommandLine;

#endregion

namespace SmartImage
{
	public static class Program
	{
		// @"C:\Users\Deci\Desktop\test.jpg";

		// C:\Users\Deci\RiderProjects\SmartImage\SmartImage\bin\Release\netcoreapp3.0\win10-x64\publish
		// C:\Users\Deci\RiderProjects\SmartImage\SmartImage\bin\Debug\netcoreapp3.0\win10-x64
		// copy SmartImage.exe C:\Users\Deci\AppData\Local\SmartImage /Y
		// copy SmartImage.exe C:\Users\Deci\Desktop /Y
		// dotnet publish -c Release -r win10-x64

		// copy SmartImage.exe C:\Library /Y
		// copy SmartImage.exe C:\Users\Deci\Desktop /Y

		// Computer\HKEY_CLASSES_ROOT\*\shell\SmartImage
		// Computer\HKEY_CURRENT_USER\Software\SmartImage

		// Computer\HKEY_CLASSES_ROOT\*\shell\SmartImage
		// "HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Environment"

		// C:\Users\Deci\AppData\Local\Temp\.net\SmartImage
		

		private static void Main(string[] args)
		{
			//Commands.Setup();
			Config.Setup();


			var result = Parser.Default.ParseArguments<CliUtilities.Arguments>(args);

			var resultOptions = result.WithParsed(cmds =>
			{
				/*
				 * Parse 
				 */
				Console.WriteLine(cmds);
			});

			var types = CliUtilities.LoadVerbs();

			Parser.Default.ParseArguments(args, types)
			      .WithParsed(CliUtilities.Run)
			      .WithNotParsed(CliUtilities.HandleErrors);
			
			/*var verbOptions = result.WithParsed<CtxMenuOptions>(ctxMenu =>
			{
				Console.WriteLine(ctxMenu);
			});*/

			return;

			if (args == null || args.Length < 1) {
				CliOutput.WriteError("Image or command not specified!");
				CliOutput.WriteCommands();
				return;
			}

			var arg = args[0];

			if (arg == "--test") {
				// ...

				return;
			}
			else if (arg == "--qr") {
				// Display commands with autocompletion

				Console.Clear();

				var commands = Commands.AllCommands.Select(c => c.Parameter).ToArray();

				Console.WriteLine("Available commands:\n");

				foreach (var c in commands)
					Console.WriteLine(c);

				Console.WriteLine("\nEnter a command:");

				var input = Commands.ReadHintedLine(commands, c => c);

				Console.WriteLine($"\n>> {input}");

				args = input.Split(' ');
				arg  = args[0];
			}

			// Run the command if one was parsed
			var cmd = CliOutput.ReadCommand(arg);

			if (cmd != null) {
				cmd.Action(args);
				return;
			}

			var  auth     = Config.ImgurAuth;
			bool useImgur = !auth.IsNull;

			var engines  = Config.SearchEngines;
			var priority = Config.PriorityEngines;

			if (engines == SearchEngines.None) {
				CliOutput.WriteError("Please configure search engine preferences!");
				return;
			}

			CliOutput.WriteInfo("Engines: {0}", engines);
			CliOutput.WriteInfo("Priority engines: {0}", priority);

			string img = args[0];

			if (!Search.IsFileValid(img)) {
				return;
			}

			CliOutput.WriteInfo("Source image: {0}", img);

			string imgUrl = Search.Upload(img, useImgur);

			CliOutput.WriteInfo("Temporary image url: {0}", imgUrl);

			Console.WriteLine();

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
		}
	}
}