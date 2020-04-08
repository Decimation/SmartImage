using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Permissions;
using Microsoft.Win32;
using SmartImage.Utilities;

namespace SmartImage
{
	internal static class Config
	{
		internal const string NAME = "SmartImage";

		internal const string NAME_EXE = "SmartImage.exe";

		private const string SUBKEY = @"SOFTWARE\SmartImage";

		private const string CLIENT_ID_STR     = "client_id";
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

		internal static (string, string) ImgurAuth {
			get {
				var key = SubKey;

				var id     = (string) key.GetValue(CLIENT_ID_STR);
				var secret = (string) key.GetValue(CLIENT_SECRET_STR);

				key.Close();

				return (id, secret);
			}
			set {
				var key = SubKey;

				var (id, secret) = value;


				key.SetValue(CLIENT_ID_STR, id);
				key.SetValue(CLIENT_SECRET_STR, secret);

				key.Close();
			}
		}

		internal static string SauceNaoAuth {
			get {
				var key = SubKey;

				var id = (string) key.GetValue(SAUCENAO_APIKEY_STR);

				key.Close();

				return id;
			}
			set {
				var key = SubKey;

				var id = value;

				key.SetValue(SAUCENAO_APIKEY_STR, id);

				key.Close();
			}
		}

		private static RegistryKey SubKey => Registry.CurrentUser.CreateSubKey(SUBKEY);

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


		// Computer\HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\Environment

		internal static void AddToContextMenu()
		{
			var fullPath = Common.GetExecutableLocation(NAME_EXE);

			if (fullPath == null) {
				Cli.WriteInfo("Could not find exe in system path! Add the exe to a folder in %PATH%.");

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
			ImgurAuth       = (String.Empty, String.Empty);

			// Computer\HKEY_CLASSES_ROOT\*\shell\SmartImage

			RemoveFromContextMenu();
		}
	}
}