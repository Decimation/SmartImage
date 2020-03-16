using System;
using System.Diagnostics;
using System.IO;
using Microsoft.Win32;

namespace SmartImage
{
	internal static class Config
	{
		private const string SUBKEY = @"SOFTWARE\SmartImage";

		private const string CLIENT_ID_STR     = "client_id";
		private const string CLIENT_SECRET_STR = "client_secret";
		private const string OPEN_OPTIONS_STR  = "search_engines";

		internal static SearchEngines SearchEngines {
			get {
				var key = SubKey;

				var str = (string) key.GetValue(OPEN_OPTIONS_STR);

				if (str == null) {
					Cli.Error("Search engines have not been configured!");
					return SearchEngines.None;
				}
				
				var id = Enum.Parse<SearchEngines>(str);

				key.Close();

				return id;
			}
			set {
				var key = SubKey;

				key.SetValue(OPEN_OPTIONS_STR, value);

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

		// Probably not a good idea to have this hardcoded lol
		internal static string SauceNaoAuth => "c1f946bb2003c92fa8a25ce7fa923e0f213a0db8";

		private static RegistryKey SubKey => Registry.CurrentUser.CreateSubKey(SUBKEY);

		private static string CreateBatchFile()
		{
			string[] code =
			{
				"@echo off",
				"SET \"SMARTIMAGE=C:\\Library\\SmartImage.exe\"",
				"SET COMMAND=%SMARTIMAGE% \"%%1\"",
				"%SystemRoot%\\System32\\reg.exe ADD HKEY_CLASSES_ROOT\\*\\shell\\SmartImage\\command /ve /d \"%COMMAND%\" /f >nul",
				//"pause"
			};


			var file = Path.Combine(Directory.GetCurrentDirectory(), "add_to_menu.bat");

			File.WriteAllLines(file, code);

			return file;
		}

		internal static void AddToContextMenu()
		{
			string file = CreateBatchFile();
			var process = new Process
			{
				StartInfo = new ProcessStartInfo
				{
					WindowStyle     = ProcessWindowStyle.Hidden,
					FileName        = "cmd.exe",
					Arguments       = "/C \"" + file + "\"",
					Verb            = "runas",
					UseShellExecute = true
				}
			};


			process.Start();
			process.WaitForExit();


			File.Delete(file);
		}
	}
}