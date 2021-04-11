using System;
using System.Diagnostics;
using Novus.Win32;
using RestSharp;
using SimpleCore.Numeric;
using SimpleCore.Utilities;

#nullable enable
namespace SmartImage.Lib.Engines.Impl
{
	public sealed class ImgOpsEngine : SearchEngine, IUploadEngine
	{
		public ImgOpsEngine() : base("http://imgops.com/") { }

		public override SearchEngineOptions Engine => SearchEngineOptions.ImgOps;


		private string UploadImage(string path)
		{
			//https://github.com/dogancelik/imgops

			var rc = new RestClient(BaseUrl)
			{
				FollowRedirects = true
			};

			var rq = new RestRequest("store", Method.POST);
			rq.AddHeader("Content-Type", "multipart/form-data");
			rq.AddFile("photo", path);

			var re = rc.Execute(rq);


			return re.ResponseUri.ToString();
		}


		public int MaxSize => 5;

		public Uri? Upload(string img)
		{
			if (string.IsNullOrWhiteSpace(img)) {
				throw new ArgumentNullException(nameof(img));
			}

			if (!((IUploadEngine) this).FileSizeValid(img)) {
				throw new ArgumentException($"File {img} is too large (max {MaxSize} MB) for {Name}"); //todo
			}

			Debug.WriteLine($"Uploading {img}");


			string imgOpsUrl = UploadImage(img);

			string? link = imgOpsUrl;
			link = "http://" + link.SubstringAfter(BaseUrl);

			return new Uri(link);
		}
	}
}