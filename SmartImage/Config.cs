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

		private const string SUBKEY = @"SOFTWARE\SmartImage";

		private const string CLIENT_ID_STR = "client_id";

		private const string CLIENT_SECRET_STR = "client_secret";

		private const string SAUCENAO_APIKEY_STR = "saucenao_key";

		private const string SEARCH_ENGINES_STR = "search_engines";

		private const string PRIORITY_ENGINES_STR = "priority_engines";

		public const string Readme = "https://github.com/Decimation/SmartImage/blob/master/README.md";

		internal static SearchEngines SearchEngines {
			get {
				var key = SubKey;

				var str = (string) key.GetValue(SEARCH_ENGINES_STR);

				// todo: config automatically, set defaults
				if (str == null) {
					CliOutput.WriteError("Search engines have not been configured!");

					SearchEngines = SearchEngines.All;

					return SearchEngines;
				}

				var id = Enum.Parse<SearchEngines>(str);

				key.Close();

				return id;
			}
			set {
				var key = SubKey;

				key.SetValue(SEARCH_ENGINES_STR, value);

				key.Close();
			}
		}

		internal static SearchEngines PriorityEngines {
			get {
				var key = SubKey;

				var str = (string) key.GetValue(PRIORITY_ENGINES_STR);

				if (str == null) {
					return SearchEngines.None;
				}

				var id = Enum.Parse<SearchEngines>(str);

				key.Close();

				return id;
			}
			set {
				var key = SubKey;

				key.SetValue(PRIORITY_ENGINES_STR, value);

				key.Close();
			}
		}


		internal static AuthInfo ImgurAuth {
			get {
				var key = SubKey;

				var id     = (string) key.GetValue(CLIENT_ID_STR);
				var secret = (string) key.GetValue(CLIENT_SECRET_STR);

				key.Close();

				return new AuthInfo(id, secret);
			}
			set {
				var key = SubKey;

				var ai = value;

				key.SetValue(CLIENT_ID_STR, ai.Id);
				key.SetValue(CLIENT_SECRET_STR, ai.Secret);

				key.Close();
			}
		}

		internal static AuthInfo SauceNaoAuth {
			get {
				var key = SubKey;


				var id = (string) key.GetValue(SAUCENAO_APIKEY_STR);

				key.Close();

				return new AuthInfo(id, null);
			}
			set {
				var key = SubKey;

				var id = value;

				key.SetValue(SAUCENAO_APIKEY_STR, id.Id);

				key.Close();
			}
		}

		private static RegistryKey SubKey => Registry.CurrentUser.CreateSubKey(SUBKEY);

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
					Common.KillProc(cmd);
					return true;
				}

				var stdErr = Cli.ReadAllLines(cmd.StandardError);

				if (stdErr != null && stdErr.Any(s => s.Contains("ERROR"))) {
					Common.KillProc(cmd);
					return false;
				}


				Common.KillProc(cmd);


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


			string[] code =
			{
				"@echo off",
				String.Format("SET \"SMARTIMAGE={0}\"", fullPath),
				"SET COMMAND=%SMARTIMAGE% \"\"%%1\"\"",
				"reg.exe ADD HKEY_CLASSES_ROOT\\*\\shell\\SmartImage\\command /ve /d \"%COMMAND%\" /f >nul",
			};

			var bat = Cli.CreateBatchFile("add_to_menu.bat", code);

			Cli.RunBatchFile(bat);
		}

		internal static void RemoveFromPath()
		{
			var name     = "PATH";
			var scope    = EnvironmentVariableTarget.User;
			var oldValue = Environment.GetEnvironmentVariable(name, scope);


			var newValue = oldValue.Replace(";" + AppFolder, string.Empty);


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
				Console.WriteLine("Removed from path");
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
	}
}