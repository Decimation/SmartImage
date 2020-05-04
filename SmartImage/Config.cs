#region

#nullable enable
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Neocmd;
using Newtonsoft.Json.Linq;
using RestSharp;
using SmartImage.Model;
using SmartImage.Searching;
using SmartImage.Utilities;

// ReSharper disable MemberCanBePrivate.Global

#endregion


namespace SmartImage
{
	public static class Config
	{
		public const string NAME = "SmartImage";

		public const string NAME_EXE = "SmartImage.exe";

		public const string NAME_CFG = "smartimage.cfg";

		public const string Readme = "https://github.com/Decimation/SmartImage/blob/master/README.md";

		private const string REG_SUBKEY = @"SOFTWARE\SmartImage";

		private const string REG_SHELL = @"HKEY_CLASSES_ROOT\*\shell\SmartImage\";

		private const string REG_SHELL_CMD = @"HKEY_CLASSES_ROOT\*\shell\SmartImage\command";

		private const string CFG_IMGUR_CLIENT_ID = "imgur_client_id";

		private const string CFG_SAUCENAO_APIKEY = "saucenao_key";

		private const string CFG_SEARCH_ENGINES = "search_engines";

		private const string CFG_PRIORITY_ENGINES = "priority_engines";

		public static string AppFolder {
			get {
				string? folder = Path.GetDirectoryName(ExeLocation);
				return folder;
			}
		}

		internal static void Setup()
		{
			if (!File.Exists(ConfigLocation)) {
				var f = File.Create(ConfigLocation);
				f.Close();
				Reset();
			}
		}

		public static string ConfigLocation {
			get { return Path.Combine(AppFolder, NAME_CFG); }
		}


		public static bool IsExeInAppFolder => File.Exists(Path.Combine(AppFolder, NAME_EXE));

		/// <summary>
		///     Null if executable is not in path.
		/// </summary>
		public static string ExeLocation => FindExecutableLocation(NAME_EXE);

		public static bool IsContextMenuAdded {
			get {
				string cmdStr = String.Format(@"reg query {0}", REG_SHELL_CMD);
				var    cmd    = Cli.Shell(cmdStr, true);

				string[] stdOut = Cli.ReadAllLines(cmd.StandardOutput);

				// todo
				if (stdOut.Any(s => s.Contains(NAME))) {
					return true;
				}

				string[] stdErr = Cli.ReadAllLines(cmd.StandardError);

				if (stdErr.Any(s => s.Contains("ERROR"))) {
					return false;
				}


				throw new InvalidOperationException();
			}
		}

		public static bool IsAppFolderInPath => ExplorerSystem.IsFolderInPath(AppFolder);

		public static ConfigFile RegConfig { get; } = new ConfigFile(ConfigLocation);

		public static SearchEngines SearchEngines {
			get => RegConfig.Read(CFG_SEARCH_ENGINES, true, SearchEngines.All);
			set => RegConfig.Write(CFG_SEARCH_ENGINES, value);
		}

		public static SearchEngines PriorityEngines {
			get => RegConfig.Read(CFG_PRIORITY_ENGINES, true, SearchEngines.None);
			set => RegConfig.Write(CFG_PRIORITY_ENGINES, value);
		}

		public static AuthInfo ImgurAuth {
			get {
				string id = RegConfig.Read<string>(CFG_IMGUR_CLIENT_ID);

				return new AuthInfo(id);
			}
			set => RegConfig.Write(CFG_IMGUR_CLIENT_ID, value.Id);
		}

		public static AuthInfo SauceNaoAuth {
			get {
				string id = RegConfig.Read<string>(CFG_SAUCENAO_APIKEY);
				return new AuthInfo(id);
			}
			set => RegConfig.Write(CFG_SAUCENAO_APIKEY, value.Id);
		}


		public static ReleaseInfo LatestRelease()
		{
			// todo
			var rc = new RestClient("https://api.github.com/");
			var re = new RestRequest("repos/Decimation/SmartImage/releases");
			var rs = rc.Execute(re);
			var ja = JArray.Parse(rs.Content);

			var first = ja[0];


			var tagName = first["tag_name"];
			var url     = first["html_url"];
			var publish = first["published_at"];

			var r = new ReleaseInfo(tagName.ToString(), url.ToString(), publish.ToString());
			return r;
		}

		public static void RemoveFromContextMenu()
		{
			// reg delete HKEY_CLASSES_ROOT\*\shell\SmartImage

			// const string DEL = @"reg delete HKEY_CLASSES_ROOT\*\shell\SmartImage";

			string[] code =
			{
				"@echo off",
				String.Format(@"reg.exe delete {0} /f >nul", REG_SHELL)
			};

			Cli.CreateRunBatchFile("rem_from_menu.bat", code);
		}

		public static void AddToPath()
		{
			string oldValue = ExplorerSystem.EnvironmentPath;

			string appFolder = AppFolder;

			if (IsAppFolderInPath) {
				CliOutput.WriteInfo("Executable is already in path: {0}", ExeLocation);
				return;
			}


			bool appFolderInPath = oldValue.Split(ExplorerSystem.PATH_DELIM).Any(p => p == appFolder);


			string cd  = Environment.CurrentDirectory;
			string exe = Path.Combine(cd, NAME_EXE);


			if (appFolderInPath) {
				CliOutput.WriteInfo("App folder already in path: {0}", appFolder);
			}
			else {
				string newValue = oldValue + ExplorerSystem.PATH_DELIM + cd;
				ExplorerSystem.EnvironmentPath = newValue;
				CliOutput.WriteInfo("Added {0} to path", cd);
			}
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
			CliOutput.WriteInfo("Executable location: {0}", ExeLocation);
			CliOutput.WriteInfo("Config location: {0}", ConfigLocation);
			CliOutput.WriteInfo("Context menu integrated: {0}", IsContextMenuAdded);
			CliOutput.WriteInfo("In path: {0}\n", IsAppFolderInPath);

			//

			// CliOutput.WriteInfo("Supported search engines: {0}\n", SearchEngines.All);

			//

			CliOutput.WriteInfo("Readme: {0}", Readme);

			var asm            = typeof(Config).Assembly.GetName();
			var currentVersion = asm.Version;
			CliOutput.WriteInfo("Current version: {0}", currentVersion);

			var release = LatestRelease();
			CliOutput.WriteInfo("Latest version: {0} (tag {1}) ({2})", release.Version, release.TagName,
			                    release.PublishedAt);

			int vcmp = currentVersion.CompareTo(release.Version);

			if (vcmp < 0) {
				CliOutput.WriteInfo("Update available");
			}
			else if (vcmp == 0) {
				CliOutput.WriteInfo("Up to date");
			}
			else if (vcmp > 0) {
				CliOutput.WriteInfo("(preview)");
			}
		}

		public static void AddToContextMenu()
		{
			string fullPath = ExeLocation;

			if (!IsExeInAppFolder) {
				bool v = CliOutput.ReadConfirm("Could not find exe in system path. Add now?");

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
				String.Format("reg.exe add {0} /ve /d \"{1} \"\"%%1\"\"\" /f >nul", REG_SHELL_CMD, fullPath)
			};

			Cli.CreateRunBatchFile("add_to_menu.bat", commandCode);


			// Add icon
			string[] iconCode =
			{
				"@echo off",
				String.Format("reg.exe add {0} /v Icon /d \"{1}\" /f >nul", REG_SHELL, fullPath),
			};

			Cli.CreateRunBatchFile("add_icon_to_menu.bat", iconCode);
		}

		public static void RemoveFromPath() => ExplorerSystem.RemoveFromPath(AppFolder);

		public static void Reset(bool all = false)
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
		}


		private static string FindExecutableLocation(string exe)
		{
			string path = ExplorerSystem.FindExectableInPath(exe);

			if (path == null) {
				string cd = Environment.CurrentDirectory;

				if (Try(cd, exe, out path)) {
					return path;
				}


				// SPECIAL CASE: app folder is not in path, continuing past here causes a stack overflow
				// todo
				

				/*if (Try(AppFolder, exe, out path)) {
					return path;
				}*/
			}

			static bool Try(string folder, string exeStr, out string folderExe)
			{
				string folderExeFull = Path.Combine(folder, exeStr);
				bool   inFolder      = File.Exists(folderExeFull);

				folderExe = folderExeFull;
				return inFolder;
			}

			return path;
		}

		// todo
		private static string FindExecutableLocationOld(string exe, params string[] searchDirs)
		{
			string path = ExplorerSystem.FindExectableInPath(exe);

			if (path == null) {
				foreach (string dir in searchDirs) {
					if (SearchFolder(exe, dir, out var s)) {
						return s;
					}
				}
			}

			return path;
		}

		// todo
		private static bool SearchFolder(string exe, string folder, out string path)
		{
			path = Path.Combine(folder, exe);

			bool inFolder = File.Exists(path);

			if (inFolder) {
				return true;
			}
			else {
				return false;
			}
		}
	}
}