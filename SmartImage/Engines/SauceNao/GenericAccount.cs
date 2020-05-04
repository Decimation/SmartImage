using System.Text;

namespace SmartImage.Engines.SauceNao
{
	public readonly struct GenericAccount
	{
		public string Username { get; }
		public string Password { get; }
		public string Email { get; }
		public string ApiKey { get; }

		public GenericAccount(string username, string password, string email, string apiKey)
		{
			Username = username;
			Password = password;
			Email = email;
			ApiKey = apiKey;
		}

		public override string ToString()
		{
			var sb = new StringBuilder();
			sb.AppendFormat("Username: {0}\n", Username);
			sb.AppendFormat("Password: {0}\n", Password);
			sb.AppendFormat("Email: {0}\n", Email);
			sb.AppendFormat("Api key: {0}\n", ApiKey);
			return sb.ToString();
		}
	}
}