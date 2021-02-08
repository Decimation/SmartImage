// ReSharper disable RedundantUsingDirective

#pragma warning disable HAA0601,

using SmartImage.Searching;
using System;
using System.Text;
using System.Threading;
using SimpleCore.Cli;
using SimpleCore.Net;
using SmartImage.Core;
using SmartImage.Utilities;

// ReSharper disable UnusedParameter.Local
#pragma warning disable CA1416

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
		 * todo: reorganize, restructure, refactor, etc.
		 */
		

		/*
		 * Entry point
		 */

		private static void Main(string[] args)
		{
			/*
			 * Setup
			 * Check compatibility
			 */
			Info.Setup();


			/*
			 * Set up console
			 */

			Console.Title = Info.NAME;

			NConsole.Resize(Interface.ConsoleWindowWidth, Interface.ConsoleWindowHeight);


			Console.Clear();

			Console.WriteLine(Info.NAME_BANNER);
			NConsole.WriteInfo("Setting up...");


			/*
			 * Set up NConsole
			 */
			NConsole.Init();
			NConsoleInterface.DefaultName = Info.NAME_BANNER;
			

			/*
			 * Check for any legacy integrations that need to be migrated
			 */
			bool cleanupOk = LegacyIntegration.LegacyCleanup();

			if (!cleanupOk) {
				NConsole.WriteError("Could not migrate legacy features");
			}

			/*
			 * Run search
			 */

			try {

				// Setup
				SearchConfig.Config.EnsureConfig();
				Integration.Setup();

				// Run UI if not using command line arguments
				if (SearchConfig.Config.NoArguments) {
					Interface.Run();
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
				SearchClient.Client.Interface.Run();
			}
			catch (Exception exception) {
#if !DEBUG
				var cr = new CrashReport(exception);

				Console.WriteLine(cr);


				var src = cr.WriteToFile();
				Console.WriteLine(exception.InnerException?.StackTrace);
				Console.WriteLine(exception.InnerException?.Message);
				Console.WriteLine("Crash log written to {0}", src);

				Console.WriteLine("Please file an issue and attach the crash log.");

				//Network.OpenUrl(Info.Issue);

				NConsole.WaitForInput();
#else
				Console.WriteLine(exception);
#endif
			}
			finally {
				// Exit

				if (SearchConfig.Config.UpdateConfig) {
					SearchConfig.Config.SaveFile();
				}
			}
		}
	}
}