using System.Drawing;
using System.Text;
using Kantan.Text;
using Kantan.Utilities;
using Novus.OS.Win32;
using SmartImage.Lib.Searching;

namespace SmartImage.UI;

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
	internal static readonly Color ColorKey2      = ColorKey.ChangeBrightness(.4f);
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


	private static readonly Encoding CodePage437 =
		CodePagesEncodingProvider.Instance.GetEncoding((int) Native.CodePages.CP_IBM437);

	private static readonly string CheckMark =
		Strings.EncodingConvert(Encoding.Unicode, CodePage437, Strings.Constants.CHECK_MARK.ToString());

	private static readonly string Enabled = CheckMark.AddColor(ColorYes);

	private static readonly string Disabled = Strings.Constants.MUL_SIGN.ToString().AddColor(ColorNo);


	internal static string GetToggleString(bool b) => b ? Enabled : Disabled;

	internal static string GetVersionString(Version v) => $"{v.Major}.{v.Minor}.{v.Build}";

	
}