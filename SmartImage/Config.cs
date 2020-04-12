using System;
using System.Diagnostics;
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

		internal static string AppFolder {
			get {
				var folder = Path.GetDirectoryName(Location);

				return folder;
			}
		}

		//internal static bool IsExeInPath => GetPath(NAME_EXE) != null;

		internal static bool IsExeInAppFolder => File.Exists(Path.Combine(AppFolder, NAME_EXE));

		/// <summary>
		/// Null if executable is not in path.
		/// </summary>
		internal static string Location => FindExecutableLocation(NAME_EXE);

		internal static bool IsContextMenuAdded {
			get {
				var cmdStr = @"reg query HKEY_CLASSES_ROOT\*\shell\SmartImage\command";
				var cmd    = Cli.Shell(cmdStr, true);

				var stdOut = Cli.ReadAllLines(cmd.StandardOutput);

				// todo
				if (stdOut != null && stdOut.Any(s => s.Contains(NAME))) {
					return true;
				}

				var stdErr = Cli.ReadAllLines(cmd.StandardError);

				if (stdErr != null && stdErr.Any(s => s.Contains("ERROR"))) {
					return false;
				}


				throw new InvalidOperationException();
			}
		}

		internal static bool IsAppFolderInPath {
			get {
				string dir = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User)
				                       ?.Split(';')
				                        .FirstOrDefault(s => s == AppFolder);

				return !String.IsNullOrWhiteSpace(dir);
			}
		}

		private static void RemoveFromContextMenu()
		{
			// reg delete HKEY_CLASSES_ROOT\*\shell\SmartImage

			// const string DEL = @"reg delete HKEY_CLASSES_ROOT\*\shell\SmartImage";

			string[] code =
			{
				"@echo off",
				@"reg.exe delete HKEY_CLASSES_ROOT\*\shell\SmartImage\ /f >nul",
			};

			var bat = Cli.CreateBatchFile("rem_from_menu.bat", code);

			Cli.RunBatchFile(bat);
		}

		internal static void AddToPath()
		{
			var name     = "PATH";
			var scope    = EnvironmentVariableTarget.User;
			var oldValue = Environment.GetEnvironmentVariable(name, scope);

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
				Environment.SetEnvironmentVariable(name, newValue, scope);
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
			string[] code =
			{
				"@echo off",
				String.Format("SET \"SMARTIMAGE={0}\"", fullPath),
				"SET COMMAND=%SMARTIMAGE% \"\"%%1\"\"",
				"reg.exe ADD HKEY_CLASSES_ROOT\\*\\shell\\SmartImage\\command /ve /d \"%COMMAND%\" /f >nul",
			};

			var bat = Cli.CreateBatchFile("add_to_menu.bat", code);

			Cli.RunBatchFile(bat);


			// Add icon
			string[] iconReg =
			{
				"@echo off",
				String.Format("SET \"SMARTIMAGE={0}\"", fullPath),
				"SET ICO=%SMARTIMAGE%",
				"reg.exe add HKEY_CLASSES_ROOT\\*\\shell\\SmartImage /v Icon /d \"%ICO%\" /f >nul",
			};

			var iconBat = Cli.CreateBatchFile("add_icon_to_menu.bat", iconReg);

			Cli.RunBatchFile(iconBat);
		}

		internal static void RemoveFromPath()
		{
			var name     = "PATH";
			var scope    = EnvironmentVariableTarget.User;
			var oldValue = Environment.GetEnvironmentVariable(name, scope);


			var newValue = oldValue.Replace(";" + AppFolder, String.Empty);


			Environment.SetEnvironmentVariable(name, newValue, scope);
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
			string dir = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User)
			                       ?.Split(';')
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

		private const string REG_SUBKEY = @"SOFTWARE\SmartImage";

		private const string REG_IMGUR_CLIENT_ID     = "imgur_client_id";
		private const string REG_IMGUR_CLIENT_SECRET = "imgur_client_secret";
		private const string REG_SAUCENAO_APIKEY     = "saucenao_key";
		private const string REG_SEARCH_ENGINES      = "search_engines";
		private const string REG_PRIORITY_ENGINES    = "priority_engines";

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
				string id     = RegConfig.Read<string>(REG_IMGUR_CLIENT_ID);
				string secret = RegConfig.Read<string>(REG_IMGUR_CLIENT_SECRET);

				return new AuthInfo(id, secret);
			}
			set {
				RegConfig.Write(REG_IMGUR_CLIENT_ID, value.Id);
				RegConfig.Write(REG_IMGUR_CLIENT_SECRET, value.Secret);
			}
		}

		internal static AuthInfo SauceNaoAuth {
			get {
				string id = RegConfig.Read<string>(REG_SAUCENAO_APIKEY);
				return new AuthInfo(id, null);
			}
			set => RegConfig.Write(REG_SAUCENAO_APIKEY, value.Id);
		}
	}
}