using System;
using System.Collections.Generic;
using System.Json;
using System.Linq;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Policy;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Flurl.Http;
using Novus.Streams;
using SmartImage.Lib.Engines;

namespace SmartImage.Lib;

public class HydrusClient : IHttpClient
{
	public FlurlClient Client { get; }

	public HydrusClient(string endpoint, string key)
	{
		EndpointUrl = endpoint;
		Key         = key;

		Client = new FlurlClient(EndpointUrl)
		{
			Headers =
			{
				{ "Hydrus-Client-API-Access-Key", key }
			}
		};
	}

	public async Task<JsonValue> GetFileHashesAsync(string hash)
	{
		const string s = "sha256";

		using var res = await Client.Request("/get_files/file_hashes")
			                .SetQueryParam("hash", hash)
			                .SetQueryParam("source_hash_type", s)
			                .SetQueryParam("desired_hash_type", s)
			                .GetAsync();

		var b = await res.GetStreamAsync();
		var j = JsonValue.Load(b);
		return j;

	}

	public async Task<JsonValue> GetFileMetadataAsync(string hash)
	{

		using var res = await Client.Request("/get_files/file_metadata")
			                .SetQueryParam("hash", hash)
			                .GetAsync();

		var b = await res.GetStreamAsync();
		var j = JsonValue.Load(b);
		return j;
	}

	public async Task<JsonValue> GetFileRelationshipsAsync(string hash)
	{
		using var res = await Client.Request("/manage_file_relationships/get_file_relationships")
			                .SetQueryParam("hash", hash)
			                .GetAsync();

		var b = await res.GetStreamAsync();
		var j = JsonValue.Load(b);
		b.TrySeek();

		return j;
	}

	public string Key { get; }

	public string EndpointUrl { get; }

	public void Dispose()
	{
		Client.Dispose();
	}
}

