using System;
using System.Diagnostics;
using System.Drawing;
using Novus.Win32;
using RestSharp;
using SimpleCore.Numeric;
using SimpleCore.Utilities;

#nullable enable
namespace SmartImage.Lib
{
	public sealed class ImgOpsEngine : SearchEngine
	{
		public ImgOpsEngine() : base("http://imgops.com/") { }
		
		public override SearchEngineOptions Engine => SearchEngineOptions.ImgOps;


		public static string QuickUpload(string path)
		{
			//todo
			return new ImgOpsEngine().Upload(path);
		}

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

		private const double MAX_FILE_SIZE_MB = 5;

		public string? Upload(string img)
		{
			if (string.IsNullOrWhiteSpace(img)) {
				throw new ArgumentNullException(nameof(img));
			}

			Debug.WriteLine($"Uploading {img}");


			double fileSizeMegabytes =
				MathHelper.ConvertToUnit(FileSystem.GetFileSize(img), MetricUnit.Mega);
			
			if (fileSizeMegabytes >= MAX_FILE_SIZE_MB) {
				throw new ArgumentException($"File {img} is too large (max {MAX_FILE_SIZE_MB} MB)");
			}

			string imgOpsUrl = UploadImage(img);

			string? link = imgOpsUrl;
			link = "http://" + link.SubstringAfter(BaseUrl);

			return link;
		}
	}
}