using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Text;
using JetBrains.Annotations;
using Kantan.Cli;
using Kantan.Cli.Controls;
using Kantan.Text;
using Kantan.Utilities;
using Novus.Utilities;
using Novus.Win32;
using SmartImage.Core;
using SmartImage.Lib.Engines;

namespace SmartImage.UI;

internal static partial class AppInterface
{
	internal static class Elements
	{
		#region Colors

		internal static readonly Color ColorMain      = Color.Yellow;
		internal static readonly Color ColorOther     = Color.Aquamarine;
		internal static readonly Color ColorYes       = Color.GreenYellow;
		internal static readonly Color ColorNo        = Color.Red;
		internal static readonly Color ColorHighlight = Color.LawnGreen;
		internal static readonly Color ColorError     = Color.LightCoral;
		internal static readonly Color ColorKey       = Color.SandyBrown;
		internal static readonly Color ColorOther2    = Color.MediumVioletRed;

		internal static readonly Dictionary<SearchEngineOptions, Color> EngineColorMap = new()
		{
			{ SearchEngineOptions.Iqdb, Color.Pink },
			{ SearchEngineOptions.SauceNao, Color.SpringGreen },
			{ SearchEngineOptions.Ascii2D, Color.NavajoWhite },
			{ SearchEngineOptions.Bing, Color.DeepSkyBlue },
			{ SearchEngineOptions.GoogleImages, Color.FloralWhite },
			{ SearchEngineOptions.ImgOps, Color.Gray },
			{ SearchEngineOptions.KarmaDecay, Color.IndianRed },
			{ SearchEngineOptions.Tidder, Color.Orange },
			{ SearchEngineOptions.TraceMoe, Color.MediumSlateBlue },
			{ SearchEngineOptions.Yandex, Color.OrangeRed },
			{ SearchEngineOptions.TinEye, Color.CornflowerBlue },
		};

		#endregion

		/*
		 * Note: Weird encoding nuance
		 */


		#region Constants

		private static readonly Encoding CodePage437 = CodePagesEncodingProvider.Instance.GetEncoding(Native.CP_IBM437);

		private static readonly string CheckMark =
			Strings.EncodingConvert(Encoding.Unicode, CodePage437, Strings.Constants.CHECK_MARK.ToString());

		private static readonly string Enabled = CheckMark.AddColor(ColorYes);

		private static readonly string Disabled = Strings.Constants.MUL_SIGN.ToString().AddColor(ColorNo);

		internal static string ToToggleString(bool b) => b ? Enabled : Disabled;

		internal static string ToVersionString(Version v) => $"{v.Major}.{v.Minor}.{v.Build}";

		#endregion

		[UsedImplicitly]
		internal static string GetName(string s, bool added) => $"{s} ({(ToToggleString(added))})";

		internal static ConsoleOption CreateEnumConfigOption<T>(string f, string name, object o) where T : Enum
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

					AppConfig.UpdateConfig();

					return null;
				}
			};
		}

		internal static ConsoleOption CreateConfigOption(PropertyInfo member, string name, Action<bool> fn, object o)
		{
			bool initVal = (bool) member.GetValue(o);

			var option = new ConsoleOption
			{
				Name = Elements.GetName(name, initVal)
			};

			option.Function = () =>
			{
				var  pi     = member.DeclaringType.GetProperty(member.Name);
				bool curVal = (bool) pi.GetValue(null);
				fn(curVal);
				bool newVal = (bool) pi.GetValue(null);
				option.Name = Elements.GetName(name, newVal);

				Debug.Assert((bool) pi.GetValue(null) == newVal);

				return null;
			};

			return option;
		}

		internal static ConsoleOption CreateConfigOption(string field, string name, object t)
		{
			bool initVal = (bool) (t).GetType().GetAnyResolvedField(field).GetValue(t);

			ConsoleOption option = new ConsoleOption
			{
				Name = Elements.GetName(name, initVal)
			};

			option.Function = () =>
			{
				var    fi     = t.GetType().GetAnyResolvedField(field);
				object curVal = fi.GetValue(t);
				bool   newVal = !(bool) curVal;
				fi.SetValue(t, newVal);


				option.Name = Elements.GetName(name, newVal);

				Debug.Assert((bool) fi.GetValue(t) == newVal);

				AppConfig.UpdateConfig();

				return null;
			};


			return option;
		}
	}
}