// ReSharper disable InconsistentNaming

using NStack;
using Terminal.Gui;
using Attribute = Terminal.Gui.Attribute;

namespace SmartImage_3;

public static partial class Gui
{
	public static readonly Label Lbl_Input = new($"Input:")
	{
		X           = 3,
		Y           = 2,
		ColorScheme = GS.CS_Elem2
	};

	public static readonly TextField Tf_Query = new(ustring.Empty)
	{
		X           = Pos.X(Tf_Input),
		Y           = Pos.Bottom(Tf_Input),
		Width       = 50,
		ColorScheme = GS.CS_Win2,
		ReadOnly    = true,
		CanFocus    = false,

		// AutoSize = true,
	};

	public static readonly ListView Lv_Engines = new(new Rect(3, 8, 15, 25), GC.EngineNames)
	{
		AllowsMultipleSelection = true,
		AllowsMarking           = true,
		CanFocus                = true,
		ColorScheme             = GS.CS_Elem3,
	};

	private static readonly TableView Tv_Config = new(Dt_Config);
}