using System.Collections.Generic;
using System.Drawing;
using Kantan.Utilities;
using SmartImage.Lib.Engines;

namespace SmartImage.UI
{
	internal static class Elements
	{
		internal static readonly Color ColorMain  = Color.Yellow;
		internal static readonly Color ColorOther = Color.Aquamarine;
		internal static readonly Color ColorYes   = Color.GreenYellow;
		internal static readonly Color ColorNo    = Color.Red;

		internal static readonly Dictionary<SearchEngineOptions, Color> EngineColorMap = new()
		{
			{SearchEngineOptions.Iqdb, Color.Pink},
			{SearchEngineOptions.SauceNao, Color.SpringGreen},
			{SearchEngineOptions.Ascii2D, Color.NavajoWhite},
			{SearchEngineOptions.Bing, Color.DeepSkyBlue},
			{SearchEngineOptions.GoogleImages, Color.FloralWhite},
			{SearchEngineOptions.ImgOps, Color.Gray},
			{SearchEngineOptions.KarmaDecay, Color.IndianRed},
			{SearchEngineOptions.Tidder, Color.Orange},
			{SearchEngineOptions.TraceMoe, Color.MediumSlateBlue},
			{SearchEngineOptions.Yandex, Color.OrangeRed},
			{SearchEngineOptions.TinEye, Color.CornflowerBlue},
		};

		private static readonly string Enabled = StringConstants.CHECK_MARK.ToString().AddColor(ColorYes);

		private static readonly string Disabled = StringConstants.MUL_SIGN.ToString().AddColor(ColorNo);

		internal static string ToToggleString(this bool b) => b ? Enabled : Disabled;

		internal static string GetName(string s, bool added) => $"{s} ({(added.ToToggleString())})";

	}
}