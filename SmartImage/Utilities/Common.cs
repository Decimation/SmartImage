using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace SmartImage.Utilities
{
	// todo
	internal static class Common
	{
		

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

		internal static string QuickJoin<T>(this IEnumerable<T> enumerable, string delim = ", ") => String.Join(delim, enumerable);

		internal static TEnum ReadEnumFromSet<TEnum>(ISet<object> set) where TEnum : Enum
		{
			var t = typeof(TEnum);

			if (t.GetCustomAttribute<FlagsAttribute>() != null) {

				var sz = QuickJoin(set);
				Enum.TryParse(typeof(TEnum), (string) sz, out var e);
				return (TEnum) e;
			}

			return default;
		}

		public static void WriteMap(IDictionary<string, string> d, string filename)
		{
			string[] lines = d.Select(kvp => kvp.Key + "=" + kvp.Value).ToArray();
			File.WriteAllLines(filename, lines);
		}

		public static IDictionary<string, string> ReadMap(string filename)
		{
			string[] lines = File.ReadAllLines(filename);
			var dict = lines.Select(l => l.Split('=')).ToDictionary(a => a[0], a => a[1]);

			return dict;
		}
	}
}