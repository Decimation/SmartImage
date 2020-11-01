using System.Drawing;
using System.Text.RegularExpressions;
using RestSharp;
using SimpleCore.Net;
using SmartImage.Searching.Model;

namespace SmartImage.Searching.Engines.Other
{
	public sealed class ImgOpsEngine : BasicSearchEngine
	{
		public ImgOpsEngine() : base("http://imgops.com/") { }

		public override string Name => "ImgOps";
		public override Color Color => Color.DarkMagenta;

		public override SearchEngineOptions Engine => SearchEngineOptions.ImgOps;

		public string UploadTempImage(string path, out string imgOpsPageUrl)
		{
			string imgOpsUrl = UploadImage(path);
			imgOpsPageUrl = imgOpsUrl;

			string html = Network.GetString(imgOpsUrl);

			const string HREF_REGEX = "href=\"(.*)\"";

			var match = Regex.Matches(html, HREF_REGEX);

			// May change in the future
			const int HREF_N = 7;

			string link = match[HREF_N].Groups[1].Value;

			return link;
		}

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
	}
}