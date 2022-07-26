using System.Data;
using NStack;
using SmartImage.Lib.Searching;
using Terminal.Gui;

namespace SmartImage_3;

public static partial class Gui
{
	public static class Constants
	{
		public static readonly string[] EngineNames = Enum.GetNames<SearchEngineOptions>();

		public const string SYM_NA      = "-";
		public const string SYM_ERR     = "!";
		public const string SYM_OK      = "*";
		public const string SYM_PROCESS = "^";
		public const string NAME        = "SmartImage";
	}

	public static readonly Label Lbl_Query = new($">>>")
	{
		X           = Pos.X(Lbl_Input),
		Y           = Pos.Bottom(Lbl_Input),
		ColorScheme = GS.CS_Elem2
	};

	public static readonly Label Lbl_InputOk = new(GC.SYM_NA)
	{
		X           = Pos.Right(Btn_Ok),
		Y           = Pos.Y(Btn_Ok),
		ColorScheme = GS.CS_Elem4
	};

	private static DataTable Dt_Config = new DataTable()
	{
		Columns = { },
		Rows    = { }
	};

	public static ustring _tfInputStrBuffer = null;
}