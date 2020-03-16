using System;
using Microsoft.Win32;

namespace SmartImage
{
	internal static class Config
	{
		private const string SUBKEY = @"SOFTWARE\SmartImage";

		private const string CLIENT_ID_STR     = "client_id";
		private const string CLIENT_SECRET_STR = "client_secret";
		private const string OPEN_OPTIONS_STR  = "open_options";

		internal static OpenOptions OpenOptions {
			get {
				var key = GetKey();

				var id = Enum.Parse<OpenOptions>((string) key.GetValue(OPEN_OPTIONS_STR));

				key.Close();

				return id;
			}
			set {
				var key = GetKey();

				key.SetValue(OPEN_OPTIONS_STR, value);

				key.Close();
			}
		}

		internal static (string, string) ImgurAuth {
			get {
				var key = GetKey();

				var id     = (string) key.GetValue(CLIENT_ID_STR);
				var secret = (string) key.GetValue(CLIENT_SECRET_STR);

				key.Close();

				return (id, secret);
			}
			set {
				var key = GetKey();

				var (id, secret) = value;

				key.SetValue(CLIENT_ID_STR, id);
				key.SetValue(CLIENT_SECRET_STR, secret);

				key.Close();
			}
		}

		internal static string SauceNaoAuth => "c1f946bb2003c92fa8a25ce7fa923e0f213a0db8";

		private static RegistryKey GetKey()
		{
			return Registry.CurrentUser.CreateSubKey(SUBKEY);
		}
		
		internal static void AddToContextMenu()
		{
			// see bat file
			//Computer\HKEY_CLASSES_ROOT\*\shell\SmartImage\command
		}
	}
}