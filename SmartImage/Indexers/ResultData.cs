using System.Runtime.Serialization;
using RestSharp.Deserializers;

namespace SmartImage.Indexers
{
	public class ResultData
	{
		[DeserializeAs(Name = "ext_urls")]
		public string[] Url { get; set; }

		public string Title { get; set; }

	}
}