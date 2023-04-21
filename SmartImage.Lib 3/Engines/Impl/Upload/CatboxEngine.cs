using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Flurl.Http;
using SmartImage.Lib.Engines.Impl.Upload;

public sealed class CatboxEngine : BaseUploadEngine
{
	public override string Name => "Catbox";

	public override int MaxSize => 1 * 1000 * 1000 * 200;

	public CatboxEngine() : base("https://catbox.moe/user/api.php") { }

	public override async Task<Url> UploadFileAsync(string file)
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

		/*if (!responseMessage.IsSuccessStatusCode) {

			return null;
		}*/

		return new(content);
	}
}