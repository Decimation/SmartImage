using Microsoft.Win32;
using Novus.Win32;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using Kantan.Cli;
using Novus.Utilities;
using static Kantan.Diagnostics.LogCategories;

// ReSharper disable CognitiveComplexity

// ReSharper disable InconsistentNaming

// ReSharper disable UnusedMember.Global

namespace SmartImage.Core
{
	public enum IntegrationOption
	{
		Add,
		Remove
	}

	/// <summary>
	/// Program OS integrations
	/// </summary>
	public static class AppIntegration
	{
		/*
		 * HKEY_CLASSES_ROOT is an alias, a merging, of two other locations:
		 *		HKEY_CURRENT_USER\Software\Classes
		 *		HKEY_LOCAL_MACHINE\Software\Classes
		 */


		/// <returns><c>true</c> if operation succeeded; <c>false</c> otherwise</returns>
		public static bool HandleContextMenu(IntegrationOption option)
		{
			/*
			 * New context menu
			 */

			switch (option) {
				case IntegrationOption.Add:

					RegistryKey regMenu = null;
					RegistryKey regCmd  = null;

					string fullPath = AppInfo.ExeLocation;

					try {
						regMenu = Registry.CurrentUser.CreateSubKey(REG_SHELL);
						regMenu?.SetValue(String.Empty, AppInfo.NAME);
						regMenu?.SetValue("Icon", $"\"{fullPath}\"");

						regCmd = Registry.CurrentUser.CreateSubKey(REG_SHELL_CMD);
						regCmd?.SetValue(String.Empty, $"\"{fullPath}\" \"%1\"");
					}
					catch (Exception ex) {
						Trace.WriteLine($"{ex.Message}");
						NConsole.WaitForInput();
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
						Trace.WriteLine($"{ex.Message}", C_ERROR);

						return false;
					}

					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(option), option, null);
			}


			return true;

		}

		public static void HandlePath(IntegrationOption option)
		{
			switch (option) {
				case IntegrationOption.Add:
				{
					string oldValue  = FileSystem.EnvironmentPath;
					string appFolder = AppInfo.AppFolder;

					if (AppInfo.IsAppFolderInPath) {
						return;
					}

					bool appFolderInPath = oldValue
					                       .Split(FileSystem.PATH_DELIM)
					                       .Any(p => p == appFolder);

					string cd  = Environment.CurrentDirectory;
					string exe = Path.Combine(cd, AppInfo.NAME_EXE);

					if (!appFolderInPath) {
						string newValue = oldValue + FileSystem.PATH_DELIM + cd;
						FileSystem.EnvironmentPath = newValue;
					}

					break;
				}
				case IntegrationOption.Remove:
					FileSystem.RemoveFromPath(AppInfo.AppFolder);
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(option), option, null);
			}
		}


		public static void ResetIntegrations()
		{
			// Computer\HKEY_CLASSES_ROOT\*\shell\SmartImage

			if (IsContextMenuAdded) {
				HandleContextMenu(IntegrationOption.Remove);
			}

			Trace.WriteLine("Reset config");
		}

		[DoesNotReturn]
		public static void Uninstall()
		{
			// autonomous uninstall routine

			// self destruct

			string exeFileName = AppInfo.ExeLocation;

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
			var proc = Command.Batch(commands, DEL_BAT_NAME);
			proc.Start();

		}


		private const string REG_SHELL = "SOFTWARE\\Classes\\*\\shell\\SmartImage";

		private const string REG_SHELL_CMD = "SOFTWARE\\Classes\\*\\shell\\SmartImage\\command";

		public static bool IsContextMenuAdded
		{
			get
			{
				var reg = Registry.CurrentUser.OpenSubKey(REG_SHELL_CMD);

				return reg != null;
			}
		}
	}
}