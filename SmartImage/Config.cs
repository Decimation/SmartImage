using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Permissions;
using Microsoft.Win32;
using SmartImage.Engines;
using SmartImage.Utilities;

namespace SmartImage
{
	internal static class Config
	{
		internal const string NAME = "SmartImage";

		internal const string NAME_EXE = "SmartImage.exe";

		private const string SUBKEY = @"SOFTWARE\SmartImage";

		private const string CLIENT_ID_STR = "client_id";

		private const string CLIENT_SECRET_STR = "client_secret";

		private const string SAUCENAO_APIKEY_STR = "saucenao_key";

		private const string SEARCH_ENGINES_STR = "search_engines";

		private const string PRIORITY_ENGINES_STR = "priority_engines";

		internal static SearchEngines SearchEngines {
			get {
				var key = SubKey;

				var str = (string) key.GetValue(SEARCH_ENGINES_STR);

				// todo: config automatically, set defaults
				if (str == null) {
					Cli.WriteError("Search engines have not been configured!");

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

		internal static DirectoryInfo AppFolder {
			get {
				var app    = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
				var folder = Path.Combine(app, NAME);
				var di     = new DirectoryInfo(folder);

				if (!di.Exists) {
					di.Create();
				}

				return di;
			}
		}


		/// <summary>
		/// Null if executable is not in path.
		/// </summary>
		internal static string Location => Common.GetExecutableLocation(NAME_EXE);

		private static void RemoveFromContextMenu()
		{
			// reg delete HKEY_CLASSES_ROOT\*\shell\SmartImage

			// const string DEL = @"reg delete HKEY_CLASSES_ROOT\*\shell\SmartImage";

			string[] code =
			{
				"@echo off",
				@"%SystemRoot%\System32\reg.exe delete HKEY_CLASSES_ROOT\*\shell\SmartImage\ /f >nul",
				//"pause"
			};

			var bat = Common.CreateBatchFile("rem_from_menu.bat", code);

			Common.RunBatchFile(bat);
		}

		internal static void AddToPath()
		{
			if (Location != null) {
				Cli.WriteInfo("Executable is already in path: {0}", Location);
				return;
			}

			var name     = "PATH";
			var scope    = EnvironmentVariableTarget.User;
			var oldValue = Environment.GetEnvironmentVariable(name, scope);


			//var cd = Directory.GetCurrentDirectory();
			//var cd = Assembly.GetExecutingAssembly().Location;
			//var cd = Assembly.GetExecutingAssembly().CodeBase;
			//var cd = AppDomain.CurrentDomain.BaseDirectory;
			//var cd = Assembly.GetEntryAssembly().CodeBase;


			var cd  = Environment.CurrentDirectory;
			var exe = Path.Combine(cd, NAME_EXE);

			var appFolder = AppFolder;

			bool b = Cli.Confirm("Add {0} to environment path?", appFolder.FullName);

			if (b) {
				var newValue = oldValue + @";" + appFolder.FullName;
				Environment.SetEnvironmentVariable(name, newValue, scope);

				var dest = Path.Combine(appFolder.FullName, NAME_EXE);

				Cli.WriteInfo("Moving executable from {0} to {1}", exe, dest);
				File.Move(exe, dest);


				Cli.WriteSuccess("Success. Relaunch the program for changes to take effect.");

				// Can't reload environment variables immediately
				Environment.Exit(0);
			}
			else {
				Cli.WriteError("Cancelled");
			}
		}

		internal static void Info()
		{
			Console.Clear();

			Cli.WriteInfo("Search engines: {0}", SearchEngines);
			Cli.WriteInfo("Priority engines: {0}", PriorityEngines);

			var sn = SauceNaoAuth;
			
			Cli.WriteInfo("SauceNao authentication: {0} ({1})", sn.IsNull ? Cli.MUL_SIGN.ToString() : sn.Id,
			              sn.IsNull ? "Basic" : "Advanced");

			var imgur = ImgurAuth;
			Cli.WriteInfo("Imgur authentication: {0}", imgur.IsNull ? Cli.MUL_SIGN.ToString() : imgur.Id);

			Cli.WriteInfo("Image upload service: {0}", imgur.IsNull ? "ImgOps" : "Imgur");

			Cli.WriteInfo("Application folder: {0}", AppFolder);
			Cli.WriteInfo("Executable location: {0}", Location);
			Cli.WriteInfo("Context menu integrated: {0}", ContextMenuAdded);
		}

		internal static bool ContextMenuAdded {
			get {
				var cmdStr = @"reg query HKEY_CLASSES_ROOT\*\shell\SmartImage\command";
				var cmd    = Common.Shell(cmdStr, true);

				var stdOut = Common.ReadAllLines(cmd.StandardOutput);

				if (stdOut != null && stdOut.Any(s => s.Contains(NAME))) {
					return true;
				}

				var stdErr = Common.ReadAllLines(cmd.StandardError);

				if (stdErr != null && stdErr.Any(s => s.Contains("ERROR"))) {
					return false;
				}
				
				
				throw new InvalidOperationException();
			}
		}

		// Computer\HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\Environment

		internal static void AddToContextMenu()
		{
			var fullPath = Location;

			if (fullPath == null) {
				var v = Cli.Confirm("Could not find exe in system path. Add now?");

				if (v) {
					AddToPath();
					return;
				}

				//AddToPath();
				//fullPath = Common.GetExecutableLocation(NAME_EXE);

				if (fullPath == null) {
					throw new ApplicationException();
				}
			}


			string[] code =
			{
				"@echo off",
				//"SET \"SMARTIMAGE=SmartImage.exe\"",
				string.Format("SET \"SMARTIMAGE={0}\"", fullPath),
				"SET COMMAND=%SMARTIMAGE% \"\"%%1\"\"",
				"%SystemRoot%\\System32\\reg.exe ADD HKEY_CLASSES_ROOT\\*\\shell\\SmartImage\\command /ve /d \"%COMMAND%\" /f >nul",
				//"pause"
			};

			var bat = Common.CreateBatchFile("add_to_menu.bat", code);

			Common.RunBatchFile(bat);
		}

		internal static void Reset()
		{
			SearchEngines   = SearchEngines.All;
			PriorityEngines = SearchEngines.SauceNao;
			ImgurAuth       = AuthInfo.Null;
			SauceNaoAuth    = AuthInfo.Null;

			// Computer\HKEY_CLASSES_ROOT\*\shell\SmartImage

			RemoveFromContextMenu();
			
			Info();
		}
	}
}