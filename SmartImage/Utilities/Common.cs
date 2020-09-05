using System;
using System.Collections.Generic;
using System.Reflection;

namespace SmartImage.Utilities
{
	internal static class Common
	{
		internal static T Read<T>(string rawValue)
		{
			if (typeof(T).IsEnum) {
				Enum.TryParse(typeof(T), (string) rawValue, out var e);
				return (T) e;
			}

			if (typeof(T) == typeof(bool)) {
				Boolean.TryParse(rawValue, out var b);
				return (T) (object) b;
			}

			return (T) (object) rawValue;
		}

		internal static string CleanString(String s)
		{
			s = s.Replace("\"", String.Empty);

			return s;
		}

		internal static string Join<T>(IEnumerable<T> enumerable) => String.Join(", ", enumerable);

		internal static TEnum ReadEnumFromSet<TEnum>(ISet<object> set) where TEnum : Enum
		{
			var t = typeof(TEnum);

			if (t.GetCustomAttribute<FlagsAttribute>() != null) {

				var sz = Join(set);
				Enum.TryParse(typeof(TEnum), (string) sz, out var e);
				return (TEnum) e;
			}

			return default;
		}
	}
}