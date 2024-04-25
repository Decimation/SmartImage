// Author: Deci | Project: SmartImage.Rdx | Name: ColorUtil.cs
// Date: 2024/04/24 @ 21:04:10

using Spectre.Console;

namespace SmartImage.Rdx.Shell;

public static class ColorUtil
{

	public const double BYTE_D = 255.0;

	private const double LUM_DELTA = 0.05;

	public static double GetLuminance(this Color c)
	{
		return 0.2126 * c.R / BYTE_D + 0.7152 * c.G / BYTE_D + 0.0722 * c.B / BYTE_D;
	}

	public static double GetContrastRatio(this Color color1, Color color2)
	{
		double luminance1 = color1.GetLuminance();
		double luminance2 = color2.GetLuminance();

		if (luminance1 > luminance2)
			return (luminance1 + LUM_DELTA) / (luminance2 + LUM_DELTA);
		else
			return (luminance2 + LUM_DELTA) / (luminance1 + LUM_DELTA);
	}

}