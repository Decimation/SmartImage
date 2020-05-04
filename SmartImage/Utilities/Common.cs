#region

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using Neocmd;
using OpenQA.Selenium;
using RestSharp;

#endregion

namespace SmartImage.Utilities
{
	internal static class Common
	{
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

		internal static void AssertResponse(IRestResponse response)
		{
			// todo

			if (!response.IsSuccessful) {
				var sb = new StringBuilder();
				sb.AppendFormat("Uri: {0}\n", response.ResponseUri);
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
					CliOutput.WriteError("URL is null!");
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

		internal static string GetString(string url)
		{
			using var wc = new WebClient();
			return wc.DownloadString(url);
		}

		public static void WriteMap(IDictionary<string, string> d, string filename)
		{
			string[] lines = d.Select(kvp => kvp.Key + "=" + kvp.Value).ToArray();
			File.WriteAllLines(filename, lines);
		}

		public static IDictionary<string, string> ReadMap(string filename)
		{
			string[] lines = File.ReadAllLines(filename);
			var      dict  = lines.Select(l => l.Split('=')).ToDictionary(a => a[0], a => a[1]);

			return dict;
		}
		
		
	}
}