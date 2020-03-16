namespace SmartImage.Model
{
	public class SearchResult
	{
		public string Url { get; }
		
		public string Name { get; }

		public float? Similarity { get; internal set; }

		//public bool Success => Url != null;

		public SearchResult(string url, string name)
		{
			Url  = url;
			Name = name;
		}
	}
}