using Terminal.Gui;
using Attribute = Terminal.Gui.Attribute;

// ReSharper disable InconsistentNaming

namespace SmartImage.UI;

internal static class Styles
{
	#region Attributes

	internal static readonly Attribute Atr_Green_Black        = Attribute.Make(Color.Green, Color.Black);
	internal static readonly Attribute Atr_Red_Black          = Attribute.Make(Color.Red, Color.Black);
	internal static readonly Attribute Atr_BrightYellow_Black = Attribute.Make(Color.BrightYellow, Color.Black);
	internal static readonly Attribute Atr_White_Black        = Attribute.Make(Color.White, Color.Black);
	internal static readonly Attribute Atr_Cyan_Black         = Attribute.Make(Color.Cyan, Color.Black);
	internal static readonly Attribute Atr_Cyan_White         = Attribute.Make(Color.Cyan, Color.White);
	internal static readonly Attribute Atr_Blue_White         = Attribute.Make(Color.Blue, Color.White);
	internal static readonly Attribute Atr_BrightRed_Black    = Attribute.Make(Color.BrightRed, Color.Black);
	internal static readonly Attribute Atr_BrightGreen_Black  = Attribute.Make(Color.BrightGreen, Color.Black);
	internal static readonly Attribute Atr_Black_White        = Attribute.Make(Color.Black, Color.White);
	internal static readonly Attribute Atr_Gray_Black         = Attribute.Make(Color.Gray, Color.Black);
	internal static readonly Attribute Atr_White_DarkGray     = Attribute.Make(Color.White, Color.DarkGray);
	internal static readonly Attribute Atr_DarkGray_Black     = Attribute.Make(Color.DarkGray, Color.Black);
	private static readonly  Attribute Atr_BrightBlue_Black   = Attribute.Make(Color.BrightBlue, Color.Black);
	private static readonly  Attribute Atr_Blue_Black         = Attribute.Make(Color.Blue, Color.Black);
	private static readonly  Attribute Atr_Brown_Black        = Attribute.Make(Color.Brown, Color.Black);

	#endregion

	#region Color schemes

	internal static readonly ColorScheme Cs_Btn1x = new()
	{
		Normal    = Atr_Cyan_Black,
		Focus     = Atr_BrightGreen_Black,
		Disabled  = Atr_BrightYellow_Black,
		HotNormal = Atr_Cyan_Black,
		HotFocus  = Atr_BrightGreen_Black
	};

	internal static readonly ColorScheme Cs_Btn1 = new()
	{
		Normal    = Atr_BrightBlue_Black,
		Disabled  = Atr_DarkGray_Black,
		HotNormal = Atr_BrightBlue_Black,
		HotFocus  = Atr_Blue_Black,
		Focus     = Atr_Blue_Black
	};
	internal static readonly ColorScheme Cs_Btn2 = new()
	{
		Normal    = Atr_Brown_Black,
		Disabled  = Atr_DarkGray_Black,
		HotNormal = Atr_Brown_Black,
		HotFocus  = Atr_Blue_Black,
		Focus     = Atr_Blue_Black
	};
	internal static readonly ColorScheme Cs_Elem2 = new()
	{
		Normal   = Atr_Cyan_Black,
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