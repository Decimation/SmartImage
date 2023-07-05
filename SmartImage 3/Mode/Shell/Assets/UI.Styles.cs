// Read S SmartImage UI.Styles.cs
// 2023-02-14 @ 12:12 AM

#region

using Color = Terminal.Gui.Color;
using System.Runtime.Caching;
using AngleSharp.Dom;
using Terminal.Gui;
using Attribute = Terminal.Gui.Attribute;

#endregion

// ReSharper disable InconsistentNaming

namespace SmartImage.Mode.Shell.Assets;

// todo: possible overkill with caching
internal static partial class UI
{
	internal static readonly Attribute Atr_Green_White         = Attribute.Make(Color.Green, Color.White);
	internal static readonly Attribute Atr_Green_Gray          = Attribute.Make(Color.Green, Color.Gray);
	internal static readonly Attribute Atr_BrightGreen_White   = Attribute.Make(Color.BrightGreen, Color.White);
	internal static readonly Attribute Atr_BrightGreen_Gray    = Attribute.Make(Color.BrightGreen, Color.Gray);
	internal static readonly Attribute Atr_BrightRed_White     = Attribute.Make(Color.BrightRed, Color.White);
	internal static readonly Attribute Atr_BrightRed_Gray      = Attribute.Make(Color.BrightRed, Color.Gray);
	internal static readonly Attribute Atr_Brown_White         = Attribute.Make(Color.Brown, Color.White);
	internal static readonly Attribute Atr_Red_Black           = Attribute.Make(Color.Red, Color.Black);
	internal static readonly Attribute Atr_Red_White           = Attribute.Make(Color.Red, Color.White);
	internal static readonly Attribute Atr_White_Black         = Attribute.Make(Color.White, Color.Black);
	internal static readonly Attribute Atr_White_Cyan          = Attribute.Make(Color.White, Color.Cyan);
	internal static readonly Attribute Atr_White_Gray      = Attribute.Make(Color.White, Color.Gray);
	internal static readonly Attribute Atr_White_DarkGray      = Attribute.Make(Color.White, Color.DarkGray);
	internal static readonly Attribute Atr_White_BrightCyan    = Attribute.Make(Color.White, Color.BrightCyan);
	internal static readonly Attribute Atr_BrightCyan_DarkGray = Attribute.Make(Color.BrightCyan, Color.DarkGray);
	internal static readonly Attribute Atr_Cyan_Black          = Attribute.Make(Color.Cyan, Color.Black);
	internal static readonly Attribute Atr_Blue_White          = Attribute.Make(Color.Blue, Color.White);
	internal static readonly Attribute Atr_Blue_Gray           = Attribute.Make(Color.Blue, Color.Gray);
	internal static readonly Attribute Atr_BrightBlue_Gray     = Attribute.Make(Color.BrightBlue, Color.Gray);
	internal static readonly Attribute Atr_BrightGreen_Black   = Attribute.Make(Color.BrightGreen, Color.Black);
	internal static readonly Attribute Atr_Gray_Black          = Attribute.Make(Color.Gray, Color.Black);
	internal static readonly Attribute Atr_DarkGray_Blue       = Attribute.Make(Color.DarkGray, Color.Blue);
	internal static readonly Attribute Atr_DarkGray_Black      = Attribute.Make(Color.DarkGray, Color.Black);
	private static readonly  Attribute Atr_DarkGray_White      = Attribute.Make(Color.DarkGray, Color.White);
	internal static readonly Attribute Atr_Black_DarkGray      = Attribute.Make(Color.Black, Color.DarkGray);
	internal static readonly Attribute Atr_Black_Gray          = Attribute.Make(Color.Black, Color.Gray);
	internal static readonly Attribute Atr_Brown_Gray          = Attribute.Make(Color.Brown, Color.Gray);

	internal static readonly ColorScheme Cs_Err        = Make(Atr_BrightRed_White, disabled: Atr_BrightRed_Gray);
	internal static readonly ColorScheme Cs_Ok         = Make(Atr_BrightGreen_White, disabled: Atr_BrightGreen_Gray);
	internal static readonly ColorScheme Cs_NA         = Make(Atr_Brown_White, disabled: Atr_Brown_Gray);
	internal static readonly ColorScheme Cs_Btn_Cancel = Make(Atr_Red_White, disabled: Atr_DarkGray_White);

	internal static readonly ColorScheme Cs_Btn1x = new()
	{
		Normal   = Atr_White_Cyan,
		Disabled = Atr_DarkGray_Blue,
		// HotNormal = Atr_White_Cyan,
		// HotFocus  = Atr_White_BrightCyan,
		Focus     = Atr_White_BrightCyan,
		HotNormal = Attribute.Make(Color.BrightYellow, Color.Cyan),
		HotFocus  = Attribute.Make(Color.BrightYellow, Color.BrightCyan)
	};

	internal static readonly ColorScheme Cs_Btn1 = new()
	{
		Normal    = Atr_Blue_White,
		Disabled  = Atr_DarkGray_White,
		HotNormal = Atr_Blue_White,
		HotFocus  = Atr_BrightBlue_Gray,
		Focus     = Atr_BrightBlue_Gray
	};

	internal static readonly ColorScheme Cs_Btn2x = new()
	{
		Normal    = Atr_Green_White,
		Disabled  = Atr_DarkGray_White,
		HotNormal = Atr_Green_White,
		HotFocus  = Atr_Green_Gray,
		Focus     = Atr_Green_Gray
	};

	internal static readonly ColorScheme Cs_Btn3 = new()
	{
		Normal = Atr_Black_DarkGray,
		// Disabled  = Atr_DarkGray_White,
		HotNormal = Atr_Black_DarkGray,
		HotFocus  = Atr_BrightBlue_Gray,
		Focus     = Atr_BrightBlue_Gray
	};

	internal static readonly ColorScheme Cs_Elem2 = new()
	{
		Normal   = Atr_White_Cyan,
		Disabled = Atr_DarkGray_Black
	};

	internal static readonly ColorScheme Cs_Win = new()
	{
		Normal    = Atr_White_Black,
		Focus     = Atr_Cyan_Black,
		Disabled  = Atr_Gray_Black,
		HotNormal = Atr_White_Black,
		HotFocus  = Atr_Cyan_Black
	};

	internal static readonly ColorScheme Cs_Lbl4 = new()
	{
		Normal    = Atr_Red_Black,
		HotNormal = Atr_Red_Black,
	};

	internal static readonly ColorScheme Cs_Win2 = Make(Atr_White_DarkGray, Atr_Blue_White);

	internal static readonly ColorScheme Cs_ListView2 = new()
	{
		Normal    = Atr_Black_Gray,
		HotNormal = Atr_Black_Gray,
		Focus     = Atr_Blue_Gray,
		HotFocus  = Atr_Blue_Gray
	};

	internal static readonly ColorScheme Cs_Lbl1 = new()
	{
		Normal    = Atr_White_Black,
		HotNormal = Atr_White_Black,
		Focus     = Atr_Cyan_Black,
		HotFocus  = Atr_Cyan_Black,
	};
	internal static readonly ColorScheme Cs_Lbl1x = new()
	{
		Normal    = Atr_Black_Gray,
		HotNormal = Atr_Black_Gray,
		Focus     = Atr_Cyan_Black,
		HotFocus  = Atr_Cyan_Black,
	};
	internal static readonly ColorScheme Cs_Lbl2 = new()
	{
		Normal    = Atr_BrightCyan_DarkGray,
		HotNormal = Atr_BrightCyan_DarkGray,
		Focus     = Atr_Cyan_Black,
		HotFocus  = Atr_Cyan_Black,
	};

	internal static readonly Border Br_1 = new()
	{
		BorderStyle     = BorderStyle.Single,
		DrawMarginFrame = true,
		BorderThickness = new Thickness(2),
		BorderBrush     = Color.Red,
		Background      = Color.Black,
		Effect3D        = true,
	};

	internal static readonly ColorScheme Cs_Lbl1_Success = new()
	{
		Normal    = Atr_BrightGreen_Black,
		HotNormal = Atr_BrightGreen_Black,
		Disabled  = Cs_Lbl1.Disabled,
		Focus     = Cs_Lbl1.Focus,
		HotFocus  = Cs_Lbl1.HotFocus
	};

	internal static readonly Dim Dim_30_Pct = Dim.Percent(30);
	internal static readonly Dim Dim_80_Pct = Dim.Percent(80);

	static UI() { }

	internal static ColorScheme Make(Attribute norm, Attribute? focus = null, Attribute? disabled = null)
	{
		focus    ??= Attribute.Get();
		disabled ??= Attribute.Get();

		return new ColorScheme()
		{
			Normal   = norm,
			Focus    = focus.Value,
			Disabled = disabled.Value
		}.NormalizeHot();
	}

	public static ColorScheme NormalizeHot(this ColorScheme cs)
	{
		cs.HotFocus  = cs.Focus;
		cs.HotNormal = cs.Normal;
		return cs;
	}

	private const int BRIGHT_DELTA = 0b1000;

	public static Color ToBrightVariant(this Color c)
	{
		// +8
		return (Color) ((int) c + BRIGHT_DELTA);
	}

	public static Color ToDarkVariant(this Color c)
	{
		// +8
		return (Color) ((int) c - BRIGHT_DELTA);
	}
}