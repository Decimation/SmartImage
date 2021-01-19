using System.Drawing;
using Novus.Utilities;
using Novus.Win32;
using RestSharp;
using SimpleCore.Cli;
using SimpleCore.Numeric;
using SimpleCore.Utilities;

#nullable enable
namespace SmartImage.Engines.Other
{
	public sealed class ImgOpsEngine : BasicSearchEngine, IUploadEngine
	{
		public ImgOpsEngine() : base("http://imgops.com/") { }

		public override string Name  => "ImgOps";
		
		public override Color  Color => Color.Pink;

		public override SearchEngineOptions Engine => SearchEngineOptions.ImgOps;


		public string UploadImage(string path)
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
			double fileSizeMegabytes =
				MathHelper.ConvertToUnit(FileSystem.GetFileSize(img), MetricUnit.Mega);

			if (fileSizeMegabytes >= MAX_FILE_SIZE_MB) {
				NConsole.WriteError("File size too large");
				return null;
			}

			string imgOpsUrl = UploadImage(img);

			string? link = imgOpsUrl;
			link = "http://" + link.SubstringAfter(BaseUrl);

			return link;
		}
	}
}