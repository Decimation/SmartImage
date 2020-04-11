using System;
using System.Diagnostics;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using Neocmd;
using RestSharp;

namespace SmartImage.Utilities
{
	internal static class Http
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
	}
}