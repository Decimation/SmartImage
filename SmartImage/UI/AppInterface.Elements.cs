using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using JetBrains.Annotations;
using Kantan.Text;
using Kantan.Utilities;
using Novus.Win32;
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

		internal static readonly string CheckMark =
			Strings.EncodingConvert(Encoding.Unicode, CodePage437, Strings.Constants.CHECK_MARK.ToString());

		private static readonly string Enabled = CheckMark.AddColor(ColorYes);

		private static readonly string Disabled = Strings.Constants.MUL_SIGN.ToString().AddColor(ColorNo);

		internal static string ToToggleString(bool b) => b ? Enabled : Disabled;

		internal static string ToVersionString(Version v) => $"{v.Major}.{v.Minor}.{v.Build}";

		#endregion

		[UsedImplicitly]
		internal static string GetName(string s, bool added) => $"{s} ({(ToToggleString(added))})";
	}
}