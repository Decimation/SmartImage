// ReSharper disable RedundantUsingDirective

#pragma warning disable HAA0601,

using SimpleCore.CommandLine;
using SmartImage.Searching;
using System;
using System.Text;
using SimpleCore.Net;
using SmartImage.Utilities;
// ReSharper disable UnusedParameter.Local


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
		 * todo: maybe create a separate unit testing project
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
			NConsole.Init();

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
					MainMenu.Run();
					Console.Clear();
				}

				// Image is automatically read from command line arguments,
				// or it is input through the main menu


				// Exit if no image is given
				if (String.IsNullOrWhiteSpace(SearchConfig.Config.Image)) {
					return;
				}

				// Run search
				SearchClient.Client.Start();

				// Show results
				NConsoleIO.HandleOptions(SearchClient.Client.Interface);
			}
			catch (Exception exception) {
#if !DEBUG
				var cr = new CrashReport(exception);

				Console.WriteLine(cr);

				var src = cr.WriteToFile();

				Console.WriteLine("Crash log written to {0}", src);

				Console.WriteLine("Please file an issue and attach the crash log.");

				Network.OpenUrl(RuntimeInfo.Issue);

				NConsoleIO.WaitForInput();
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