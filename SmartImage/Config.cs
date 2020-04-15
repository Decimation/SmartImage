using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Permissions;
using Microsoft.Win32;
using Neocmd;
using SmartImage.Engines;
using SmartImage.Searching;
using SmartImage.Utilities;

namespace SmartImage
{
	public static class Config
	{
		public const string NAME = "SmartImage";

		public const string NAME_EXE = "SmartImage.exe";

		public const string Readme = "https://github.com/Decimation/SmartImage/blob/master/README.md";

		private const string REG_SUBKEY = @"SOFTWARE\SmartImage";

		private const string REG_SHELL = @"HKEY_CLASSES_ROOT\*\shell\SmartImage\";

		private const string REG_SHELL_CMD = @"HKEY_CLASSES_ROOT\*\shell\SmartImage\command";

		private const string REG_IMGUR_CLIENT_ID = "imgur_client_id";

		private const string REG_SAUCENAO_APIKEY = "saucenao_key";

		private const string REG_SEARCH_ENGINES = "search_engines";

		private const string REG_PRIORITY_ENGINES = "priority_engines";

		internal static string AppFolder {
			get {
				var folder = Path.GetDirectoryName(Location);


				return folder;
			}
		}


		internal static bool IsExeInAppFolder => File.Exists(Path.Combine(AppFolder, NAME_EXE));

		/// <summary>
		/// Null if executable is not in path.
		/// </summary>
		internal static string Location => FindExecutableLocation(NAME_EXE);

		internal static bool IsContextMenuAdded {
			get {
				var cmdStr = string.Format(@"reg query {0}", REG_SHELL_CMD);
				var cmd    = Cli.Shell(cmdStr, true);

				var stdOut = Cli.ReadAllLines(cmd.StandardOutput);

				// todo
				if (stdOut.Any(s => s.Contains(NAME))) {
					return true;
				}

				var stdErr = Cli.ReadAllLines(cmd.StandardError);

				if (stdErr.Any(s => s.Contains("ERROR"))) {
					return false;
				}


				throw new InvalidOperationException();
			}
		}

		internal static bool IsAppFolderInPath {
			get {
				string dir = Win32.GetEnvironmentPath()?.Split(';').FirstOrDefault(s => s == AppFolder);

				return !String.IsNullOrWhiteSpace(dir);
			}
		}

		private static RegistryConfig RegConfig { get; } = new RegistryConfig(REG_SUBKEY);

		internal static SearchEngines SearchEngines {
			get => RegConfig.Read(REG_SEARCH_ENGINES, true, SearchEngines.All);
			set => RegConfig.Write(REG_SEARCH_ENGINES, value);
		}

		internal static SearchEngines PriorityEngines {
			get => RegConfig.Read(REG_PRIORITY_ENGINES, true, SearchEngines.None);
			set => RegConfig.Write(REG_PRIORITY_ENGINES, value);
		}

		internal static AuthInfo ImgurAuth {
			get {
				string id = RegConfig.Read<string>(REG_IMGUR_CLIENT_ID);

				return new AuthInfo(id);
			}
			set { RegConfig.Write(REG_IMGUR_CLIENT_ID, value.Id); }
		}

		internal static AuthInfo SauceNaoAuth {
			get {
				string id = RegConfig.Read<string>(REG_SAUCENAO_APIKEY);
				return new AuthInfo(id);
			}
			set => RegConfig.Write(REG_SAUCENAO_APIKEY, value.Id);
		}

		private static void RemoveFromContextMenu()
		{
			// reg delete HKEY_CLASSES_ROOT\*\shell\SmartImage

			// const string DEL = @"reg delete HKEY_CLASSES_ROOT\*\shell\SmartImage";

			string[] code =
			{
				"@echo off",
				string.Format(@"reg.exe delete {0} /f >nul", REG_SHELL)
			};

			Cli.CreateRunBatchFile("rem_from_menu.bat", code);
		}

		internal static void AddToPath()
		{
			var oldValue = Win32.GetEnvironmentPath();

			var appFolder = AppFolder;

			if (IsAppFolderInPath) {
				CliOutput.WriteInfo("Executable is already in path: {0}", Location);
				return;
			}


			bool appFolderInPath = oldValue.Split(';').Any(p => p == appFolder);


			var cd  = Environment.CurrentDirectory;
			var exe = Path.Combine(cd, NAME_EXE);


			if (appFolderInPath) {
				CliOutput.WriteInfo("App folder already in path: {0}", appFolder);
			}
			else {
				var newValue = oldValue + @";" + cd;
				Win32.SetEnvironmentPath(newValue);
				CliOutput.WriteInfo("Added {0} to path", cd);
			}


			var dest = Path.Combine(appFolder, NAME_EXE);


			// todo: fix
			//Common.TryMove(exe, dest);
			// Can't reload environment variables immediately
			//Console.WriteLine("Global alloc: {0}", Alloc.Count);
			//Environment.Exit(0);

			return;
		}

		internal static void Info()
		{
			Console.Clear();

			CliOutput.WriteInfo("Search engines: {0}", SearchEngines);
			CliOutput.WriteInfo("Priority engines: {0}", PriorityEngines);

			var sn = SauceNaoAuth;

			CliOutput.WriteInfo("SauceNao authentication: {0} ({1})", sn.IsNull ? CliOutput.MUL_SIGN.ToString() : sn.Id,
			                    sn.IsNull ? "Basic" : "Advanced");

			var imgur = ImgurAuth;
			CliOutput.WriteInfo("Imgur authentication: {0}", imgur.IsNull ? CliOutput.MUL_SIGN.ToString() : imgur.Id);

			CliOutput.WriteInfo("Image upload service: {0}", imgur.IsNull ? "ImgOps" : "Imgur");

			CliOutput.WriteInfo("Application folder: {0}", AppFolder);
			CliOutput.WriteInfo("Executable location: {0}", Location);
			CliOutput.WriteInfo("Context menu integrated: {0}", IsContextMenuAdded);
			CliOutput.WriteInfo("In path: {0}", IsAppFolderInPath);

			CliOutput.WriteInfo("Readme: {0}", Readme);
			CliOutput.WriteInfo("Supported search engines: {0}", SearchEngines.All);
		}

		internal static void AddToContextMenu()
		{
			var fullPath = Location;

			if (!IsExeInAppFolder) {
				var v = CliOutput.ReadConfirm("Could not find exe in system path. Add now?");

				if (v) {
					AddToPath();
					return;
				}

				if (fullPath == null) {
					throw new ApplicationException();
				}
			}


			// Add command
			string[] commandCode =
			{
				"@echo off",
				//String.Format("SET \"SMARTIMAGE={0}\"", fullPath),
				//"SET COMMAND=%SMARTIMAGE% \"\"%%1\"\"",
				string.Format("reg.exe add {0} /ve /d \"{1} \"\"%%1\"\"\" /f >nul", REG_SHELL_CMD, fullPath)
			};

			Cli.CreateRunBatchFile("add_to_menu.bat", commandCode);


			// Add icon
			string[] iconCode =
			{
				"@echo off",
				//String.Format("SET \"SMARTIMAGE={0}\"", fullPath),
				//"SET ICO=%SMARTIMAGE%",
				String.Format("reg.exe add {0} /v Icon /d \"{1}\" /f >nul", REG_SHELL, fullPath),
			};

			Cli.CreateRunBatchFile("add_icon_to_menu.bat", iconCode);
		}

		internal static void RemoveFromPath()
		{
			var oldValue = Win32.GetEnvironmentPath();


			var newValue = oldValue.Replace(";" + AppFolder, String.Empty);


			Win32.SetEnvironmentPath(newValue);
		}

		internal static void Reset(bool all = false)
		{
			SearchEngines   = SearchEngines.All;
			PriorityEngines = SearchEngines.SauceNao;
			ImgurAuth       = AuthInfo.Null;
			SauceNaoAuth    = AuthInfo.Null;

			// Computer\HKEY_CLASSES_ROOT\*\shell\SmartImage

			RemoveFromContextMenu();

			if (all) {
				RemoveFromPath();
				CliOutput.WriteSuccess("Removed from path");
				return;
			}

			Info();
		}

		internal static void Check()
		{
			//var files = AppFolder.GetFiles("*.exe").Any(f => f.Name == NAME_EXE);

			//CliOutput.WriteInfo("{0}", files);

			//var l = new FileInfo(Location);
			//bool exeInAppFolder = l.DirectoryName == AppFolder.Name;
		}

		private static string GetPath(string exe)
		{
			string dir = Win32.GetEnvironmentPath().Split(';')
			                  .FirstOrDefault(s => File.Exists(Path.Combine(s, exe)));

			if (!String.IsNullOrWhiteSpace(dir)) {
				return Path.Combine(dir, exe);
			}

			return null;
		}

		internal static string FindExecutableLocation(string exe)
		{
			var path = GetPath(exe);

			if (path == null) {
				var cd    = Environment.CurrentDirectory;
				var cdExe = Path.Combine(cd, exe);
				var inCd  = File.Exists(cdExe);

				if (inCd) {
					return cdExe;
				}

				else {
					var appFolderExe = Path.Combine(AppFolder, exe);
					var inAppFolder  = File.Exists(appFolderExe);
					if (inAppFolder) {
						return appFolderExe;
					}
				}
			}


			return path;
		}
	}
}