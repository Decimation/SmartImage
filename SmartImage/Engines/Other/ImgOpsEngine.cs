using System.Drawing;
using Novus.Utilities;
using Novus.Win32;
using Novus.Win32.FileSystem;
using RestSharp;
using SimpleCore.Console.CommandLine;
using SimpleCore.Utilities;

#nullable enable
namespace SmartImage.Engines.Other
{
	public sealed class ImgOpsEngine : BasicSearchEngine, IUploadEngine
	{
		public ImgOpsEngine() : base("http://imgops.com/") { }

		public override string Name  => "ImgOps";
		public override Color  Color => Color.DarkMagenta;

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
				MathHelper.ConvertToUnit(Files.GetFileSize(img), MetricUnit.Mega);

			if (fileSizeMegabytes >= MAX_FILE_SIZE_MB) {
				NConsole.WriteError("File size too large");
				return null;
			}

			string imgOpsUrl = UploadImage(img);

			// var imgOpsPageUrl = imgOpsUrl;

			// string html = Network.GetString(imgOpsUrl);
			//
			// const string HREF_REGEX = "href=\"(.*)\"";
			//
			// var    match = Regex.Matches(html, HREF_REGEX);
			/*string link = null;
			
			
			foreach (Match match1 in match) {
				foreach (Group @group in match1.Groups) {
					var v = group.Value;

					if (v.StartsWith("http://imgops.com/") && v.Contains("userUploadTempCache")) {
						link = v;
						break;
					}
				}
			}*/

			// May change in the future
			// const int HREF_N = 7;
			//
			// string link = match[HREF_N].Groups[1].Value;

			var link = imgOpsUrl;
			link = "http://" + link.SubstringAfter(BaseUrl);

			//Debug.WriteLine("> " + link);

			return link;
		}
	}
}