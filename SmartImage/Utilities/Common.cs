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