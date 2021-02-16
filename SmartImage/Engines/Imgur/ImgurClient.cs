using RestSharp;
using RestSharp.Serialization.Json;
using SmartImage.Configuration;
using System;
using System.IO;

// ReSharper disable UnusedMember.Local

namespace SmartImage.Engines.Imgur
{
	// https://github.com/Auo/ImgurSharp

	public sealed class ImgurClient : IUploadEngine
	{
		private readonly string m_apiKey;

		private const string BaseUrl = "https://api.imgur.com/3/";

		private ImgurClient(string apiKey)
		{
			m_apiKey = apiKey;
		}

		public ImgurClient() : this(SearchConfig.Config.ImgurAuth) { }

		public string Upload(string path)
		{

			var client = new RestClient(BaseUrl)
			{
				Timeout = -1
			};

			var request = new RestRequest("image", Method.POST);
			request.AddHeader("Authorization", "Client-ID " + m_apiKey);
			request.AlwaysMultipartFormData = true;
			request.AddParameter("image", Convert.ToBase64String(File.ReadAllBytes(path)));

			var response = client.Execute(request);

			var des = new JsonDeserializer();
			return des.Deserialize<ImgurDataResponse<ImgurImage>>(response).Data.Link;
		}
	}
}