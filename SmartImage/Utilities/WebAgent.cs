#region

using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using Newtonsoft.Json.Linq;
using RestSharp;
using SimpleCore.Utilities;

#endregion

namespace SmartImage.Utilities
{
	internal static class WebAgent
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

		internal static class EMail
		{
			// https://github.com/Ezzpify/C-GuerrillaMail/blob/master/GuerrillaMail.cs


			/// <summary>
			/// Get the domain matching the given parameter.
			/// </summary>
			/// <param name="domain">Specifies which domain to return (0-8) useful for services that blocks certain domains</param>
			/// <returns></returns>
			private static string GetDomain(int domain)
			{
				return domain switch
				{
					1 => "grr.la",
					2 => "guerrillamail.biz",
					3 => "guerrillamail.com",
					4 => "guerrillamail.de",
					5 => "guerrillamail.net",
					6 => "guerrillamail.org",
					7 => "guerrillamailblock.com",
					8 => "spam4.me",
					_ => "sharklasers.com"
				};
			}

			/// <summary>
			/// This initializes the email and inbox on site
			/// </summary>
			internal static string GetEmail(string email = null)
			{
				/*Initialize the inbox*/
				JObject obj;
				if (email == null) {
					obj = JObject.Parse(Contact("f=get_email_address"));
				}
				else {
					obj = JObject.Parse(Contact("f=set_email_user",
					                            String.Format("email_user={0}&lang=en&site={1}", email, GetDomain(0))));
				}


				var emailAddressTk = ((string) obj.SelectToken("email_addr"));

				//var emailAddress = emailAddressTk.Split('@')[0];
				//var emailAlias   = (string) Obj.SelectToken("alias");


				return emailAddressTk;
			}

			/// <summary>
			/// Calls the page with arguments
			/// </summary>
			/// <param name="parameters">GET arguments</param>
			/// <param name="body">POST arguments</param>
			/// <returns>Returns json</returns>
			private static string Contact(string parameters, string body = null)
			{
				/*Set up the request*/
				var request =
					(HttpWebRequest) WebRequest.Create("http://api.guerrillamail.com/ajax.php?" + parameters);
				//request.CookieContainer = mCookies;
				request.Method = "GET";
				request.UserAgent =
					"Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/42.0.2311.135 Safari/537.36";
				request.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8";

				if (!String.IsNullOrEmpty(body)) {
					byte[] buffer = Encoding.UTF8.GetBytes(body);

					request.Method        = "POST";
					request.ContentType   = "application/x-www-form-urlencoded; charset=UTF-8";
					request.ContentLength = buffer.Length;

					using var steam = request.GetRequestStream();
					steam.Write(buffer, 0, buffer.Length);
				}


				/*Fetch the response*/
				using (var response = (HttpWebResponse) request.GetResponse()) {
					if (response.StatusCode == HttpStatusCode.OK) {
						using var stream = response.GetResponseStream();
						var       reader = new StreamReader(stream, Encoding.UTF8);
						return reader.ReadToEnd();
					}
				}

				/*Something messed up, returning empty string*/
				return String.Empty;
			}
		}
	}
}