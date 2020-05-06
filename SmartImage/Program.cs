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

		public class MyClass
		{
			[Option("search-engines", Separator = ',')]
			public SearchEngines Engines { get; set; }

			[Option("priority-engines", Separator = ',')]
			public SearchEngines PriorityEngines { get; set; }

			[Option("imgur-auth")]
			public string ImgurAuth { get; set; }

			[Option("saucenao-auth")]
			public string SauceNaoAuth { get; set; }


			public override string ToString()
			{
				var sb = new StringBuilder();

				sb.AppendFormat("Search engines: {0}\n", Engines);
				sb.AppendFormat("Priority engines: {0}\n", PriorityEngines);
				sb.AppendFormat("Imgur auth: {0}\n", ImgurAuth);
				sb.AppendFormat("SauceNao auth: {0}\n", SauceNaoAuth);

				return sb.ToString();
			}
		}

		[Verb("ctx-menu")]
		public class CtxMenuOptions
		{
			public bool Add { get; set; }

			public override string ToString()
			{
				var sb = new StringBuilder();
				sb.AppendFormat("Add: {0}\n", Add);
				return sb.ToString();
			}
		}

		//load all types using Reflection
		private static Type[] LoadVerbs()
		{
			return Assembly.GetExecutingAssembly().GetTypes()
			               .Where(t => t.GetCustomAttribute<VerbAttribute>() != null).ToArray();
		}

		private static void Run(object obj)
		{
			switch (obj) {
				case CtxMenuOptions c:
					//process CloneOptions
					Console.WriteLine(c);
					break;
			}
		}

		private static void HandleErrors(object obj)
		{
			Console.WriteLine("error");
		}

		private static void Main(string[] args)
		{
			//Commands.Setup();
			Config.Setup();


			var result = Parser.Default.ParseArguments<MyClass>(args);

			var resultOptions = result.WithParsed(cmds => { Console.WriteLine(cmds); });

			var types = LoadVerbs();

			Parser.Default.ParseArguments(args, types)
			      .WithParsed(Run)
			      .WithNotParsed(HandleErrors);
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