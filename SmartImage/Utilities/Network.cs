#region

using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using JetBrains.Annotations;
using RestSharp;
using SimpleCore.CommandLine;
using SimpleCore.Utilities;

#nullable enable
#endregion
#pragma warning disable HAA0101, HAA0601, HAA0502
namespace SmartImage.Utilities
{
	internal static class Network
	{
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
			sb.AppendFormat("Response status: {0}\n", response.ResponseStatus);
			sb.AppendFormat("Response URI: {0}\n", response.ResponseUri);

			sb.AppendFormat("Content: {0}\n", response.Content);

			Console.Clear();

			Console.WriteLine(sb);
		}
		
		
		internal static string? IdentifyType(string url)
		{
			//var u =new Uri(url);

			var req = new RestRequest(url, Method.HEAD);
			RestClient client = new RestClient();

			var res = client.Execute(req);

			
			foreach (var h in res.Headers) {
				if (h.Name == "Content-Type") {
					var t = h.Value;

					return (string)t;
				}
			}


			return null;
		}


		internal static string DownloadUrl(string url)
		{
			string fileName = System.IO.Path.GetFileName(url);
			WebClient client = new WebClient();
			client.Headers.Add("User-Agent: Other");
			
			var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Desktop",fileName);

			client.DownloadFile(url, dir);

			return dir;
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
					NConsole.WriteError("URL is null!");
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

		private static readonly RestClient Client = new RestClient();

		internal static IRestResponse GetSimpleResponse(string link)
		{

			var restReq = new RestRequest(link);
			var restRes = Client.Execute(restReq);

			return restRes;
		}

		internal static string GetString(string url)
		{
			using var wc = new WebClient();
			return wc.DownloadString(url);
		}
	}
}