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
		// todo: move some functions into SimpleCore

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


		/// <summary>
		/// Identifies the MIME type of <paramref name="url"/>
		/// </summary>
		internal static string? IdentifyType(string url)
		{
			//var u =new Uri(url);

			var req = new RestRequest(url, Method.HEAD);
			RestClient client = new RestClient();

			var res = client.Execute(req);


			foreach (var h in res.Headers) {
				if (h.Name == "Content-Type") {
					var t = h.Value;

					return (string) t;
				}
			}


			return null;
		}

		internal static string DownloadUrl(string url)
		{
			string fileName = Path.GetFileName(url);
			using WebClient client = new WebClient();
			client.Headers.Add("User-Agent: Other");

			var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), 
				"Desktop", fileName);

			client.DownloadFile(url, dir);

			return dir;
		}

		internal static void OpenUrl(string url)
		{
			// https://stackoverflow.com/questions/4580263/how-to-open-in-default-browser-in-c-sharp

			try {
				Process.Start(url);
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

		/// <summary>
		/// Whether the MIME type <paramref name="type"/> is an image type.
		/// </summary>
		internal static bool IsImage(string? type)
		{
			var notImage = type == null || type.Split("/")[0] != "image";

			return !notImage;
		}
	}
}