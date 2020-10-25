#region

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Media;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;
using SmartImage.Searching;
using SimpleCore;
using SimpleCore.CommandLine;
using SimpleCore.Utilities;
using SmartImage.Searching.Model;
using SmartImage.Utilities;

#endregion

#pragma warning disable HAA0601
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

		/*
		 * todo: refactor access modifiers
		 */

		/*
		 * Entry point
		 */
		private static void Main(string[] args)
		{
			/*
			 * Set up console
			 */


			Console.Title = RuntimeInfo.NAME;
			Console.SetWindowSize(120, 50);
			Console.OutputEncoding = Encoding.Unicode;
			Console.Clear();

			NConsoleUI.DefaultName = RuntimeInfo.NAME_BANNER;

			/*
			 * Run search
			 */

			try {

				// Setup
				SearchConfig.Config.Setup();
				Integration.Setup();

				// Run UI if not using command line arguments
				if (SearchConfig.Config.NoArguments) {
					ConsoleMainMenu.Run();
					Console.Clear();
				}

				// Run search

				SearchClient.Client.Start();

				// Show results

				NConsole.IO.HandleOptions(SearchClient.Client.Interface);
			}
			catch (Exception exception) {

#if !DEBUG
				var cr = new CrashReport(exception);

				Console.WriteLine(cr);

				var src = cr.WriteToFile();

				Console.WriteLine("Crash log written to {0}", src);

				Console.WriteLine("Please file an issue and attach the crash log.");

				Network.OpenUrl(RuntimeInfo.Issue);

				NConsole.IO.WaitForInput();
#else
				Console.WriteLine(exception);
#endif

			}
			finally {
				// Exit
				SearchConfig.Config.UpdateFile();
			}


		}
	}
}