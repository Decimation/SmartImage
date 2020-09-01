using System;
using System.Collections.Generic;
using System.Text;
using SimpleCore.Utilities;
using SmartImage.Model;
using SmartImage.Searching;

namespace SmartImage
{
	public static class Utilities
	{
		public static T Read<T>(string rawValue)
		{
			if (typeof(T).IsEnum)
			{
				Enum.TryParse(typeof(T), (string)rawValue, out var e);
				return (T)e;
			}

			if (typeof(T) == typeof(bool))
			{
				Boolean.TryParse(rawValue, out var b);
				return (T)(object)b;
			}

			return (T) (object) rawValue;
		}

		internal static string CleanString(String s)
		{
			s = s.Replace("\"", String.Empty);

			return s;
		}
	}
}
