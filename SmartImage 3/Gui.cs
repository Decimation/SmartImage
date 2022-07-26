using Terminal.Gui;
using Attribute = Terminal.Gui.Attribute;

namespace SmartImage_3;

public static partial class Gui
{
	#region Attributes

	private static readonly Attribute AT_GreenBlack = Attribute.Make(Color.Green, Color.Black);
	private static readonly Attribute AT_RedBlack   = Attribute.Make(Color.Red, Color.Black);
	private static readonly Attribute AT_WhiteBlack = Attribute.Make(Color.White, Color.Black);
	private static readonly Attribute AT_CyanBlack  = Attribute.Make(Color.Cyan, Color.Black);

	#endregion

	#region Color schemes

	public static readonly ColorScheme CS_Elem1 = new()
	{
		Normal    = AT_GreenBlack,
		Disabled  = AT_RedBlack,
		HotNormal = AT_GreenBlack,
		HotFocus = AT_RedBlack
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

	public static readonly ColorScheme CS_Win = new()
	{
		Normal   = AT_WhiteBlack,
		Focus    = AT_CyanBlack,
		Disabled = Attribute.Make(Color.Gray, Color.Black),
		HotNormal = AT_WhiteBlack,
		HotFocus = AT_CyanBlack
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

	#endregion
}