using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Json;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Policy;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Threading.Tasks;
using Flurl.Http;
using Novus.Streams;
using JsonObject = System.Json.JsonObject;
using JsonValue = System.Json.JsonValue;

namespace SmartImage.Lib.Clients;

public class HydrusClient : INotifyPropertyChanged, IDisposable
{

	private const string HDR_HYDRUS_KEY = "Hydrus-Client-API-Access-Key";

	public FlurlClient Client { get; }

	public HydrusClient(string endpoint, string key)
	{
		EndpointUrl = endpoint;
		Key         = key;

		Client = new FlurlClient(EndpointUrl)
		{
			Headers =
			{
				{ HDR_HYDRUS_KEY, key }
			}
		};

		PropertyChanged += (sender, args) =>
		{
			switch (args.PropertyName) {
				case nameof(Key):
					Client.Headers.AddOrReplace(HDR_HYDRUS_KEY, Key);
					break;

				case nameof(EndpointUrl):
					Client.BaseUrl = EndpointUrl;
					break;
			}

		};
	}

	public HydrusClient() : this(null, null) { }

	public bool IsValid => EndpointUrl != null && Key != null;

	public async Task<JsonValue> GetFileHashesAsync(string hash, string hashType = "sha256")
	{

		using var res = await Client.Request("/get_files/file_hashes")
			                .SetQueryParam("hash", hash)
			                .SetQueryParam("source_hash_type", hashType)
			                .SetQueryParam("desired_hash_type", hashType)
			                .GetAsync();

		var b = await res.GetStreamAsync();
		var j = JsonValue.Load(b);
		return j;

	}

	public async Task<JsonValue> GetFileMetadataAsync(HydrusQuery q)
	{
		var (name, value) = q.GetValue();

		using var res = await Client.Request("/get_files/file_metadata")
			                .SetQueryParam(name, value)
			                .GetAsync();

		var b = await res.GetStreamAsync();
		var j = JsonValue.Load(b);
		return j;
	}

	public async Task<JsonValue> GetFileRelationshipsAsync(HydrusQuery q)
	{
		var (name, value) = q.GetValue();

		using var res = await Client.Request("/manage_file_relationships/get_file_relationships")
			                .SetQueryParam(name, value)
			                .GetAsync();

		var b = await res.GetStreamAsync();
		var j = JsonValue.Load(b);

		return j;
	}

	public async Task<IFlurlResponse> GetFileAsync(HydrusQuery q)
	{
		var (name, value) = q.GetValue();

		var res = await Client.Request("/get_files/file")
			          .SetQueryParam(name, value)
			          .GetAsync();

		return res;
	}

	public async Task<IFlurlResponse> GetFileThumbnailAsync(HydrusQuery q)
	{
		var (name, value) = q.GetValue();

		var res = await Client.Request("/get_files/thumbnail")
			          .SetQueryParam(name, value)
			          .GetAsync();

		return res;
	}

	public async Task<IFlurlResponse> GetUrlInfoAsync(string url)
	{
		var res = await Client.Request("/add_urls/get_url_info")
			          .SetQueryParam("url", url)
			          .GetAsync();

		return res;
	}

	public async Task<IFlurlResponse> GetUrlFilesAsync(string url)
	{
		var res = await Client.Request("/add_urls/get_url_files")
			          .SetQueryParam("url", url)
			          .GetAsync();

		return res;
	}

	private string m_key;

	public string Key
	{
		get => m_key;
		set
		{
			if (value == m_key) return;

			m_key = value;
			OnPropertyChanged();
			OnPropertyChanged(nameof(IsValid));
		}
	}

	private string m_endpointUrl;

	public string EndpointUrl
	{
		get => m_endpointUrl;
		set
		{
			if (value == m_endpointUrl) return;

			m_endpointUrl = value;
			OnPropertyChanged();
			OnPropertyChanged(nameof(IsValid));
		}
	}

	public void Dispose()
	{
		GC.SuppressFinalize(this);
		Client.Dispose();
	}

	public event PropertyChangedEventHandler PropertyChanged;

	protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
	{
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}

	protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
	{
		if (EqualityComparer<T>.Default.Equals(field, value)) return false;

		field = value;
		OnPropertyChanged(propertyName);
		return true;
	}

	public static string HyEncode(object o)
	{
		return Url.Encode(JsonSerializer.Serialize(o));
	}

}

public sealed class HydrusQuery
{

/*

   file_id: (selective, a numerical file id)
   file_ids: (selective, a list of numerical file ids)
   hash: (selective, a hexadecimal SHA256 hash)
   hashes: (selective, a list of hexadecimal SHA256 hashes)
 *
 */

	public long? FileId { get; init; }

	public IEnumerable<long> FileIds { get; init; }

	public string Hash { get; init; }

	public IEnumerable<string> Hashes { get; init; }

	public HydrusQuery(long? fileId)
	{
		FileId = fileId;
	}

	public HydrusQuery(string h)
	{
		Hash = h;
	}

	public HydrusQuery(IEnumerable<string> hashes)
	{
		Hashes = hashes;
	}

	public HydrusQuery(IEnumerable<long> fileIds)
	{
		FileIds = fileIds;
	}

	public static implicit operator HydrusQuery(long l)
		=> new(l);

	public static implicit operator HydrusQuery(long[] l)
		=> new(l);

	public static implicit operator HydrusQuery(string s)
		=> new(s);

	public static implicit operator HydrusQuery(string[] s)
		=> new(s);

	public (string Name, string Value) GetValue()
	{
		if (FileId.HasValue) {
			return ("file_id", FileId.Value.ToString());
		}

		if (FileIds != null && FileIds.Any()) {
			return ("file_ids", HydrusClient.HyEncode(FileIds));
		}

		if (!String.IsNullOrWhiteSpace(Hash)) {
			return ("hash", Hash);
		}

		if (Hashes != null && Hashes.Any()) {
			return ("hashes", HydrusClient.HyEncode(Hashes));

			// return ("hashes", $"[{Url.Encode(String.Join(", ", Hashes), true)}]");
		}

		return (null, null);
	}

}

#pragma warning disable IL2026

public partial class HydrusFileRelationship
{

	[JsonPropertyName("0")]
	public string[] PotentialDuplicates { get; set; }

	[JsonPropertyName("1")]
	public string[] FalsePositives { get; set; }

	[JsonPropertyName("3")]
	public string[] Alternates { get; set; }

	[JsonPropertyName("8")]
	public string[] Duplicates { get; set; }

	[JsonPropertyName("is_king")]
	public bool IsKing { get; set; }

	[JsonPropertyName("king")]
	public string King { get; set; }

	[JsonPropertyName("king_is_local")]
	public bool KingIsLocal { get; set; }

	[JsonPropertyName("king_is_on_file_domain")]
	public bool KingIsOnFileDomain { get; set; }

	public static Dictionary<string, HydrusFileRelationship> Deserialize(JsonValue v)
	{
		var vs = ((JsonObject) v)["file_relationships"];

		var re = JsonSerializer.Deserialize<Dictionary<string, HydrusFileRelationship>>(vs.ToString());

		return re;
	}

}