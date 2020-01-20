using System.Collections.Generic;
using RestSharp.Deserializers;

namespace SmartImage.Indexers
{
	public class Response
	{
		//ignore
		//[DeserializeAs(Name = "header")]
		//public object Header { get; set; }

		[DeserializeAs(Name = "results")]
		public Result[] Results { get; set; }

		public override string ToString()
		{
			return string.Format("Results: {0}", Results.Length);
		}
	}
}