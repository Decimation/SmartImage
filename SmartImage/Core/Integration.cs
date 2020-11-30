using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Novus.Win32;
using SimpleCore.Console.CommandLine;

namespace SmartImage.Core
{
	// todo: move context menu integration to Novus for use in other projects

	internal enum IntegrationOption
	{
		Add,
		Remove
	}

	/// <summary>
	/// Program system integrations
	/// </summary>
	internal static class Integration
	{
		internal static void HandleContextMenu(IntegrationOption option)
		{

			switch (option) {
				case IntegrationOption.Add:
					string fullPath = Info.ExeLocation;

					// Add command and icon to command
					string[] addCode =
					{
						"@echo off",
						$"reg.exe add {REG_SHELL_CMD} /ve /d \"{fullPath} \"\"%%1\"\"\" /f >nul",
						$"reg.exe add {REG_SHELL} /v Icon /d \"{fullPath}\" /f >nul"
					};

					Command.RunBatch(addCode, true);

					break;
				case IntegrationOption.Remove:

					string[] removeCode =
					{
						"@echo off",
						$@"reg.exe delete {REG_SHELL} /f >nul"
					};

					Command.RunBatch(removeCode, true);

					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(option), option, null);
			}
		}

		internal static void HandlePath(IntegrationOption option)
		{
			switch (option) {
				case IntegrationOption.Add:
				{
					string oldValue = OS.EnvironmentPath;

					string appFolder = Info.AppFolder;

					if (Info.IsAppFolderInPath) {
						return;
					}


					bool appFolderInPath = oldValue
						.Split(OS.PATH_DELIM)
						.Any(p => p == appFolder);

					string cd = Environment.CurrentDirectory;
					string exe = Path.Combine(cd, Info.NAME_EXE);

					if (!appFolderInPath) {
						string newValue = oldValue + OS.PATH_DELIM + cd;
						OS.EnvironmentPath = newValue;
					}

					break;
				}
				case IntegrationOption.Remove:
					OS.RemoveFromPath(Info.AppFolder);
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(option), option, null);
			}
		}


		internal static void ResetIntegrations()
		{

			SearchConfig.Config.Reset();
			SearchConfig.Config.WriteToFile();

			// Computer\HKEY_CLASSES_ROOT\*\shell\SmartImage

			if (IsContextMenuAdded) {
				HandleContextMenu(IntegrationOption.Remove);
			}


			// will be added automatically if run again
			//Path.Remove();

			NConsole.WriteSuccess("Reset config");
		}

		[DoesNotReturn]
		internal static void Uninstall()
		{
			// autonomous uninstall routine

			// self destruct

			string exeFileName = Info.ExeLocation;

			const string DEL_BAT_NAME = "SmartImage_Delete.bat";

			string[] commands =
			{
				"@echo off",

				/* Wait approximately 4 seconds (so that the process is already terminated) */
				"ping 127.0.0.1 > nul",

				/* Delete executable */
				$"echo y | del /F {exeFileName}",

				/* Delete this bat file */
				$"echo y | del {DEL_BAT_NAME}"
			};

			// Runs in background
			Command.RunBatch(commands, false, DEL_BAT_NAME);

			

		}

		private const string REG_SHELL = @"HKEY_CLASSES_ROOT\*\shell\SmartImage\";

		private const string REG_SHELL_CMD = @"HKEY_CLASSES_ROOT\*\shell\SmartImage\command";


		internal static bool IsContextMenuAdded
		{
			get
			{
				var cmd = Command.Shell(@$"reg query {REG_SHELL_CMD}");
				cmd.Start();

				var stdOut = Command.ReadAllLines(cmd.StandardOutput);

				bool b = stdOut.Any(s => s.Contains(Info.NAME));
				return b;
			}
		}

		internal static void Setup()
		{
			if (!Info.IsAppFolderInPath) {
				HandlePath(IntegrationOption.Add);
			}
		}
	}
}