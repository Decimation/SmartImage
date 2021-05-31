using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualBasic;
using RestSharp;
using SimpleCore.Numeric;
using SmartImage.Lib.Utilities;
using FileSystem = Novus.Win32.FileSystem;
// ReSharper disable UnusedMember.Global

namespace SmartImage.Lib.Engines.Impl
{
	public sealed class CatBoxEngine : IUploadEngine
	{
		public string Name => "CatBox";

		public int MaxSize => 200;

		private readonly RestClient m_client;

		public CatBoxEngine()
		{
			m_client = new RestClient("https://catbox.moe/user/api.php");
		}

		public Uri Upload(string file)
		{
			IUploadEngine.Verify(this, file);


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