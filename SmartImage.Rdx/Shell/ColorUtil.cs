// Author: Deci | Project: SmartImage.Rdx | Name: ColorUtil.cs
// Date: 2024/04/24 @ 21:04:10

global using SysColor = System.Drawing.Color;
global using SpcColor = Spectre.Console.Color;
global using ImsColor = SixLabors.ImageSharp.Color;
global using AngColor = AngleSharp.Css.Values.Color;
using Spectre.Console;

namespace SmartImage.Rdx.Shell;

internal static class ColorUtil
{

	public const double BYTE_D = Byte.MaxValue;

	private const double LUM_DELTA = 0.05;

	public static double GetLuminance(this SpcColor c)
	{
		return 0.2126 * c.R / BYTE_D + 0.7152 * c.G / BYTE_D + 0.0722 * c.B / BYTE_D;
	}

	public static double GetContrastRatio(this SpcColor color1, SpcColor color2)
	{
		double luminance1 = color1.GetLuminance();
		double luminance2 = color2.GetLuminance();

		if (luminance1 > luminance2)
			return (luminance1 + LUM_DELTA) / (luminance2 + LUM_DELTA);
		else
			return (luminance2 + LUM_DELTA) / (luminance1 + LUM_DELTA);
	}

	public static IEnumerable<SpcColor> Interpolate(this SpcColor a, SpcColor b, byte nIntervals = 10)
	{
		byte r2 = b.R, r1 = a.R;
		byte g2 = b.G, g1 = a.G;
		byte b2 = b.B, b1 = a.B;

		byte intervalR = (byte) ((r2 - r1) / nIntervals);
		byte intervalG = (byte) ((g2 - g1) / nIntervals);
		byte intervalB = (byte) ((b2 - b1) / nIntervals);

		var currentR = r1;
		var currentG = g1;
		var currentB = b1;

		for (var i = 0; i <= nIntervals; i++) {
			// var color = SysColor.FromArgb(currentR, currentG, currentB);
			var color = new SpcColor(currentR, currentG, currentB);

			//do something with color.

			//increment.
			currentR += intervalR;
			currentG += intervalG;
			currentB += intervalB;
			yield return color;
		}
	}

}