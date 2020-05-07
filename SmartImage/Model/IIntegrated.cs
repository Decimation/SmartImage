namespace SmartImage.Model
{
	public interface IIntegrated
	{
		string Option { get; set; }
		
		bool Add => Option == "add";
		bool Remove => Option == "remove";

		//void Add();
		//void Remove();
	}
}