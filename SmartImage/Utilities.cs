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
		public static string Prettify(string s)
		{
			char c = '*';
			var ELLIPSES = "...";
			int lim = Console.BufferWidth - (10 + ELLIPSES.Length);

			var srg = s.Split('\n');

			for (int i = 0; i < srg.Length; i++) {
				var y = " " + srg[i];

				string x;


				if (string.IsNullOrWhiteSpace(y)) {
					x = string.Empty;
				}
				else {
					x = c + y;
				}

				var x2 = x.Truncate(lim);

				if (x2.Length < x.Length) {
					x2 += ELLIPSES;
				}


				srg[i] = x2;


			}

			var s2 = string.Join('\n', srg);

			return s2;
		}

		public static T Read<T>(string rawValue)
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
	}
}