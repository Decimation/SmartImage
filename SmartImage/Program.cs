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
using SmartImage.Engines.SauceNao;
using SmartImage.Searching;
using SimpleCore;
using SimpleCore.Utilities;

#endregion

namespace SmartImage
{
	/**
	 * Single file executable build dir
	 * 
	 * C:\Users\Deci\RiderProjects\SmartImage\SmartImage\bin\Release\netcoreapp3.1\win10-x64
	 * C:\Users\Deci\RiderProjects\SmartImage\SmartImage\bin\Release\netcoreapp3.1\win10-x64\publish
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
	 * copy C:\Users\Deci\RiderProjects\SmartImage\SmartImage\bin\Release\netcoreapp3.1\win10-x64\publish\SmartImage.exe C:\Users\Deci\Desktop /Y
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
			Console.SetWindowSize(120, 35);
			Console.Clear();

			RuntimeInfo.Setup();
			SearchConfig.ReadSearchConfigArguments(args);

			if (SearchConfig.Config.NoArguments) {
				Commands.RunCommandMenu();
				Console.Clear();
			}

			string img = SearchConfig.Config.Image;

			bool run = !String.IsNullOrWhiteSpace(img);

			if (!run) {
				return;
			}

			var results = new SearchResult[(int) SearchEngines.All];
			var ok = Search.RunSearch(img, ref results);

			if (!ok) {
				CliOutput.WriteError("Search failed");
				return;
			}

			Commands.HandleConsoleOptions(results);

			// Exit
			SearchConfig.Cleanup();
		}
	}
}