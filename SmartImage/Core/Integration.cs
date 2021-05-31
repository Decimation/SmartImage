using Microsoft.Win32;
using Novus.Win32;
using SimpleCore.Cli;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Novus.Utilities;

// ReSharper disable UnusedMember.Global

namespace SmartImage.Core
{
	/*

	/// <summary>
	///     Legacy integration features
	/// </summary>
	internal static class LegacyIntegration
	{
		/// <summary>
		///     Legacy <see cref="Integration.REG_SHELL" />
		/// </summary>
		private const string REG_SHELL = @"HKEY_CLASSES_ROOT\*\shell\SmartImage\";

		/// <summary>
		///     Legacy <see cref="Integration.REG_SHELL_CMD" />
		/// </summary>
		private const string REG_SHELL_CMD = @"HKEY_CLASSES_ROOT\*\shell\SmartImage\command";

		/// <summary>
		///     Legacy <see cref="Integration.IsContextMenuAdded" />
		/// </summary>
		/// <remarks>
		///     <c>true</c> if context menu was added through legacy integration; <c>false</c> otherwise; <c>null</c> if
		///     indeterminate
		/// </remarks>
		internal static bool? IsContextMenuAdded
		{
			get
			{
				try
				{
					var cmd = Command.Shell(@$"reg query {REG_SHELL_CMD}");

					cmd.Start();

					string[] stdOut = cmd.StandardOutput.ReadAllLines();

					bool b = stdOut.Any(s => s.Contains(Info.NAME));

					return b;
				}
				catch (Exception)
				{
					return null;
				}
			}
		}

		/// <summary>
		///     Legacy <see cref="Integration.HandleContextMenu" />
		/// </summary>
		internal static bool HandleContextMenu(IntegrationOption option)
		{
			try
			{
				switch (option)
				{
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

				return true;
			}
			catch (Exception e)
			{
				Trace.WriteLine($"Context menu error: {e.Message}");
				NConsole.WaitForSecond();
				return false;
			}
		}

		/// <summary>
		///     Cleans up legacy integration features (i.e. context menu) and migrates them to the new integration system.
		/// </summary>
		/// <returns><c>true</c> if operation succeeded; <c>false</c> otherwise</returns>
		internal static bool LegacyCleanup()
		{
			// Convert old context menu integration to new context menu integration

			bool? legacy = IsContextMenuAdded;

			if (!legacy.HasValue)
			{
				Trace.WriteLine("Could not check for legacy features");
				return false;
			}

			if (legacy.Value && !Integration.IsContextMenuAdded)
			{
				Trace.WriteLine("Cleaning up legacy features...");

				bool ok = HandleContextMenu(IntegrationOption.Remove);

				if (ok)
				{
					Trace.WriteLine("Removed legacy context menu");
				}

				Integration.HandleContextMenu(IntegrationOption.Add);

				Trace.WriteLine("Added new context menu");
				NConsole.WaitForSecond();
			}

			return true;
		}
	}*/

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
		/*
		 * HKEY_CLASSES_ROOT is an alias, a merging, of two other locations:
		 *		HKEY_CURRENT_USER\Software\Classes
		 *		HKEY_LOCAL_MACHINE\Software\Classes
		 */


		/// <returns><c>true</c> if operation succeeded; <c>false</c> otherwise</returns>
		internal static bool HandleContextMenu(IntegrationOption option)
		{
			/*
			 * New context menu
			 */

			switch (option) {
				case IntegrationOption.Add:

					RegistryKey regMenu = null;
					RegistryKey regCmd  = null;

					string fullPath = Info.ExeLocation;

					try {
						regMenu = Registry.CurrentUser.CreateSubKey(REG_SHELL);
						regMenu?.SetValue(String.Empty, Info.NAME);
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
						Trace.WriteLine($"{ex.Message}");
						NConsole.WaitForInput();
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
					string oldValue  = FileSystem.EnvironmentPath;
					string appFolder = Info.AppFolder;

					if (Info.IsAppFolderInPath) {
						return;
					}

					bool appFolderInPath = oldValue
					                       .Split(FileSystem.PATH_DELIM)
					                       .Any(p => p == appFolder);

					string cd  = Environment.CurrentDirectory;
					string exe = Path.Combine(cd, Info.NAME_EXE);

					if (!appFolderInPath) {
						string newValue = oldValue + FileSystem.PATH_DELIM + cd;
						FileSystem.EnvironmentPath = newValue;
					}

					break;
				}
				case IntegrationOption.Remove:
					FileSystem.RemoveFromPath(Info.AppFolder);
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(option), option, null);
			}
		}


		internal static void ResetIntegrations()
		{


			// Computer\HKEY_CLASSES_ROOT\*\shell\SmartImage

			if (IsContextMenuAdded) {
				HandleContextMenu(IntegrationOption.Remove);
			}


			// will be added automatically if run again
			//Path.Remove();

			Trace.WriteLine("Reset config");
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