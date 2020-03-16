using System.Runtime.Serialization;

namespace SmartImage.Indexers.SauceNao
{
	[DataContract]
	public class SauceNaoResponse
	{
		//ignore
		//[DeserializeAs(Name = "header")]
		//public object Header { get; set; }

		[DataMember(Name = "results")]
		public SauceNaoResult[] Results { get; set; }

		public override string ToString()
		{
			return string.Format("Results: {0}", Results.Length);
		}
	}
}