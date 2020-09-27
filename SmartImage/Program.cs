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
using SmartImage.Searching;
using SimpleCore;
using SimpleCore.Utilities;
using SimpleCore.Win32.Cli;
using SmartImage.Shell;
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
			/*
			 * Set up console
			 */

			Console.Title = RuntimeInfo.NAME;
			Console.SetWindowSize(120, 45);
			Console.OutputEncoding = Encoding.Unicode;
			CliOutput.EnableVirtualTerminalProcessing();
			Console.Clear();

			/*
			 * Run search
			 */

			try {

				Integration.Setup();
				SearchConfig.ReadSearchConfigArguments(args);

				if (SearchConfig.Config.NoArguments) {
					ConsoleIO.RunMainCommandMenu();
					Console.Clear();
				}

				string img = SearchConfig.Config.Image;

				var n = Enum.GetValues(typeof(SearchEngines)).Length;

				ConsoleOption[] results = new SearchResult[n];
				var ok = Search.RunSearch(img, ref results);

				if (!ok) {
					CliOutput.WriteError("Search failed or aborted");
					return;
				}

				ConsoleIO.HandleConsoleOptions(results);
			}
			catch (Exception exception) {

#if !DEBUG
				var cr = new CrashReport(exception);

				Console.WriteLine(cr);

				var src = cr.WriteToFile();

				Console.WriteLine("Crash log written to {0}", src);

				Console.WriteLine("Please file an issue and attach the crash log.");

				Network.OpenUrl(RuntimeInfo.Issue);

				ConsoleIO.WaitForInput();
#endif

			}
			finally {
				// Exit
				SearchConfig.UpdateFile();
			}


		}
	}
}