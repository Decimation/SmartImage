using System;
using RestSharp;
using SmartImage.Lib.Utilities;

// ReSharper disable StringLiteralTypo

// ReSharper disable UnusedMember.Global

namespace SmartImage.Lib.Upload
{
	public sealed class LitterboxEngine : BaseUploadEngine
	{
		public override string Name => "Litterbox";

		public override int MaxSize => 1000;

		private readonly RestClient m_client;

		public LitterboxEngine()
		{
			m_client = new RestClient("https://litterbox.catbox.moe/resources/internals/api.php");
		}

		public override Uri Upload(string file)
		{
			Verify(file);


			var req = new RestRequest(Method.POST);

			req.AddParameter("time", "1h");
			req.AddParameter("reqtype", "fileupload");
			req.AddFile("fileToUpload", file);
			req.AddHeader("Content-Type", "multipart/form-data");

			var res = m_client.Execute(req);

			if (!res.IsSuccessful) {
				throw new SmartImageException($"{res.ErrorMessage} {res.StatusCode} {res.ResponseStatus}");
			}

			return new Uri(res.Content);
		}
	}
}