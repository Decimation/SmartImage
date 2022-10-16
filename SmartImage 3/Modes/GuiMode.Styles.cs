using Terminal.Gui;
using Attribute = Terminal.Gui.Attribute;

// ReSharper disable InconsistentNaming

namespace SmartImage.Modes;

public sealed partial class GuiMode
{
	private static class Styles
	{
		#region Attributes

		internal static readonly Attribute Atr_Green_Black        = Attribute.Make(Color.Green, Color.Black);
		internal static readonly Attribute Atr_Red_Black          = Attribute.Make(Color.Red, Color.Black);
		internal static readonly Attribute Atr_BrightYellow_Black = Attribute.Make(Color.BrightYellow, Color.Black);
		internal static readonly Attribute Atr_White_Black        = Attribute.Make(Color.White, Color.Black);
		internal static readonly Attribute Atr_Cyan_Black         = Attribute.Make(Color.Cyan, Color.Black);

		internal static readonly Attribute Atr_BrightRed_Black   = Attribute.Make(Color.BrightRed, Color.Black);
		internal static readonly Attribute Atr_BrightGreen_Black = Attribute.Make(Color.BrightGreen, Color.Black);

		internal static readonly Attribute Atr_Black_White = Attribute.Make(Color.Black, Color.White);

		#endregion

		#region Color schemes

		internal static readonly ColorScheme Cs_Elem1 = new()
		{
			Normal    = Atr_Green_Black,
			Focus     = Atr_BrightGreen_Black,
			Disabled  = Atr_BrightYellow_Black,
			HotNormal = Atr_Green_Black,
			HotFocus  = Atr_BrightGreen_Black
		};

		internal static readonly ColorScheme Cs_Elem2 = new()
		{
			Normal   = Atr_Cyan_Black,
			Disabled = Attribute.Make(Color.DarkGray, Color.Black)
		};

		internal static readonly ColorScheme Cs_Elem3 = new()
		{
			Normal   = Attribute.Make(Color.BrightBlue, Color.Black),
			Focus    = Attribute.Make(Color.Cyan, Color.DarkGray),
			Disabled = Attribute.Make(Color.BrightBlue, Color.DarkGray)
		};

		internal static readonly ColorScheme Cs_Elem4 = new()
		{
			Normal = Attribute.Make(Color.Blue, Color.Gray),
		};

		internal static readonly ColorScheme Cs_Win = new()
		{
			Normal    = Atr_White_Black,
			Focus     = Atr_Cyan_Black,
			Disabled  = Attribute.Make(Color.Gray, Color.Black),
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
			Focus  = Atr_Black_White,
		};

		#endregion
	}
}