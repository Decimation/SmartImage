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

			switch (option) {
				case IntegrationOption.Add:
					string fullPath = RuntimeInfo.ExeLocation;

					if (!RuntimeInfo.IsExeInAppFolder) {
						bool v = CliOutput.ReadConfirm("Could not find exe in system path. Add now?");

						if (v) {
							Setup();
							return;
						}
					}


					RegistrySubKey.SetValue("Icon", fullPath);

					var cmd = RegistrySubKey.CreateSubKey("command");
					cmd.SetValue(null, String.Format("\"{0}\" \"%1\"", fullPath));

					break;
				case IntegrationOption.Remove:

					Registry.CurrentUser.DeleteSubKeyTree(REG_HKCU_SHELL_SMARTIMAGE);

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
			const string REG_SHELL_LEGACY = @"HKEY_CLASSES_ROOT\*\shell\SmartImage\";

			const string REG_SHELL_CMD_LEGACY = @"HKEY_CLASSES_ROOT\*\shell\SmartImage\command";

			bool added = IsAdded();

			if (added) {
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
					$@"reg.exe delete {REG_SHELL_LEGACY} /f >nul"
				};

				Cli.CreateRunBatchFile("rem_from_menu.bat", code);
			}

			static bool IsAdded()
			{
				string cmdStr = String.Format(@"reg query {0}", REG_SHELL_CMD_LEGACY);
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

			if (IsContextMenuAdded) {
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

			string exeFileName = RuntimeInfo.ExeLocation;
			const string DEL_BAT_NAME = "SmartImage_Delete.bat";

			string[] commands =
			{
				"@echo off",

				/* Wait approximately 4 seconds (so that the process is already terminated) */
				"ping 127.0.0.1 > nul",

				/* Delete executable */
				"echo y | del /F " + exeFileName,

				/* Delete this bat file */
				"echo y | del " + DEL_BAT_NAME
			};

			var dir = Path.Combine(Path.GetTempPath(), DEL_BAT_NAME);

			File.WriteAllText(dir, commands.QuickJoin("\n"));

			
			// Runs in background
			Process.Start(dir);
		}

		private const string REG_HKCU_SHELL_ROOT = @"Software\Classes\*\shell\";

		private const string REG_HKCU_SHELL_SMARTIMAGE = REG_HKCU_SHELL_ROOT + @"SmartImage\";

		internal static RegistryKey RegistrySubKey => Registry.CurrentUser.CreateSubKey(REG_HKCU_SHELL_SMARTIMAGE);

		internal static bool IsContextMenuAdded
		{
			get
			{
				var shell = Registry.CurrentUser.OpenSubKey(REG_HKCU_SHELL_ROOT)!;
				return shell.GetSubKeyNames().Contains(RuntimeInfo.NAME);
			}
		}

		internal static void Setup()
		{
			if (!RuntimeInfo.IsAppFolderInPath) {
				Integration.HandlePath(IntegrationOption.Add);
			}
		}
	}
}