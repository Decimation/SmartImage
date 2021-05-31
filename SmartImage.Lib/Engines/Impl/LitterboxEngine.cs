using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RestSharp;
using SmartImage.Lib.Utilities;
// ReSharper disable StringLiteralTypo

// ReSharper disable UnusedMember.Global

namespace SmartImage.Lib.Engines.Impl
{
	public sealed class LitterboxEngine : IUploadEngine
	{
		public string Name => "Litterbox";

		public int MaxSize => 1000;

		private readonly RestClient m_client;

		public LitterboxEngine()
		{
			m_client = new RestClient("https://litterbox.catbox.moe/resources/internals/api.php");
		}

		public Uri Upload(string file)
		{
			IUploadEngine.Verify(this,file);


			var req = new RestRequest(Method.POST);

			req.AddParameter("time", "1h");
			req.AddParameter("reqtype", "fileupload");
			req.AddFile("fileToUpload", file);
			req.AddHeader("Content-Type", "multipart/form-data");

			var res = m_client.Execute(req);

			if (!res.IsSuccessful) {
				throw new SmartImageException($"{res.ErrorMessage} {res.StatusCode} {res.ResponseStatus}"); //todo
			}

			return new Uri(res.Content);
		}
	}
}