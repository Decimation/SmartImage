using System;
using RestSharp;
using SmartImage.Lib.Utilities;

// ReSharper disable UnusedMember.Global

namespace SmartImage.Lib.Upload
{
	public sealed class CatBoxEngine : BaseUploadEngine
	{
		public override string Name => "CatBox";

		public override int MaxSize => 200;

		private readonly RestClient m_client;

		public CatBoxEngine()
		{
			m_client = new RestClient("https://catbox.moe/user/api.php");
		}

		public override Uri Upload(string file)
		{
			Verify(file);


			var req = new RestRequest(Method.POST);

			req.AddParameter("reqtype", "fileupload");
			req.AddFile("fileToUpload", file);
			req.AddHeader("Content-Type", "multipart/form-data");

			var res = m_client.Execute(req);

			if (!res.IsSuccessful) {
				throw new SmartImageException();
			}

			return new Uri(res.Content);
		}
	}
}