// ReSharper disable RedundantUsingDirective

#pragma warning disable HAA0601,

using SimpleCore.Cli;
using SmartImage.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using SimpleCore.Net;
using SimpleCore.Utilities;
using SmartImage.Lib;
using SmartImage.Lib.Searching;
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
		 * Entry point
		 */

		private static async Task Main(string[] args)
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

			/*
			 * Set up NConsole
			 */
			NConsole.Init();

			Console.CancelKeyPress += (sender, eventArgs) => { };

			Console.Clear();

			Console.WriteLine(Info.NAME_BANNER);

			var x = NConsoleOption.FromArray(new[] {"run", "exit"}, (x) => x);


			var io = NConsole.ReadOptions(x, false);
			Console.WriteLine(io.QuickJoin());


			/*
			 * Run search
			 */

			try {


				// Setup
				Integration.Setup();


				// Run search
				ImageQuery q      = Console.ReadLine();
				var        cfg    = new SearchConfig() {Query = q};
				var        client = new SearchClient(cfg);

				client.ResultCompleted += (sender, eventArgs) =>
				{
					Console.WriteLine(eventArgs.Result);
				};
				// Show results


				await client.RunSearchAsync();

			}
			catch (Exception exception) {
#if !DEBUG
#else
				Console.WriteLine(exception);
#endif
			}
			finally {
				// Exit

			}
		}
	}
}