using System.Runtime.Serialization;
using RestSharp.Deserializers;
using SmartImage.Indexers;

namespace SmartImage
{
	public class Result
	{
		[DeserializeAs(Name = "header")]
		public ResultHeader Header { get; set;}
		
		[DeserializeAs(Name = "data")]
		public ResultData Data { get; set;}
		
		
		[IgnoreDataMember]
		public string WebsiteTitle { get; set; }

		public override string ToString()
		{
			return string.Format("Url: {0} | Similarity: {1}", Data.Url.Length, Header.Similarity);
		}
	}
}