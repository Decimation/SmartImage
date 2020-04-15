using System;
using JetBrains.Annotations;

namespace SmartImage.Utilities
{
	public readonly struct AuthInfo
	{
		public string Id { get; }
		
		public bool IsNull { get; }
		
		public static readonly AuthInfo Null = new AuthInfo(String.Empty);

		public AuthInfo(string id) : this()
		{
			Id     = id;
			IsNull = String.IsNullOrWhiteSpace(id);
		}
	}
}