using System;
using System.Diagnostics.CodeAnalysis;

namespace SmartImage.Utilities
{
	public class Win32
	{
		private const string PATH_ENV = "PATH";
		
		[return: NotNull]
		public static string GetEnvironmentPath()
		{
			var env = Environment.GetEnvironmentVariable(PATH_ENV, EnvironmentVariableTarget.User);
			if (env == null) {
				throw new NullReferenceException();
			}

			return env;
		}

		public static void SetEnvironmentPath(string s)
		{
			Environment.SetEnvironmentVariable(PATH_ENV, s, EnvironmentVariableTarget.User);
		}
	}
}