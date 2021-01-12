using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Microsoft.Win32;
using Novus.Win32;
using SimpleCore.Console.CommandLine;

namespace SmartImage.Core
{
	// todo: move context menu integration to Novus for use in other projects?

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
		/// <returns><c>true</c> if operation succeeded; <c>false</c> otherwise</returns>
		internal static bool HandleContextMenu(IntegrationOption option)
		{
			/*
			 * New context menu
			 */

			switch (option) {
				case IntegrationOption.Add:

					RegistryKey regMenu  = null;
					RegistryKey regCmd   = null;
					
					string      fullPath = Info.ExeLocation;

					try {
						regMenu = Registry.CurrentUser.CreateSubKey(REG_SHELL);
						regMenu?.SetValue(String.Empty, Info.NAME);

						regCmd = Registry.CurrentUser.CreateSubKey(REG_SHELL_CMD);
						regCmd?.SetValue(String.Empty, $"\"{fullPath}\" \"%1\"");
					}
					catch (Exception ex) {
						NConsole.WriteError("{0}", ex.Message);
						NConsoleIO.WaitForInput();
						return false;
					}
					finally {
						regMenu?.Close();
						regCmd?.Close();
					}

					break;
				case IntegrationOption.Remove:

					try {
						var reg = Registry.CurrentUser.OpenSubKey(REG_SHELL_CMD);

						if (reg != null) {
							reg.Close();
							Registry.CurrentUser.DeleteSubKey(REG_SHELL_CMD);
						}

						reg = Registry.CurrentUser.OpenSubKey(REG_SHELL);

						if (reg != null) {
							reg.Close();
							Registry.CurrentUser.DeleteSubKey(REG_SHELL);
						}
					}
					catch (Exception ex) {
						NConsole.WriteError("{0}", ex.Message);
						NConsoleIO.WaitForInput();
						return false;
					}

					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(option), option, null);
			}


			return true;

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

					string cd  = Environment.CurrentDirectory;
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


		private const string REG_SHELL = "SOFTWARE\\Classes\\*\\shell\\SmartImage";

		private const string REG_SHELL_CMD = "SOFTWARE\\Classes\\*\\shell\\SmartImage\\command";

		internal static bool IsContextMenuAdded
		{
			get
			{
				var reg = Registry.CurrentUser.OpenSubKey(REG_SHELL_CMD);

				return reg != null;
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