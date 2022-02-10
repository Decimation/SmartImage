using System;
using System.Drawing;
using System.Drawing.Drawing2D;
// ReSharper disable NotAccessedVariable

namespace SmartImage.Lib.Utilities;
#pragma warning disable CA1416
public static class ImageManipulation
{
	public static Bitmap ResizeImage(Bitmap mg, Size newSize)
	{
		// todo
		/*
		 * Adapted from https://stackoverflow.com/questions/5243203/how-to-compress-jpg-image
		 */
		double ratio         = 0d;
		double myThumbWidth  = 0d;
		double myThumbHeight = 0d;
		int    x             = 0;
		int    y             = 0;

		Bitmap bp;

		if (mg.Width / Convert.ToDouble(newSize.Width) > mg.Height / Convert.ToDouble(newSize.Height)) {
			ratio = Convert.ToDouble(mg.Width) / Convert.ToDouble(newSize.Width);
		}
		else {
			ratio = Convert.ToDouble(mg.Height) / Convert.ToDouble(newSize.Height);
		}

		myThumbHeight = Math.Ceiling(mg.Height / ratio);
		myThumbWidth  = Math.Ceiling(mg.Width / ratio);

		//Size thumbSize = new Size((int)myThumbWidth, (int)myThumbHeight);
		var thumbSize = new Size(newSize.Width, newSize.Height);
		bp = new Bitmap(newSize.Width, newSize.Height);
		x  = (newSize.Width - thumbSize.Width) / 2;
		y  = newSize.Height - thumbSize.Height;
		// Had to add System.Drawing class in front of Graphics ---
		Graphics g = Graphics.FromImage(bp);
		g.SmoothingMode     = SmoothingMode.HighQuality;
		g.InterpolationMode = InterpolationMode.HighQualityBicubic;
		g.PixelOffsetMode   = PixelOffsetMode.HighQuality;
		var rect = new Rectangle(x, y, thumbSize.Width, thumbSize.Height);
		g.DrawImage(mg, rect, 0, 0, mg.Width, mg.Height, GraphicsUnit.Pixel);

		return bp;

	}

	public static DisplayResolutionType GetDisplayResolution(int w, int h)
	{
		/*
		 *	Other			W < 1280
		 *	[HD, FHD)		[1280, 1920)	1280 <= W < 1920	W: >= 1280 < 1920
		 *	[FHD, QHD)		[1920, 2560)	1920 <= W < 2560	W: >= 1920 < 2560
		 *	[QHD, UHD)		[2560, 3840)	2560 <= W < 3840	W: >= 2560 < 3840
		 *	[UHD, ∞)											W: >= 3840
		 */

		return (w, h) switch
		{
			/*
			 * Specific resolutions
			 */

			(640, 360) => DisplayResolutionType.nHD,

			/*
			 * General resolutions
			 */

			_ => w switch
			{
				>= 1280 and < 1920 => DisplayResolutionType.HD,
				>= 1920 and < 2560 => DisplayResolutionType.FHD,
				>= 2560 and < 3840 => DisplayResolutionType.QHD,
				>= 3840            => DisplayResolutionType.UHD,
				_                  => DisplayResolutionType.Unknown
			}
		};

	}
}

public enum DisplayResolutionType
{
	Unknown,

	nHD,
	HD,
	FHD,
	QHD,
	UHD
}