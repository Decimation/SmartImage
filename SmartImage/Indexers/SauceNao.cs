using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Xml;
using RestSharp;
using RestSharp.Deserializers;
using RestSharp.Serialization.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using System.Json;
using System.Text;
using System.Runtime.Serialization.Json;
using System.Text.RegularExpressions;
using System.Xml;
using JsonObject = System.Json.JsonObject;

namespace SmartImage.Indexers
{
	public class SauceNao : Indexer
	{
		public SauceNao(string endpoint) : base(endpoint)
		{
			
		}

		/// <summary>Convert a word that is formatted in pascal case to have splits (by space) at each upper case letter.</summary>
		private static string SplitPascalCase(string convert)
		{
			return Regex.Replace(Regex.Replace(convert, @"(\P{Ll})(\P{Ll}\p{Ll})", "$1 $2"), @"(\p{Ll})(\P{Ll})", "$1 $2");
		}
		public override Result[] GetResults(string url, string apiKey)
		{
			var req = new RestRequest();
			req.AddQueryParameter("db", "999");
			req.AddQueryParameter("output_type", "2");
			req.AddQueryParameter("numres", "16");
			req.AddQueryParameter("api_key", apiKey);
			req.AddQueryParameter("url", url);

			req.JsonSerializer = new JsonDeserializer();
			var res = Client.Execute(req);


			Console.WriteLine("{0} {1} {2}", res.IsSuccessful, res.ResponseStatus, res.StatusCode);

			Console.WriteLine(res.Content);


			
			JsonValue jsonString = JsonValue.Parse(res.Content);
			if (jsonString is JsonObject jsonObject) {
				JsonValue jsonArray = jsonObject["results"];
				for (int i = 0; i < jsonArray.Count; i++) {
					JsonValue header = jsonArray[i]["header"];
					JsonValue data   = jsonArray[i]["data"];
					string    obj    = header.ToString();
					obj          =  obj.Remove(obj.Length - 1);
					obj          += data.ToString().Remove(0, 1).Insert(0, ",");
					jsonArray[i] =  JsonValue.Parse(obj);
				}

				string json = jsonArray.ToString();
				json = json.Insert(json.Length - 1, "}").Insert(0, "{\"results\":");
				using (var stream =
					JsonReaderWriterFactory.CreateJsonReader(Encoding.UTF8.GetBytes(json),
					                                         XmlDictionaryReaderQuotas.Max)) {
					var              serializer = new DataContractJsonSerializer(typeof(Response));
					Response result     = serializer.ReadObject(stream) as Response;
					stream.Dispose();
					if (result is null)
						return null;

					for (int i = 0; i < result.Results.Length; i++) {
						result.Results[i].WebsiteTitle = SplitPascalCase(result.Results[i].Index.ToString());
						
					}

					return result.Results;
				}
			}

			return null;
		}
	}
}