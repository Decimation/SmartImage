using NStack;
using Terminal.Gui;
using Attribute = Terminal.Gui.Attribute;

namespace SmartImage_3;

public static partial class Gui
{
	public static class Styles
	{
		private static readonly Attribute AT_GreenBlack        = Attribute.Make(Color.Green, Color.Black);
		private static readonly Attribute AT_RedBlack          = Attribute.Make(Color.Red, Color.Black);
		private static readonly Attribute AT_BrightYellowBlack = Attribute.Make(Color.BrightYellow, Color.Black);
		private static readonly Attribute AT_WhiteBlack        = Attribute.Make(Color.White, Color.Black);
		private static readonly Attribute AT_CyanBlack         = Attribute.Make(Color.Cyan, Color.Black);

		public static readonly ColorScheme CS_Elem1 = new()
		{
			Normal    = AT_GreenBlack,
			Focus     = Attribute.Make(Color.BrightGreen, Color.Black),
			Disabled  = AT_BrightYellowBlack,
			HotNormal = AT_GreenBlack,
			HotFocus  = Attribute.Make(Color.BrightGreen, Color.Black)
		};

		public static readonly ColorScheme CS_Elem2 = new()
		{
			Normal   = AT_CyanBlack,
			Disabled = Attribute.Make(Color.DarkGray, Color.Black)
		};

		public static readonly ColorScheme CS_Elem3 = new()
		{
			Normal   = Attribute.Make(Color.Blue, Color.Black),
			Focus    = Attribute.Make(Color.BrightBlue, Color.Black),
			Disabled = Attribute.Make(Color.DarkGray, Color.Black)
		};

		public static readonly ColorScheme CS_Elem4 = new()
		{
			Normal = Attribute.Make(Color.Blue, Color.Gray),
		};

		public static readonly ColorScheme CS_Win = new()
		{
			Normal    = AT_WhiteBlack,
			Focus     = AT_CyanBlack,
			Disabled  = Attribute.Make(Color.Gray, Color.Black),
			HotNormal = AT_WhiteBlack,
			HotFocus  = AT_CyanBlack
		};

		private static readonly ColorScheme CS_Title = new()
		{
			Normal = AT_RedBlack,
			Focus  = Attribute.Make(Color.BrightRed, Color.Black)
		};

		public static readonly ColorScheme CS_Win2 = new()
		{
			Normal = Attribute.Make(Color.Black, Color.White),
			Focus  = Attribute.Make(background: Color.DarkGray, foreground: Color.White)
		};
	}

	public static readonly TextField Tf_Input = new(ustring.Empty)
	{
		X           = Pos.Right(Lbl_Input),
		Y           = Pos.Top(Lbl_Input),
		Width       = 50,
		ColorScheme = GS.CS_Win2,

		// AutoSize = true,
	};

	public static readonly Button Btn_Ok = new("Run")
	{
		X           = Pos.Right(Tf_Input) + 2,
		Y           = Pos.Y(Tf_Input),
		ColorScheme = GS.CS_Elem1
	};

	public static readonly Window Win = new(GC.NAME)
	{
		X = 0,
		Y = 1, // Leave one row for the toplevel menu - todo

		// By using Dim.Fill(), it will automatically resize without manual intervention
		Width       = Dim.Fill(),
		Height      = Dim.Fill(),
		ColorScheme = GS.CS_Win

	};

	public static readonly Toplevel Top = Application.Top;
}