using Terminal.Gui;
using Attribute = Terminal.Gui.Attribute;
// ReSharper disable InconsistentNaming

namespace SmartImage.Shell;

internal static partial class UI
{
	#region Attributes

	internal static readonly Attribute Atr_Green_Black         = Attribute.Make(Color.Green, Color.Black);
	internal static readonly Attribute Atr_Red_Black           = Attribute.Make(Color.Red, Color.Black);
	internal static readonly Attribute Atr_BrightYellow_Black  = Attribute.Make(Color.BrightYellow, Color.Black);
	internal static readonly Attribute Atr_White_Black         = Attribute.Make(Color.White, Color.Black);
	internal static readonly Attribute Atr_White_Blue          = Attribute.Make(Color.White, Color.Blue);
	internal static readonly Attribute Atr_White_Cyan          = Attribute.Make(Color.White, Color.Cyan);
	internal static readonly Attribute Atr_White_DarkGray      = Attribute.Make(Color.White, Color.DarkGray);
	internal static readonly Attribute Atr_White_BrightCyan    = Attribute.Make(Color.White, Color.BrightCyan);
	internal static readonly Attribute Atr_BrightCyan_DarkGray = Attribute.Make(Color.BrightCyan, Color.DarkGray);
	internal static readonly Attribute Atr_Cyan_Black          = Attribute.Make(Color.Cyan, Color.Black);
	internal static readonly Attribute Atr_Cyan_White          = Attribute.Make(Color.Cyan, Color.White);
	internal static readonly Attribute Atr_Blue_White          = Attribute.Make(Color.Blue, Color.White);
	internal static readonly Attribute Atr_BrightBlue_White    = Attribute.Make(Color.BrightBlue, Color.White);
	internal static readonly Attribute Atr_BrightBlue_Gray     = Attribute.Make(Color.BrightBlue, Color.Gray);
	internal static readonly Attribute Atr_BrightRed_Black     = Attribute.Make(Color.BrightRed, Color.Black);
	internal static readonly Attribute Atr_BrightGreen_Black   = Attribute.Make(Color.BrightGreen, Color.Black);
	internal static readonly Attribute Atr_Black_White         = Attribute.Make(Color.Black, Color.White);
	internal static readonly Attribute Atr_Gray_Black          = Attribute.Make(Color.Gray, Color.Black);
	internal static readonly Attribute Atr_DarkGray_Blue       = Attribute.Make(Color.DarkGray, Color.Blue);
	internal static readonly Attribute Atr_DarkGray_Black      = Attribute.Make(Color.DarkGray, Color.Black);
	private static readonly  Attribute Atr_BrightBlue_Black    = Attribute.Make(Color.BrightBlue, Color.Black);
	private static readonly  Attribute Atr_Blue_Black          = Attribute.Make(Color.Blue, Color.Black);
	private static readonly  Attribute Atr_Brown_Black         = Attribute.Make(Color.Brown, Color.Black);
	private static readonly  Attribute Atr_DarkGray_White      = Attribute.Make(Color.DarkGray, Color.White);
	internal static readonly Attribute Atr_Black_DarkGray      = Attribute.Make(Color.Black, Color.DarkGray);

	#endregion

	#region Color schemes

	internal static readonly ColorScheme Cs_Btn1x = new()
	{
		Normal    = Atr_White_Cyan,
		Disabled  = Atr_DarkGray_Blue,
		HotNormal = Atr_White_Cyan,
		HotFocus  = Atr_White_BrightCyan,
		Focus     = Atr_White_BrightCyan,
	};

	internal static readonly ColorScheme Cs_Btn1 = new()
	{
		Normal    = Atr_Blue_White,
		Disabled  = Atr_DarkGray_White,
		HotNormal = Atr_Blue_White,
		HotFocus  = Atr_BrightBlue_Gray,
		Focus     = Atr_BrightBlue_Gray
	};

	internal static readonly ColorScheme Cs_Btn2 = new()
	{
		Normal    = Atr_Blue_White,
		Disabled  = Atr_DarkGray_White,
		HotNormal = Atr_Blue_White,
		HotFocus  = Atr_BrightBlue_Gray,
		Focus     = Atr_BrightBlue_Gray
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

	internal static readonly ColorScheme Cs_Title = new()
	{
		Normal = Atr_Red_Black,
		Focus  = Atr_BrightRed_Black
	};

	internal static readonly ColorScheme Cs_Win2 = new()
	{
		Normal = Atr_White_Black,
		Focus  = Atr_Blue_White,
	};

	internal static readonly ColorScheme Cs_ListView = new()
	{
		Disabled  = Atr_Gray_Black,
		Normal    = Atr_White_Black,
		HotNormal = Atr_White_Black,
		Focus     = Atr_Green_Black,
		HotFocus  = Atr_Green_Black
	};

	internal static readonly ColorScheme Cs_Lbl1 = new()
	{
		Normal    = UI.Atr_White_Black,
		HotNormal = UI.Atr_White_Black,
		Focus     = UI.Atr_Cyan_Black,
		HotFocus  = UI.Atr_Cyan_Black,
	};

	internal static readonly ColorScheme Cs_Lbl2 = new()
	{
		Normal    = Atr_BrightCyan_DarkGray,
		HotNormal = Atr_BrightCyan_DarkGray,
		Focus     = UI.Atr_Cyan_Black,
		HotFocus  = UI.Atr_Cyan_Black,
	};

	internal static readonly ColorScheme Cs_Lbl3 = new()
	{
		Normal    = UI.Atr_BrightBlue_Gray,
		HotNormal = UI.Atr_BrightBlue_Gray,
		Focus     = UI.Atr_Cyan_Black,
		HotFocus  = UI.Atr_Cyan_Black,
	};

	#endregion

	#region Styles

	internal static readonly Border Br_1 = new()
	{
		BorderStyle     = BorderStyle.Single,
		DrawMarginFrame = true,
		BorderThickness = new Thickness(2),
		BorderBrush     = Color.Red,
		Background      = Color.Black,
		Effect3D        = true,
	};

	#endregion

}