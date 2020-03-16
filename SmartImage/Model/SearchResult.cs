namespace SmartImage.Model
{
	public sealed class SearchResult
	{
		public string Url { get; }
		
		public string Name { get; }

		public float? Similarity { get; internal set; }

		public bool Success => Url != null;

		public SearchResult(string url, string name)
		{
			Url  = url;
			Name = name;
		}

		public override string ToString()
		{
			// redundant
			var cleanUrl = Success ? Url : null;
			return string.Format("{0}: {1}", Name, cleanUrl);
		}
	}
}