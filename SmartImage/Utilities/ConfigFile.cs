using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace SmartImage.Utilities
{
	public class ConfigFile
	{
		public string FileName { get; }

		public IDictionary<string, string> Config { get; }

		public ConfigFile(string fileName)
		{
			FileName = fileName;
			Config   = ReadMap(fileName);
		}

		public void Write<T>(string name, T value)
		{
			var valStr = value.ToString();
			if (!Config.ContainsKey(name)) {
				Config.Add(name, valStr);
			}
			else {
				Config[name] = valStr;
			}

			Store();
		}


		public T Read<T>(string name, bool setDefaultIfNull = false, T defaultValue = default)
		{
			if (!Config.ContainsKey(name)) {
				Config.Add(name, string.Empty);
				Store();
			}

			var rawValue = Config[name];

			if (setDefaultIfNull && String.IsNullOrWhiteSpace((string) rawValue)) {
				Write(name, defaultValue.ToString());
				rawValue = Read<string>(name);
			}

			if (typeof(T).IsEnum) {
				Enum.TryParse(typeof(T), (string) rawValue, out var e);
				return (T) e;
			}

			return (T) (object) rawValue;
		}

		public void Store() => WriteMap(Config, FileName);

		public static void WriteMap(IDictionary<string, string> d, string filename)
		{
			string[] lines = d.Select(kvp => kvp.Key + "=" + kvp.Value).ToArray();
			File.WriteAllLines(filename, lines);
		}

		public static IDictionary<string, string> ReadMap(string filename)
		{
			string[] lines = File.ReadAllLines(filename);
			var      dict  = lines.Select(l => l.Split('=')).ToDictionary(a => a[0], a => a[1]);

			return dict;
		}
	}
}