namespace SmartImage.Lib;

public interface ILoginEngine
{
	public Task LoginAsync(string username, string password);
}