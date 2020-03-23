using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using RestSharp;

namespace SmartImage.Utilities
{
	internal static class Common
	{
		internal static string GetString(string url)
		{
			using var wc = new WebClient();
			return wc.DownloadString(url);
		}

		internal static void AssertResponse(IRestResponse response)
		{
			// todo

			if (!response.IsSuccessful) {
				var sb = new StringBuilder();
				sb.AppendFormat("Uri: {0}\n", response.ResponseUri.ToString());
				sb.AppendFormat("Code: {0}\n", response.StatusCode);

				Console.WriteLine("\n\n{0}", sb);
			}
		}

		internal static void OpenUrl(string url)
		{
			// https://stackoverflow.com/questions/4580263/how-to-open-in-default-browser-in-c-sharp

			try {
				if (url != null) {
					Process.Start(url);
				}
				else {
					Console.WriteLine();
					Cli.WriteError("URL is null!");
				}
			}
			catch {
				// hack because of this: https://github.com/dotnet/corefx/issues/10361
				if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
					url = url.Replace("&", "^&");
					Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") {CreateNoWindow = true});
				}
				else {
					throw;
				}
			}
		}

		internal static string Truncate(this string value, int maxLength)
		{
			if (String.IsNullOrEmpty(value)) return value;
			return value.Length <= maxLength ? value : value.Substring(0, maxLength);
		}

		/// <summary>Convert a word that is formatted in pascal case to have splits (by space) at each upper case letter.</summary>
		internal static string SplitPascalCase(string convert)
		{
			return Regex.Replace(Regex.Replace(convert, @"(\P{Ll})(\P{Ll}\p{Ll})", "$1 $2"),
			                     @"(\p{Ll})(\P{Ll})", "$1 $2");
		}

		internal static string CreateBatchFile(string name, string[] code)
		{
			var file = Path.Combine(Directory.GetCurrentDirectory(), name);

			File.WriteAllLines(file, code);

			return file;
		}

		internal static void RunBatchFile(string file)
		{
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

			Cli.WriteInfo("Waiting for batch file to exit");

			process.WaitForExit();

			File.Delete(file);
		}

		internal static string GetExecutableLocation(string exe)
		{
			string dir = Environment.GetEnvironmentVariable("PATH")
			                       ?.Split(';')
			                        .FirstOrDefault(s => File.Exists(Path.Combine(s, exe)));

			if (dir != null) {
				return Path.Combine(dir, exe);
			}

			return null;
		}
	}
}