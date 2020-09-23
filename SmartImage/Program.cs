#region

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;
using SmartImage.Engines.SauceNao;
using SmartImage.Searching;
using SimpleCore;
using SimpleCore.Utilities;
using SmartImage.Utilities;

#endregion

namespace SmartImage
{
	public static class Program
	{
		//  ____                       _   ___
		// / ___| _ __ ___   __ _ _ __| |_|_ _|_ __ ___   __ _  __ _  ___
		// \___ \| '_ ` _ \ / _` | '__| __|| || '_ ` _ \ / _` |/ _` |/ _ \
		//  ___) | | | | | | (_| | |  | |_ | || | | | | | (_| | (_| |  __/
		// |____/|_| |_| |_|\__,_|_|   \__|___|_| |_| |_|\__,_|\__, |\___|
		//                                                     |___/

		// todo: further improve UI; use Terminal.Gui possibly

		// todo: remove SmartImage nuget package stuff

		// todo: fix access modifiers

		/**
		 * Entry point
		 */
		private static void Main(string[] args)
		{

			Console.Title = RuntimeInfo.NAME;
			Console.SetWindowSize(120, 40);
			Console.OutputEncoding = Encoding.Unicode;
			Console.Clear();

			try {

				RuntimeInfo.Setup();
				SearchConfig.ReadSearchConfigArguments(args);

				if (SearchConfig.Config.NoArguments) {
					Commands.RunCommandMenu();
					Console.Clear();
				}

				string img = SearchConfig.Config.Image;


				var results = new SearchResult[(int) SearchEngines.All];
				var ok = Search.RunSearch(img, ref results);

				if (!ok) {
					CliOutput.WriteError("Search failed or aborted");
					return;
				}

				Commands.HandleConsoleOptions(results);


			}
			catch (Exception exception) {


				var cr = new CrashReport(exception);

				// Console.ForegroundColor = ConsoleColor.DarkRed;
				// Console.BackgroundColor = ConsoleColor.White;


				Console.WriteLine(cr);


				var src = cr.WriteToFile();

				Console.WriteLine("Crash log written to {0}", src);

				Console.WriteLine("Please file an issue and attach the crash log.");

				NetworkUtilities.OpenUrl(RuntimeInfo.Issue);

				Commands.WaitForInput();
				
			}
			finally {
				// Exit
				SearchConfig.Cleanup();
			}


		}
	}
}