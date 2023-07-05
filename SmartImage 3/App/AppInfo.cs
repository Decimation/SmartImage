using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Flurl.Http;
using JetBrains.Annotations;
using Kantan.Net.Utilities;
using Newtonsoft.Json;

// ReSharper disable InconsistentNaming
#pragma warning disable CS8618
#pragma warning disable IDE1006

namespace SmartImage.App;

internal static class AppInfo
{
	internal static void ExceptionLog(Exception ex)
	{
		File.WriteAllLines($"smartimage.log", new[]
		{
			$"Message: {ex.Message}",
			$"Source: {ex.Source}",
			$"Stack trace: {ex.StackTrace}",

		});
	}

	internal static async Task<Release[]> GetRepoReleasesAsync()
	{
		var res = await "https://api.github.com/repos/Decimation/SmartImage/releases"
			          .WithAutoRedirect(true)
			          .AllowAnyHttpStatus()
			          .WithHeaders(new
			          {
				          User_Agent = HttpUtilities.UserAgent
			          })
			          .GetJsonAsync<Release[]>();

		return res;
	}

	// Root myDeserializedClass = JsonConvert.DeserializeObject<List<Root>>(myJsonResponse);
	#region GitHub objects

	[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
	internal class ReleaseAsset
	{
		public string   url                  { get; set; }
		public int      id                   { get; set; }
		public string   node_id              { get; set; }
		public string   name                 { get; set; }
		public object   label                { get; set; }
		public Uploader uploader             { get; set; }
		public string   content_type         { get; set; }
		public string   state                { get; set; }
		public int      size                 { get; set; }
		public int      download_count       { get; set; }
		public DateTime created_at           { get; set; }
		public DateTime updated_at           { get; set; }
		public string   browser_download_url { get; set; }
	}

	[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
	internal class Author
	{
		public string login               { get; set; }
		public int    id                  { get; set; }
		public string node_id             { get; set; }
		public string avatar_url          { get; set; }
		public string gravatar_id         { get; set; }
		public string url                 { get; set; }
		public string html_url            { get; set; }
		public string followers_url       { get; set; }
		public string following_url       { get; set; }
		public string gists_url           { get; set; }
		public string starred_url         { get; set; }
		public string subscriptions_url   { get; set; }
		public string organizations_url   { get; set; }
		public string repos_url           { get; set; }
		public string events_url          { get; set; }
		public string received_events_url { get; set; }
		public string type                { get; set; }
		public bool   site_admin          { get; set; }
	}

	[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
	internal class Reactions
	{
		public string url         { get; set; }
		public int    total_count { get; set; }

		[JsonProperty("+1")]
		public int Plus1 { get; set; }

		[JsonProperty("-1")]
		public int Minus1 { get; set; }

		public int laugh    { get; set; }
		public int hooray   { get; set; }
		public int confused { get; set; }
		public int heart    { get; set; }
		public int rocket   { get; set; }
		public int eyes     { get; set; }
	}

	[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
	internal class Release
	{
		public string             url              { get; set; }
		public string             assets_url       { get; set; }
		public string             upload_url       { get; set; }
		public string             html_url         { get; set; }
		public int                id               { get; set; }
		public Author             author           { get; set; }
		public string             node_id          { get; set; }
		public string             tag_name         { get; set; }
		public string             target_commitish { get; set; }
		public string             name             { get; set; }
		public bool               draft            { get; set; }
		public bool               prerelease       { get; set; }
		public DateTime           created_at       { get; set; }
		public DateTime           published_at     { get; set; }
		public List<ReleaseAsset> assets           { get; set; }
		public string             tarball_url      { get; set; }
		public string             zipball_url      { get; set; }
		public string             body             { get; set; }
		public string             discussion_url   { get; set; }
		public Reactions          reactions        { get; set; }
	}

	[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
	internal class Uploader
	{
		public string login               { get; set; }
		public int    id                  { get; set; }
		public string node_id             { get; set; }
		public string avatar_url          { get; set; }
		public string gravatar_id         { get; set; }
		public string url                 { get; set; }
		public string html_url            { get; set; }
		public string followers_url       { get; set; }
		public string following_url       { get; set; }
		public string gists_url           { get; set; }
		public string starred_url         { get; set; }
		public string subscriptions_url   { get; set; }
		public string organizations_url   { get; set; }
		public string repos_url           { get; set; }
		public string events_url          { get; set; }
		public string received_events_url { get; set; }
		public string type                { get; set; }
		public bool   site_admin          { get; set; }
	}

	#endregion
}