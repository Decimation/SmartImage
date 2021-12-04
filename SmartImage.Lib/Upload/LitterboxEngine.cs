using System;
using System.Net.Http;
using System.Threading.Tasks;
using Flurl.Http;
using Kantan.Threading;
using RestSharp;
using SmartImage.Lib.Utilities;

// ReSharper disable StringLiteralTypo

// ReSharper disable UnusedMember.Global

namespace SmartImage.Lib.Upload;

public sealed class LitterboxEngine : BaseUploadEngine
{
	public override string Name => "Litterbox";

	public override int MaxSize => 1000;

	private readonly string m_client;

	public LitterboxEngine()
	{
		m_client = ("https://litterbox.catbox.moe/resources/internals/api.php");
	}

	public override async Task<Uri> Upload(string file)
	{
		Verify(file);


		string task=null;

		var vv = await m_client
			         .PostMultipartAsync(mp =>
				                             mp.AddFile("fileToUpload", file)
				                               .AddString("reqtype", "fileupload")
				                               .AddString("time", "1h")
			         );
		var content = vv.ResponseMessage.Content;
		task = await content.ReadAsStringAsync();
		
		


		if (!vv.ResponseMessage.IsSuccessStatusCode) {
			return null;
		}

		return new Uri(task);
	}
}