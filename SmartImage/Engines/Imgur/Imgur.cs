#region

using System;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Text;
using RestSharp;
using RestSharp.Serialization.Json;

#endregion

namespace SmartImage.Engines.Imgur
{
	// https://github.com/Auo/ImgurSharp

	public sealed class Imgur
	{
		private readonly string m_apiKey;

		private Imgur(string apiKey)
		{
			m_apiKey = apiKey;
		}

		public Imgur() : this(Core.Config.ImgurAuth) { }

		public string Upload(string path)
		{
			// todo: cleanup

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