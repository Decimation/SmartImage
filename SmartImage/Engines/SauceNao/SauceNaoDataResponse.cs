using System;
using System.Runtime.Serialization;

namespace SmartImage.Engines.SauceNao
{
	[DataContract]
	public class SauceNaoDataResponse
	{
		//ignore
		//[DeserializeAs(Name = "header")]
		//public object Header { get; set; }

		[DataMember(Name = "results")]
		public SauceNaoDataResult[] Results { get; set; }

		public override string ToString()
		{
			return String.Format("Results: {0}", Results.Length);
		}
	}
}