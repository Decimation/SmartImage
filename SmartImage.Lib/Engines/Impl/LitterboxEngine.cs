using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RestSharp;
using SmartImage.Lib.Utilities;

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
			if (String.IsNullOrWhiteSpace(file)) {
				throw new ArgumentNullException(nameof(file));
			}

			if (!((IUploadEngine) this).FileSizeValid(file)) {
				throw new ArgumentException($"File {file} is too large (max {MaxSize} MB) for {Name}"); //todo
			}


			var req = new RestRequest(Method.POST);

			req.AddParameter("time", "1h");
			req.AddParameter("reqtype", "fileupload");
			req.AddFile("fileToUpload", file);
			req.AddHeader("Content-Type", "multipart/form-data");

			var res = m_client.Execute(req);

			if (!res.IsSuccessful) {
				throw new SmartImageException(); //todo
			}

			return new Uri(res.Content);
		}
	}
}