global using R2 = SmartImage.UI.Resources;
global using R1 = SmartImage.Lib.Resources;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;
using Novus.OS;

namespace SmartImage.UI;

public static class AppUtil
{
	#region

	public static string ExeLocation
	{
		get
		{
			var module = Process.GetCurrentProcess().MainModule;

			// Require.NotNull(module);
			Trace.Assert(module != null);
			return module.FileName;
		}
	}

	public static Version Version => Assembly.GetExecutingAssembly().GetName().Version;

	public static string CurrentAppFolder => Path.GetDirectoryName(ExeLocation);

	public static bool IsAppFolderInPath => FileSystem.IsFolderInPath(CurrentAppFolder);

	public static bool IsOnTop { get; private set; }

	#endregion

	public static bool IsContextMenuAdded
	{
		get
		{
			var reg = Registry.CurrentUser.OpenSubKey(R1.Reg_Shell_Cmd);
			return reg != null;

			return false;
		}
	}

	/*
	 * HKEY_CLASSES_ROOT is an alias, a merging, of two other locations:
	 *		HKEY_CURRENT_USER\Software\Classes
	 *		HKEY_LOCAL_MACHINE\Software\Classes
	 */

	/// <returns><c>true</c> if operation succeeded; <c>false</c> otherwise</returns>
	public static bool HandleContextMenu(bool option)
	{
		/*
		 * New context menu
		 */
		switch (option)
		{
			case true:

				RegistryKey regMenu = null;
				RegistryKey regCmd  = null;

				string fullPath = ExeLocation;

				try
				{
					regMenu = Registry.CurrentUser.CreateSubKey(R1.Reg_Shell);
					regMenu?.SetValue(String.Empty, R1.Name);
					regMenu?.SetValue("Icon", $"\"{fullPath}\"");

					regCmd = Registry.CurrentUser.CreateSubKey(R1.Reg_Shell_Cmd);

					regCmd?.SetValue(String.Empty,
					                 $"\"{fullPath}\" -i \"%1\" -as");
				}
				catch (Exception ex)
				{
					Trace.WriteLine($"{ex.Message}");
					return false;
				}
				finally
				{
					regMenu?.Close();
					regCmd?.Close();
				}

				break;
			case false:

				try
				{
					var reg = Registry.CurrentUser.OpenSubKey(R1.Reg_Shell_Cmd);

					if (reg != null)
					{
						reg.Close();
						Registry.CurrentUser.DeleteSubKey(R1.Reg_Shell_Cmd);
					}

					reg = Registry.CurrentUser.OpenSubKey(R1.Reg_Shell);

					if (reg != null)
					{
						reg.Close();
						Registry.CurrentUser.DeleteSubKey(R1.Reg_Shell);
					}
				}
				catch (Exception ex)
				{
					Trace.WriteLine($"{ex.Message}");

					return false;
				}

				break;

		}
			
		return false;

	}
}