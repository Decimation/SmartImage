using System;
using System.Collections.Generic;
using System.Reflection;

namespace SmartImage.Utilities
{
	// todo
	internal static class CommonUtilities
	{
		// todo

		private static readonly Random Random = new Random();

		internal static T GetRandomElement<T>(T[] rg)
		{
			var i = Random.Next(0, rg.Length);
			return rg[i];
		}

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

		internal static string CreateSeparator(string s)
		{
			var sx= new string('-', 10);
			return sx + s + sx;

		}

		internal static string CleanString(string s)
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