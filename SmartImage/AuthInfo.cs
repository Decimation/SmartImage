namespace SmartImage
{
	public readonly struct AuthInfo
	{
		public string Id { get; }

		// todo
		public string Secret { get; }

		public bool IsNull { get; }
		
		public static readonly AuthInfo Null = new AuthInfo(string.Empty, string.Empty);

		public AuthInfo(string id, string secret) : this()
		{
			Id     = id;
			Secret = secret;
			IsNull = string.IsNullOrWhiteSpace(id);
		}
	}
}