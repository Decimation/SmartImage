using System;
using System.Net.Http;
using System.Threading.Tasks;
using Flurl.Http;
using Kantan.Threading;
using SmartImage.Lib.Utilities;

// ReSharper disable StringLiteralTypo

// ReSharper disable UnusedMember.Global

namespace SmartImage.Lib.Upload;

public sealed class LitterboxEngine : BaseUploadEngine
{
	public override string Name => "Litterbox";

	public override int MaxSize => 1000;

	public LitterboxEngine() : base("https://litterbox.catbox.moe/resources/internals/api.php") { }

	public override async Task<Uri> UploadFileAsync(string file)
	{
		Verify(file);

		using var response = await EndpointUrl
			                     .PostMultipartAsync(mp =>
				                                         mp.AddFile("fileToUpload", file)
				                                           .AddString("reqtype", "fileupload")
				                                           .AddString("time", "1h")
			                     );

		var responseMessage = response.ResponseMessage;

		var content = await responseMessage.Content.ReadAsStringAsync();

		if (!responseMessage.IsSuccessStatusCode) {
			return null;
		}

		return new Uri(content);
	}
}