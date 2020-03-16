namespace SmartImage.Model
{
	public interface IIndexer
	{
		public SimpleResult[] GetResults(string url);
	}
}