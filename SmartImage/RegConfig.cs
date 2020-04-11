using System;
using Microsoft.Win32;

namespace SmartImage
{
	public class RegConfig
	{
		public string SubkeyName { get; }

		private RegistryKey SubKey => Registry.CurrentUser.CreateSubKey(SubkeyName);

		public RegConfig(string subkeyName)
		{
			SubkeyName = subkeyName;
		}

		public T Read<T>(string name, bool setDefaultIfNull = false, T defaultValue = default)
		{
			var rawValue = this[name];

			if (setDefaultIfNull && string.IsNullOrWhiteSpace((string)rawValue)) {
				this[name] = defaultValue;
			}

			if (typeof(T).IsEnum) {
				Enum.TryParse(typeof(T),name, out var e);
				return (T) e;
			}

			return (T) rawValue;
		}

		public object this[string name] {
			get {
				var key = SubKey;

				var value = key.GetValue(name);

				key.Close();

				return value;
			}
			set {
				var key = SubKey;


				key.SetValue(name, value);

				key.Close();
			}
		}
	}
}