// Author: Deci | Project: SmartImage.Rdx | Name: ColorUtil.cs
// Date: 2024/04/24 @ 21:04:10

global using SysColor = System.Drawing.Color;
global using SpcColor = Spectre.Console.Color;
global using ImsColor = SixLabors.ImageSharp.Color;
using SmartImage.Lib.Engines;
using Spectre.Console;

namespace SmartImage.Rdx.Shell;

internal static class ColorUtil
{
	private const double LUM_DELTA = 0.05;

	extension(SpcColor colorA)
	{

		public double GetLuminance()
		{
			return 0.2126 * colorA.R / Byte.MaxValue + 0.7152 * colorA.G / Byte.MaxValue + 0.0722 * colorA.B / Byte.MaxValue;
		}

		public double GetContrastRatio(SpcColor colorB)
		{
			double luminance1 = colorA.GetLuminance();
			double luminance2 = colorB.GetLuminance();

			if (luminance1 > luminance2)
				return (luminance1 + LUM_DELTA) / (luminance2 + LUM_DELTA);
			else
				return (luminance2 + LUM_DELTA) / (luminance1 + LUM_DELTA);
		}

		public IEnumerable<SpcColor> Interpolate(SpcColor b, byte nIntervals = 10)
		{
			byte r2 = b.R, r1 = colorA.R;
			byte g2 = b.G, g1 = colorA.G;
			byte b2 = b.B, b1 = colorA.B;

			byte intervalR = (byte) ((r2 - r1) / nIntervals);
			byte intervalG = (byte) ((g2 - g1) / nIntervals);
			byte intervalB = (byte) ((b2 - b1) / nIntervals);

			var currentR = r1;
			var currentG = g1;
			var currentB = b1;

			for (int i = 0; i <= nIntervals; i++) {
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

}