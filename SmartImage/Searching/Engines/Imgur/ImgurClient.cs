#region

using System;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Text;
using RestSharp;
using RestSharp.Serialization.Json;

#endregion

namespace SmartImage.Searching.Engines.Imgur
{
	// https://github.com/Auo/ImgurSharp

	public sealed class ImgurClient
	{
		private readonly string m_apiKey;

		private ImgurClient(string apiKey)
		{
			m_apiKey = apiKey;
		}

		public ImgurClient() : this(SearchConfig.Config.ImgurAuth) { }

		public string Upload(string path)
		{
			// todo: cleanup
			// var rc = new RestClient("https://api.imgur.com/3/upload");
			// var re = new RestRequest(Method.POST);
			// re.AddHeader("Authorization","Client-ID "+ m_apiKey);
			//
			// re.AddParameter("image", Convert.ToBase64String(File.ReadAllBytes(path)), ParameterType.RequestBody);
			//
			// var res = rc.Execute(re);
			//
			// Console.WriteLine(res.ErrorMessage);
			// Console.WriteLine(res.StatusCode);
			// Console.WriteLine(res.IsSuccessful);

			using var w = new WebClient();
			w.Headers.Add("Authorization: Client-ID " + m_apiKey);

			var values = new NameValueCollection
			{
				{"image", Convert.ToBase64String(File.ReadAllBytes(path))}
			};

			string response =
				Encoding.UTF8.GetString(w.UploadValues("https://api.imgur.com/3/upload", values));
			//Console.WriteLine(response);


			var res = new RestResponse {Content = response};

			 var des = new JsonDeserializer();
			 return des.Deserialize<ImgurResponse<ImgurImage>>(res).Data.Link;

		}
	}
}