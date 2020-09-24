using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Win32;
using SimpleCore.Win32;
using SimpleCore.Win32.Cli;
using SmartImage.Shell;
using SmartImage.Utilities;

namespace SmartImage
{
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
			// TODO: use default Registry library

			switch (option) {
				case IntegrationOption.Add:
					string fullPath = RuntimeInfo.ExeLocation;

					if (!RuntimeInfo.IsExeInAppFolder) {
						bool v = CliOutput.ReadConfirm("Could not find exe in system path. Add now?");

						if (v) {
							RuntimeInfo.Setup();
							return;
						}
					}

					// // Add command and icon to command
					// string[] commandCode =
					// {
					// 	"@echo off",
					// 	$"reg.exe add {RuntimeInfo.REG_SHELL_CMD} /ve /d \"{fullPath} \"\"%%1\"\"\" /f >nul",
					// 	$"reg.exe add {RuntimeInfo.REG_SHELL} /v Icon /d \"{fullPath}\" /f >nul"
					// };
					//
					// Cli.CreateRunBatchFile("add_to_menu.bat", commandCode);

					RuntimeInfo.Subkey.SetValue("Icon", fullPath);

					var cmd = RuntimeInfo.Subkey.CreateSubKey("command");
					cmd.SetValue(null, String.Format("\"{0}\" \"%1\"", fullPath));

					break;
				case IntegrationOption.Remove:
					// // reg delete HKEY_CLASSES_ROOT\*\shell\SmartImage
					//
					// // const string DEL = @"reg delete HKEY_CLASSES_ROOT\*\shell\SmartImage";
					//
					// string[] code =
					// {
					// 	"@echo off",
					// 	$@"reg.exe delete {RuntimeInfo.REG_SHELL} /f >nul"
					// };
					//
					// Cli.CreateRunBatchFile("rem_from_menu.bat", code);

					Registry.CurrentUser.DeleteSubKeyTree(RuntimeInfo.shell3);

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
					string oldValue = Native.EnvironmentPath;

					string appFolder = RuntimeInfo.AppFolder;

					if (RuntimeInfo.IsAppFolderInPath) return;


					bool appFolderInPath = oldValue.Split(Native.PATH_DELIM).Any(p => p == appFolder);

					string cd = Environment.CurrentDirectory;
					string exe = Path.Combine(cd, RuntimeInfo.NAME_EXE);

					if (!appFolderInPath) {
						string newValue = oldValue + Native.PATH_DELIM + cd;
						Native.EnvironmentPath = newValue;
					}

					break;
				}
				case IntegrationOption.Remove:
					Native.RemoveFromPath(RuntimeInfo.AppFolder);
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(option), option, null);
			}
		}

		/// <summary>
		/// Remove old legacy registry integrations
		/// </summary>
		internal static void RemoveOldRegistry()
		{

			bool added = IsAdded();

			if (added)
			{
				Remove();
			}
			else {
				return;
			}

			bool success = !IsAdded();


			static void Remove()
			{
				string[] code =
				{
					"@echo off",
					$@"reg.exe delete {RuntimeInfo.REG_SHELL} /f >nul"
				};

				Cli.CreateRunBatchFile("rem_from_menu.bat", code);
			}

			static bool IsAdded()
			{
				string cmdStr = String.Format(@"reg query {0}", RuntimeInfo.REG_SHELL_CMD);
				var cmd = Cli.Shell(cmdStr, true);

				string[] stdOut = Cli.ReadAllLines(cmd.StandardOutput);

				bool b = stdOut.Any(s => s.Contains(RuntimeInfo.NAME));
				return b;
			}

			if (!success) {
				throw new SmartImageException();
			}
		}

		internal static void ResetIntegrations()
		{

			SearchConfig.Config.Reset();
			SearchConfig.Config.WriteToFile();

			// Computer\HKEY_CLASSES_ROOT\*\shell\SmartImage

			if (RuntimeInfo.IsContextMenuAdded) {
				HandleContextMenu(IntegrationOption.Remove);
			}


			// will be added automatically if run again
			//Path.Remove();

			CliOutput.WriteSuccess("Reset config");
		}

		internal static void Uninstall()
		{
			// autonomous uninstall routine

			// self destruct

			// todo: optimize this

			string batchCommands = String.Empty;
			string exeFileName = RuntimeInfo.ExeLocation;
			const string DEL_BAT_NAME = "SmartImage_Delete.bat";


			batchCommands += "@echo off\n";

			/* Wait approximately 4 seconds (so that the process is already terminated) */
			batchCommands += "ping 127.0.0.1 > nul\n";

			/* Delete executable */
			batchCommands += "echo y | del /F ";

			batchCommands += exeFileName + "\n";

			/* Delete this bat file */
			batchCommands += "echo y | del " + DEL_BAT_NAME;

			var dir = Path.Combine(Path.GetTempPath(), DEL_BAT_NAME);

			File.WriteAllText(dir, batchCommands);

			Process.Start(dir);
		}
	}
}