using RestSharp.Deserializers;

namespace SmartImage.Indexers
{
	public class ResultHeader
	{
		public float  Similarity { get; set; }
		public string Thumbnail  { get; set; }

		[DeserializeAs(Name = "index_id")]
		public int Index { get; set; }

		[DeserializeAs(Name = "index_name")]
		public string IndexName { get; set; }
	}
}