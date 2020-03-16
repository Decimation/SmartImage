using System.Collections.Generic;
using System.Runtime.Serialization;
using RestSharp.Deserializers;

namespace SmartImage.Indexers
{
	[DataContract]
	public class Response
	{
		//ignore
		//[DeserializeAs(Name = "header")]
		//public object Header { get; set; }

		[DataMember(Name = "results")]
		public Result[] Results { get; set; }

		public override string ToString()
		{
			return string.Format("Results: {0}", Results.Length);
		}
	}
}