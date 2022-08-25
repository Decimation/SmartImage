using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Flurl.Http;
using Flurl.Http.Content;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

// ReSharper disable PossibleNullReferenceException

namespace SmartImage.Lib.Engines.Upload;

public sealed class AnonFilesEngine : BaseUploadEngine
{
	public AnonFilesEngine() : base("https://api.anonfiles.com/upload") { }

	public override async Task<Uri> UploadFileAsync(string file)
	{
		using var task = await EndpointUrl.PostMultipartAsync(mp =>
		{
			mp.AddFile("file", file);
		});

		var data = await task.GetJsonAsync<AnonFilesUpload>();

		return new Uri(data.Data.File.Url.Full);

		// var json = JObject.Parse(data);
		// var token = json["data"]["file"]["url"]["full"];
		// return new(token.ToString());
	}

	public override int MaxSize => 20 * 1000 * 1000;

	public override string Name => "Anonfiles";

	#region Serialization

	private sealed record AnonFilesUrl
	{
		[JsonProperty("full")]
		public string Full { get; set; }

		[JsonProperty("short")]
		public string Short { get; set; }
	}

	private sealed record AnonFilesSize
	{
		[JsonProperty("bytes")]
		public int Bytes { get; set; }

		[JsonProperty("readable")]
		public string Readable { get; set; }
	}

	private sealed record AnonFilesMetadata
	{
		[JsonProperty("id")]
		public string Id { get; set; }

		[JsonProperty("name")]
		public string Name { get; set; }

		[JsonProperty("size")]
		public AnonFilesSize Size { get; set; }
	}

	private sealed record AnonFilesFile
	{
		[JsonProperty("url")]
		public AnonFilesUrl Url { get; set; }

		[JsonProperty("metadata")]
		public AnonFilesMetadata Metadata { get; set; }
	}

	private sealed record AnonFilesData
	{
		[JsonProperty("file")]
		public AnonFilesFile File { get; set; }
	}

	private sealed record AnonFilesUpload
	{
		[JsonProperty("status")]
		public bool Status { get; set; }

		[JsonProperty("data")]
		public AnonFilesData Data { get; set; }
	}

	#endregion
}