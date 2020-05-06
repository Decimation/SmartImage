namespace SmartImage.Model
{
	public interface IIntegrated
	{
		string AddOrRem { get; set; }

		public bool Add => AddOrRem == "add";
		
		//void Add();
		//void Remove();
	}
}