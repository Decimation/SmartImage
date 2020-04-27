#region

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Neocmd;
using Newtonsoft.Json.Linq;
using RestSharp;
using SmartImage.Searching;
using SmartImage.Utilities;

#endregion


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
				string? folder = Path.GetDirectoryName(Location);


				return folder;
			}
		}

		internal static void Setup()
		{
			if (!File.Exists(ConfigFile)) {
				var f = File.Create(ConfigFile);
				f.Close();
				Reset();
			}
		}

		internal static string ConfigFile {
			get { return Path.Combine(AppFolder, "smartimage.cfg"); }
		}


		internal static bool IsExeInAppFolder => File.Exists(Path.Combine(AppFolder, NAME_EXE));

		/// <summary>
		///     Null if executable is not in path.
		/// </summary>
		internal static string Location => FindExecutableLocation(NAME_EXE);

		internal static bool IsContextMenuAdded {
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

		internal static bool IsAppFolderInPath => ExplorerSystem.IsFolderInPath(AppFolder);

		private static ConfigFile RegConfig { get; } = new ConfigFile(ConfigFile);

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
			set => RegConfig.Write(REG_IMGUR_CLIENT_ID, value.Id);
		}

		internal static AuthInfo SauceNaoAuth {
			get {
				string id = RegConfig.Read<string>(REG_SAUCENAO_APIKEY);
				return new AuthInfo(id);
			}
			set => RegConfig.Write(REG_SAUCENAO_APIKEY, value.Id);
		}


		internal static ReleaseInfo LatestRelease()
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

		private static void RemoveFromContextMenu()
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

		internal static void AddToPath()
		{
			string oldValue = ExplorerSystem.EnvironmentPath;

			string appFolder = AppFolder;

			if (IsAppFolderInPath) {
				CliOutput.WriteInfo("Executable is already in path: {0}", Location);
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
			CliOutput.WriteInfo("Executable location: {0}", Location);
			CliOutput.WriteInfo("Context menu integrated: {0}", IsContextMenuAdded);
			CliOutput.WriteInfo("In path: {0}\n", IsAppFolderInPath);

			//

			CliOutput.WriteInfo("Supported search engines: {0}\n", SearchEngines.All);

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

		internal static void AddToContextMenu()
		{
			string fullPath = Location;

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

		internal static void RemoveFromPath() => ExplorerSystem.RemoveFromPath(AppFolder);

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
		}

		private static string FindExecutableLocation(string exe)
		{
			string path = ExplorerSystem.FindExectableInPath(exe);

			if (path == null) {
				string cd    = Environment.CurrentDirectory;
				string cdExe = Path.Combine(cd, exe);
				bool   inCd  = File.Exists(cdExe);

				if (inCd) {
					return cdExe;
				}

				string appFolderExe = Path.Combine(AppFolder, exe);
				bool   inAppFolder  = File.Exists(appFolderExe);
				if (inAppFolder) {
					return appFolderExe;
				}
			}


			return path;
		}
	}
}