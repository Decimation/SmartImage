using System;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using RestSharp;
using RestSharp.Deserializers;
using RestSharp.Serialization.Json;

namespace SmartImage
{
	public class Imgur
	{
		// https://github.com/Auo/ImgurSharp
		public class ResponseRootObject<T>
		{
			public T Data { get; set; }

			public bool Success { get; set; }

			public int Status { get; set; }
		}

		public class Image
		{
			public string Id { get; set; }

			public string Title { get; set; }

			public string Description { get; set; }

			public int Datetime { get; set; }


			public string Type { get; set; }

			public bool Animated { get; set; }

			public int Width { get; set; }

			public int Height { get; set; }

			public int Size { get; set; }

			public long Views { get; set; }

			public long Bandwidth { get; set; }

			public string Deletehash { get; set; }

			public object Section { get; set; }

			public string Link { get; set; }
		}

		public static string Upload(string path, string apiKey)
		{
			using (var w = new WebClient()) {
				w.Headers.Add("Authorization: Client-ID " + apiKey);
				var values = new NameValueCollection
				{
					{"image", Convert.ToBase64String(File.ReadAllBytes(@path))}
				};

				string response =
					System.Text.Encoding.UTF8.GetString(w.UploadValues("https://api.imgur.com/3/upload", values));
				//Console.WriteLine(response);

				var res = new RestResponse();
				res.Content = response;

				//dynamic dynObj = JsonConvert.DeserializeObject(response);
				//return dynObj.data.link;

				var des = new JsonDeserializer();
				return des.Deserialize<ResponseRootObject<Image>>(res).Data.Link;
			}
		}
	}
}