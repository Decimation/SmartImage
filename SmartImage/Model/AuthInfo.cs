#region

using System;

#endregion

namespace SmartImage.Model
{
	public readonly struct AuthInfo
	{
		// todo: this struct is useless
		
		public static readonly AuthInfo Null = new AuthInfo(String.Empty);

		public AuthInfo(string id) : this()
		{
			Id     = id;
			IsNull = String.IsNullOrWhiteSpace(id);
		}

		public string Id { get; }

		public bool IsNull { get; }
	}
}