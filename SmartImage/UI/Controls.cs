using System.Diagnostics;
using System.Reflection;
using JetBrains.Annotations;
using Kantan.Cli;
using Kantan.Cli.Controls;
using Kantan.Utilities;
using Novus.Utilities;

namespace SmartImage.UI;

internal static class Controls
{
	public static ConsoleOption CreateOption(PropertyInfo member, string name, Action<bool> fn, object o)
	{
		bool initVal = (bool) member.GetValue(o);

		var option = new ConsoleOption
		{
			Name = Controls.GetName(name, initVal)
		};

		option.Function = () =>
		{
			var pi = member.DeclaringType.GetProperty(member.Name);

			bool curVal = (bool) pi.GetValue(null);
			fn(curVal);
			bool newVal = (bool) pi.GetValue(null);
			option.Name = GetName(name, newVal);

			Debug.Assert((bool) pi.GetValue(null) == newVal);

			return null;
		};

		return option;
	}

	public static ConsoleOption CreateOption(string field, string name, object t)
	{
		bool initVal = (bool) t.GetType().GetAnyResolvedField(field)
		                       .GetValue(t);

		var option = new ConsoleOption
		{
			Name = GetName(name, initVal)
		};

		option.Function = () =>
		{
			var fi = t.GetType().GetAnyResolvedField(field);

			object curVal = fi.GetValue(t);
			bool   newVal = !(bool) curVal;
			fi.SetValue(t, newVal);

			option.Name = GetName(name, newVal);

			Debug.Assert((bool) fi.GetValue(t) == newVal);

			Program.Reload(true);

			return null;
		};


		return option;
	}

	public static ConsoleOption CreateOption<T>(string f, string name, object o) where T : Enum
	{
		return new()
		{
			Name  = name,
			Color = Elements.ColorOther,
			Function = () =>
			{
				var enumOptions = ConsoleOption.FromEnum<T>();

				var selected = (new ConsoleDialog
					               {
						               Options        = enumOptions,
						               SelectMultiple = true
					               }).ReadInput();

				var enumValue = EnumHelper.ReadFromSet<T>(selected.Output);
				var field     = o.GetType().GetAnyResolvedField(f);
				field.SetValue(o, enumValue);

				Console.WriteLine(enumValue);

				ConsoleManager.WaitForSecond();

				Debug.Assert(((T) field.GetValue(o)).Equals(enumValue));

				Program.Reload(true);

				return null;
			}
		};
	}

	[UsedImplicitly]
	private static string GetName(string s, bool added) => $"{s} ({(Elements.GetToggleString(added))})";
}