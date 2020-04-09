using System;
using System.Collections.Generic;
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

		internal static void WriteResponse(IRestResponse response)
		{
			// todo

			var sb = new StringBuilder();
			sb.AppendFormat("Success: {0}\n", response.IsSuccessful);
			sb.AppendFormat("Status code: {0}\n", response.StatusCode);
			sb.AppendFormat("Error Message: {0}\n", response.ErrorMessage);
			sb.AppendFormat("Content: {0}\n", response.Content);
			sb.AppendFormat("Response status: {0}\n", response.ResponseStatus);
			sb.AppendFormat("Response URI: {0}\n", response.ResponseUri);

			Console.Clear();

			Console.WriteLine(sb);
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

		/// <summary>
		/// Creates a <see cref="Process"/> to execute <paramref name="cmd"/>
		/// </summary>
		/// <param name="cmd">Command to run</param>
		/// <param name="autoStart">Whether to automatically start the <c>cmd.exe</c> process</param>
		/// <returns><c>cmd.exe</c> process</returns>
		public static Process Shell(string cmd, bool autoStart = false)
		{
			var startInfo = new ProcessStartInfo
			{
				FileName               = "cmd.exe",
				Arguments              = String.Format("/C {0}", cmd),
				RedirectStandardOutput = true,
				RedirectStandardError  = true,
				UseShellExecute        = false,
				CreateNoWindow         = true
			};

			var process = new Process
			{
				StartInfo           = startInfo,
				EnableRaisingEvents = true
			};

			if (autoStart)
				process.Start();

			return process;
		}
		
		public enum StandardStream
		{
			StdOut,
			StdError
		}
		public static string[] ShellOutput(string cmd,StandardStream ss = StandardStream.StdOut, bool waitForExit = true)
		{
			var proc = Shell(cmd, true);
			
			var stream = ss switch
			{
				StandardStream.StdOut => proc.StandardOutput,
				StandardStream.StdError => proc.StandardError,
				_ => throw new ArgumentOutOfRangeException(nameof(ss), ss, null)
			};

			var list = ReadAllLines(stream);

			if (waitForExit) {
				proc.WaitForExit();
			}

			return list;
		}
		internal static string[] ReadAllLines(StreamReader stream)
		{
			var list = new List<string>();

			while (!stream.EndOfStream) {
				string line = stream.ReadLine();

				if (line != null) {
					list.Add(line);
				}
			}

			return list.ToArray();
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


			if (!string.IsNullOrWhiteSpace(dir)) {
				return Path.Combine(dir, exe);
			}

			return null;
		}
	}
}