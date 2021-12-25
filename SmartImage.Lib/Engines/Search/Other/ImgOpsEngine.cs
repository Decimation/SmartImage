using SmartImage.Lib.Engines.Search.Base;
using SmartImage.Lib.Searching;

#nullable enable
namespace SmartImage.Lib.Engines.Search.Other;

public sealed class ImgOpsEngine : BaseSearchEngine
{
	public override SearchEngineOptions EngineOption => SearchEngineOptions.ImgOps;

	//public int MaxSize => 5;
	public override EngineSearchType SearchType => EngineSearchType.Other;

	public ImgOpsEngine() : base("http://imgops.com/") { }

	/*public Uri Upload(string img)
	{
		IUploadEngine.Verify(this, img);

		Debug.WriteLine($"Uploading {img}");


		var imgOpsUrl = UploadInternal(img);

		string? link = imgOpsUrl.ToString();
		link = "http://" + link.SubstringAfter(BaseUrl);

		return new Uri(link);
	}

	private Uri UploadInternal(string path)
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


		return re.ResponseUri;
	}*/
}