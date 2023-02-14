namespace SmartImage.Lib.Engines;

/*
 * TODO: due to design and control flow complications this interface had to be designed this way...
 */

public interface ILoginEngine
{
    public string Username { get; set; }
    public string Password { get; set; }

    public Task<bool> LoginAsync();

    public bool     IsLoggedIn { get; }

}