using Novus.Win32;
using SimpleCore.Cli;
using System;
using System.Diagnostics;
using System.Linq;
using Novus.Utilities;
// ReSharper disable UnusedMember.Global

namespace SmartImage.Core
{
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
				Trace.WriteLine("Context menu error: {0}", e.Message);
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
	}
}