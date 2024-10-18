
// Author: Deci | Project: SmartImage.Lib | Name: ChallengeDetector.cs
// Date: 2024/10/16 @ 15:10:44

using System.Net;

namespace SmartImage.Lib.Clients;

public static class ChallengeDetector
{
	private static readonly HashSet<string> CloudflareServerNames = new HashSet<string>{
		"cloudflare",
		"cloudflare-nginx",
		"ddos-guard"
	};

	/// <summary>
	/// Checks if clearance is required.
	/// </summary>
	/// <param name="response">The HttpResponseMessage to check.</param>
	/// <returns>True if the site requires clearance</returns>
	public static bool IsClearanceRequired(HttpResponseMessage response) => IsCloudflareProtected(response);

	/// <summary>
	/// Checks if the site is protected by Cloudflare
	/// </summary>
	/// <param name="response">The HttpResponseMessage to check.</param>
	/// <returns>True if the site is protected</returns>
	private static bool IsCloudflareProtected(HttpResponseMessage response)
	{
		// check response headers
		if (!response.Headers.Server.Any(i =>
			                                 i.Product != null && CloudflareServerNames.Contains(i.Product.Name.ToLower())))
			return false;

		// detect CloudFlare and DDoS-GUARD
		if (response.StatusCode.Equals(HttpStatusCode.ServiceUnavailable) ||
		    response.StatusCode.Equals(HttpStatusCode.Forbidden)) {
			var responseHtml = response.Content.ReadAsStringAsync().Result;
			if (responseHtml.Contains("<title>Just a moment...</title>")                 ||                 // Cloudflare
			    responseHtml.Contains("<title>Access denied</title>")                    ||                 // Cloudflare Blocked
			    responseHtml.Contains("<title>Attention Required! | Cloudflare</title>") ||                 // Cloudflare Blocked
			    responseHtml.Trim().Equals("error code: 1020")                           ||                 // Cloudflare Blocked
			    responseHtml.IndexOf("<title>DDOS-GUARD</title>", StringComparison.OrdinalIgnoreCase) > -1) // DDOS-GUARD
				return true;
		}

		// detect Custom CloudFlare for EbookParadijs, Film-Paleis, MuziekFabriek and Puur-Hollands
		if (response.Headers.Vary.ToString()                    == "Accept-Encoding,User-Agent" &&
		    response.Content.Headers.ContentEncoding.ToString() == ""                           &&
		    response.Content.ReadAsStringAsync().Result.ToLower().Contains("ddos"))
			return true; 

		return false;
	}

}