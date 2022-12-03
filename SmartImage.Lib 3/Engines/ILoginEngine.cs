namespace SmartImage.Lib.Engines;

public interface ILoginEngine
{
    public string Username { get; set; }
    public string Password { get; set; }

    public Task LoginAsync();
}