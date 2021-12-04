using System;
using System.Net.Http;
using System.Threading.Tasks;
using Flurl.Http;
using RestSharp;
using SmartImage.Lib.Utilities;

// ReSharper disable UnusedMember.Global

namespace SmartImage.Lib.Upload;

public sealed class CatBoxEngine : BaseUploadEngine
{
	public override string Name => "CatBox";

	public override int MaxSize => 200;

	private readonly string m_client;

	public CatBoxEngine()
	{
		m_client = ("https://catbox.moe/user/api.php");
	}

	public override async Task<Uri> Upload(string file)
	{
		Verify(file);

		var vv = await m_client
			         .PostMultipartAsync(mp =>
				                             mp.AddFile("fileToUpload", file)
				                               .AddUrlEncoded("reqtype", "fileupload")
			         );

		var c = vv.ResponseMessage.Content.ReadAsStringAsync().Result;

		if (!vv.ResponseMessage.IsSuccessStatusCode) {
			return null;
		}

		return new Uri(c);
	}
}