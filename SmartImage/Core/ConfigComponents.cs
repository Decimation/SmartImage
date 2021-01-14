using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Novus.Utilities;
using SimpleCore.Utilities;
using SmartImage.Engines;
using SmartImage.Utilities;

namespace SmartImage.Core
{
	/// <summary>
	///     Utilities for <see cref="SearchConfig" />, <see cref="ConfigComponentAttribute" />
	/// </summary>
	internal static class ConfigComponents
	{
		/*
		 * Handles config components (properties or fields).
		 *
		 *
		 * A config component is a setting/option (i.e. field, parameter, etc).
		 * - Its value can be stored and later retrieved from a config file.
		 * - Its value can also be specified through the command line.
		 *
		 *
		 * mname - Member name
		 * id - Map name (Id)
		 */


		/// <summary>
		///     Update all fields annotated with <see cref="ConfigComponentAttribute" /> using <paramref name="obj" />.
		/// </summary>
		/// <param name="obj">Object</param>
		/// <param name="cfg">Map of values</param>
		internal static void UpdateFields(object obj, IDictionary<string, string> cfg)
		{
			var tuples = obj.GetType().GetAnnotated<ConfigComponentAttribute>();

			foreach (var (_, member) in tuples) {
				string memberName = member.Name;

				string? valStr = ReadComponentMapValue<object>(obj, cfg, memberName).ToString();

				var fi = member.GetBackingField();

				var val = ParseComponentValue(valStr, fi.FieldType);

				fi.SetValue(obj, val);
			}
		}

		/// <summary>
		///     Get all members annotated with <see cref="ConfigComponentAttribute" /> in <paramref name="obj" />.
		/// </summary>
		internal static (ConfigComponentAttribute Attribute, MemberInfo Member)[] GetMembers(object obj)
		{
			var tuples = obj.GetType().GetAnnotated<ConfigComponentAttribute>();

			return tuples.Select(y => (y.Attribute, y.Member)).ToArray();
		}

		/// <summary>
		///     Get all fields annotated with <see cref="ConfigComponentAttribute" /> in <paramref name="obj" />.
		/// </summary>
		internal static (ConfigComponentAttribute Attribute, FieldInfo Field)[] GetFields(object obj)
		{
			var tuples = GetMembers(obj);

			return tuples.Select(y => (y.Attribute, y.Member.GetBackingField())).ToArray();
		}


		/// <summary>
		///     Get field of name <paramref name="mname" /> annotated with <see cref="ConfigComponentAttribute" /> in
		///     <paramref name="obj" />.
		/// </summary>
		internal static (ConfigComponentAttribute Attribute, FieldInfo Field) GetField(object obj, string mname)
		{
			var t     = obj.GetType();
			var field = t.GetFieldAuto(mname);

			var attr = field.GetCustomAttribute<ConfigComponentAttribute>();

			return (attr, field);
		}

		/// <summary>
		///     Converts all members annotated with <see cref="ConfigComponentAttribute" /> in <paramref name="obj" /> to a
		///     <see cref="Dictionary{TKey,TValue}" />.
		/// </summary>
		internal static IDictionary<string, string> ConvertComponentsToMap(object obj)
		{
			var cfgFields = GetFields(obj);

			var keyValuePairs = cfgFields.Select(f =>
				new KeyValuePair<string, string>(f.Attribute.Id, f.Field.GetValue(obj).ToString()));

			return new Dictionary<string, string>(keyValuePairs);
		}


		internal static void WriteComponentsToFile(object obj, string dest) =>
			Collections.WriteDictionary(ConvertComponentsToMap(obj), dest);


		internal static T ReadComponentMapValue<T>(object obj, IDictionary<string, string> cfg, string mname)
		{
			var (attr, field) = GetField(obj, mname);

			var    defaultValue     = (T) attr.DefaultValue;
			bool   setDefaultIfNull = attr.SetDefaultIfNull;
			string name             = attr.Id;


			var v = ReadComponentMapValue(cfg, name, setDefaultIfNull, defaultValue);
			Debug.WriteLine($"{v} -> {name} {field.Name}");
			return v;
		}

		internal static object ParseComponentValue(string rawValue, Type t)
		{
			if (t.IsEnum) {
				Enum.TryParse(t, rawValue, out var e);
				return e;
			}

			if (t == typeof(bool)) {
				Boolean.TryParse(rawValue, out bool b);
				return b;
			}

			return rawValue;
		}

		internal static T ParseComponentValue<T>(string rawValue) =>
			(T) ParseComponentValue(rawValue, typeof(T));


		private static void AddToComponentMap<T>(IDictionary<string, string> cfg, string id, T value)
		{
			string? valStr = value.ToString();

			if (!cfg.ContainsKey(id)) {
				cfg.Add(id, valStr);
			}
			else {
				cfg[id] = valStr;
			}
		}

		/// <summary>
		///     Reset all members annotated with <see cref="ConfigComponentAttribute" /> within <paramref name="obj" /> to their
		///     respective <see cref="ConfigComponentAttribute.DefaultValue" />.
		/// </summary>
		internal static void ResetComponents(object obj)
		{
			var tuples = GetFields(obj);

			foreach (var (attr, field) in tuples) {
				var dv = attr.DefaultValue;
				field.SetValue(obj, dv);

				Debug.WriteLine($"Reset {dv} -> {field.Name}");
			}
		}

		/// <summary>
		///     Reset config component with member name <paramref name="mname" /> to its
		///     <seealso cref="ConfigComponentAttribute.DefaultValue" />.
		/// </summary>
		internal static void ResetComponent(object obj, string mname)
		{
			var (attr, field) = GetField(obj, mname);

			var dv = attr.DefaultValue;

			field.SetValue(obj, dv);
		}


		internal static T ReadComponentMapValue<T>(IDictionary<string, string> cfg, string id,
			bool setDefaultIfNull = false, T defaultValue = default)
		{

			if (!cfg.ContainsKey(id)) {
				cfg.Add(id, String.Empty);
			}

			string rawValue = cfg[id];

			if (setDefaultIfNull && String.IsNullOrWhiteSpace(rawValue)) {
				AddToComponentMap(cfg, id, defaultValue.ToString());
				rawValue = ReadComponentMapValue<string>(cfg, id);
			}

			var parse = ParseComponentValue<T>(rawValue);
			return parse;
		}


		internal static void ReadComponentFromArgument(object obj, IEnumerator<string> argEnumerator)
		{
			var parameterName = argEnumerator.Current;

			var members = GetMembers(obj);

			// Corresponding component
			var component = members.FirstOrDefault(y => y.Attribute.ParameterName == parameterName);

			if (component == default) {
				return;
			}

			argEnumerator.MoveNext();

			string argValueRaw = argEnumerator.Current;

			var field = component.Member.GetBackingField();

			var value = ParseComponentValue(argValueRaw, field.FieldType);

			field.SetValue(obj, value);
		}
	}
}