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
	/**
	 * Single file executable build dir
	 * 
	 * C:\Users\Deci\RiderProjects\SmartImage\SmartImage\bin\Release\netcoreapp3.0\win10-x64
	 * C:\Users\Deci\RiderProjects\SmartImage\SmartImage\bin\Release\netcoreapp3.0\win10-x64\publish
	 * C:\Users\Deci\RiderProjects\SmartImage\SmartImage\bin\Debug\netcoreapp3.0\win10-x64
	 *
	 * Single file publish command
	 *
	 * dotnet publish -c Release -r win10-x64
	 * dotnet publish -c Release -r win10-x64 --self-contained
	 *
	 * Legacy registry keys
	 *
	 * Computer\HKEY_CLASSES_ROOT\*\shell\SmartImage
	 * Computer\HKEY_CURRENT_USER\Software\SmartImage
	 * "HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Environment"
	 *
	 * Copy build
	 *
	 * copy SmartImage.exe C:\Library /Y
	 * copy SmartImage.exe C:\Users\Deci\Desktop /Y
	 * copy C:\Users\Deci\RiderProjects\SmartImage\SmartImage\bin\Release\netcoreapp3.0\win10-x64\publish\SmartImage.exe C:\Users\Deci\Desktop /Y
	 * 
	 * Bundle extract folder
	 * 
	 * C:\Users\Deci\AppData\Local\Temp\.net\SmartImage
	 * DOTNET_BUNDLE_EXTRACT_BASE_DIR
	 *
	 *
	 * nuget pack -Prop Configuration=Release
	 *
	 * C:\Library\Nuget
	 * dotnet pack -c Release -o %cd%
	 * dotnet nuget push "*.nupkg"
	 * del *.nupkg & dotnet pack -c Release -o %cd%
	 */
	public static class Program
	{
		/**
		 * Entry point
		 */
		private static void Main(string[] args)
		{
			if (args == null || args.Length == 0) {
				return;
			}

			RuntimeInfo.Setup();

			CliParse.ReadArguments(args);

			var img = RuntimeInfo.Config.Image;

			bool run = img != null;

			if (run) {
				var sr      = new SearchResults();
				var ok      = Search.RunSearch(img, ref sr);
				var results = sr.Results;
				
				// Console.WriteLine("Elapsed: {0:F} sec", result.Duration.TotalSeconds);

				ConsoleKeyInfo cki;

				do {
					Console.Clear();

					for (int i = 0; i < sr.Results.Length; i++) {
						var r = sr.Results[i];

						var tag = (i + 1).ToString();
						if (r != null) {
							string str = r.Format(tag);

							Console.Write(str);
						}
						else {
							Console.WriteLine("{0} - ...", tag);
						}
					}

					Console.WriteLine();

					// Exit
					if (RuntimeInfo.Config.AutoExit) {
						SearchConfig.Cleanup();
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

						if (idx < results.Length && idx >= 0) {
							var res = results[idx];
							WebAgent.OpenUrl(res.Url);
						}
					}
				} while (cki.Key != ConsoleKey.Escape);

				// Exit
				SearchConfig.Cleanup();
			}
			else {
				//CliOutput.WriteInfo("Exited");
			}
		}
	}
}