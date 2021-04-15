// ReSharper disable RedundantUsingDirective

#pragma warning disable HAA0601,

using SimpleCore.Cli;
using SmartImage.Core;
using SmartImage.Searching;
using System;
using System.Diagnostics;
using System.Linq;
using SimpleCore.Net;
using SmartImage.Configuration;
using SmartImage.Lib;
using SmartImage.Utilities;
using static SimpleCore.Cli.NConsoleOption;

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
		private static readonly string InterfacePrompt =
			$"Enter the option number to open or {NConsole.NC_GLOBAL_EXIT_KEY} to exit.\n" +
			$"Hold down {NConsole.NC_ALT_FUNC_MODIFIER} to show more info.\n"              +
			$"Hold down {NConsole.NC_CTRL_FUNC_MODIFIER} to download.\n"                   +
			$"Hold down {NConsole.NC_COMBO_FUNC_MODIFIER} to open raw result.\n"           +
			$"{NConsole.NC_GLOBAL_RETURN_KEY}: Refine\n"                                   +
			$"{NConsole.NC_GLOBAL_REFRESH_KEY}: Refresh";

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

			NConsole.AutoResizeHeight = false;
			NConsole.Resize(Interface.MainWindowWidth, Interface.MainWindowHeight);

			Console.CancelKeyPress += (sender, eventArgs) => { };

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
				UserSearchConfig.Config.EnsureConfig();
				Integration.Setup();

				// Run UI if not using command line arguments
				if (UserSearchConfig.Config.NoArguments) {
					Interface.Run();
					Console.Clear();
				}

				// Image is automatically read from command line arguments,
				// or it is input through the main menu


				// Exit if no image is given
				if (!UserSearchConfig.Config.HasImageInput) {
					return;
				}

				SEARCH:

				// Run search

				var cfg2 = new SearchConfig()
				{
					SearchEngines = UserSearchConfig.Config.SearchEngines,
					PriorityEngines = UserSearchConfig.Config.PriorityEngines,
					Query = UserSearchConfig.Config.ImageInput
				};

				var client = new SearchClient(cfg2);
				client.RunSearchAsync();

				var res = client.Results;


				// Show results
				var i = new NConsoleInterface(client.Results)
				{
					SelectMultiple = false,
					Prompt         = InterfacePrompt
				};

				var v = i.Run();

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

				if (UserSearchConfig.Config.UpdateConfig) {
					UserSearchConfig.Config.SaveFile();
				}
			}
		}
	}
}