using System;
using Microsoft.Win32;
using SmartImage.Indexers;

namespace SmartImage
{
	public static class Program
	{
		//6c97880bf8754c5
		//fe1bed3047828fed3ce67bf2ae923282f0a9a558

		private static (string, string) GetCred()
		{
			//accessing the CurrentUser root element  
			//and adding "OurSettings" subkey to the "SOFTWARE" subkey  
			RegistryKey key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\SmartImage");


			var cid = (string) key.GetValue("client_id");
			var cs  = (string) key.GetValue("client_secret");

			key.Close();

			return (cid, cs);
		}

		private static void SetCred(string cid, string cs)
		{
			//accessing the CurrentUser root element  
			//and adding "OurSettings" subkey to the "SOFTWARE" subkey  
			RegistryKey key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\SmartImage");


			key.SetValue("client_id", cid);
			key.SetValue("client_secret", cs);

			key.Close();
		}

		private static void AddToContextMenu()
		{
			
			
			// %SystemRoot%\System32\reg.exe ADD HKEY_CLASSES_ROOT\*\shell\SmartImage\command /ve /d "SmartImage.exe %1"
			
			
		}

		// @"C:\Users\Deci\Desktop\test.jpg";
		private static void Main(string[] args)
		{
			//var client_id     = "6c97880bf8754c5";
			//var client_secret = "fe1bed3047828fed3ce67bf2ae923282f0a9a558";

			if (args[0] == "--setup") {
				var cid = args[1];
				var cs  = args[2];

				Console.WriteLine("cid: {0}\ncs: {1}", cid, cs);
				SetCred(cid, cs);

				return;
			}

			if (args[0] == "--ctx-menu") {
				AddToContextMenu();
				
				return;
			}


			var (client_id, client_secret) = GetCred();

			if (client_id == null || client_secret == null) {
				Console.WriteLine("Credentials not yet set up.");
				return;
			}

			Console.WriteLine("client_id: {0}\nclient_secret: {1}", client_id, client_secret);

			var img = args[0];
			Console.WriteLine("source: {0}", img);


			var imgUrl = Imgur.Upload(img, client_id);

			var sn  = new SauceNao("https://saucenao.com/search.php");
			var res = sn.GetResults(imgUrl, "c1f946bb2003c92fa8a25ce7fa923e0f213a0db8");

			foreach (var re in res) {
				Console.WriteLine("{0}", re);
			}
		}
	}
}