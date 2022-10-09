using NStack;
using SmartImage.Lib;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terminal.Gui;
using Attribute = Terminal.Gui.Attribute;

namespace SmartImage;

public static partial class Program
{

	static public class Styles
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
			Normal   = Attribute.Make(Color.BrightBlue, Color.Black),
			Focus    = Attribute.Make(Color.Cyan, Color.DarkGray),
			Disabled = Attribute.Make(Color.BrightBlue, Color.DarkGray)
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

	public static class Gui2
	{
		private static ustring Err => ustring.Make(Application.Driver.HLine);

		private static ustring NA => ustring.Make(Application.Driver.RightDefaultIndicator);

		private static ustring OK => ustring.Make(Application.Driver.Checked);

		private static ustring PRC => ustring.Make(Application.Driver.Diamond);

		private static readonly SearchEngineOptions[] EngineNames = Enum.GetValues<SearchEngineOptions>();

		private static readonly Toplevel Top = Application.Top;

		private static readonly Window Win = new(Resources.Name)
		{
			X = 0,
			Y = 1, // Leave one row for the toplevel menu - todo

			// By using Dim.Fill(), it will automatically resize without manual intervention
			Width = Dim.Fill(),
			Height = Dim.Fill(),
			ColorScheme = Styles.CS_Win

		};

		private static readonly Label Lbl_Input = new("Input:")
		{
			X = 3,
			Y = 2,
			ColorScheme = Styles.CS_Elem2
		};

		private static readonly TextField Tf_Input = new(ustring.Empty)
		{
			X = Pos.Right(Lbl_Input),
			Y = Pos.Top(Lbl_Input),
			Width = 50,
			ColorScheme = Styles.CS_Win2,

			// AutoSize = true,
		};

		private static readonly TextField Tf_Query = new(ustring.Empty)
		{
			X = Pos.X(Tf_Input),
			Y = Pos.Bottom(Tf_Input),
			Width = 50,
			ColorScheme = Styles.CS_Win2,
			ReadOnly = true,
			CanFocus = false,

			// AutoSize = true,
		};

		private static readonly ListView Lv_Engines = new(new Rect(3, 8, 15, 25), EngineNames)
		{
			AllowsMultipleSelection = true,
			AllowsMarking = true,
			CanFocus = true,
			// ColorScheme             = GS.CS_Elem3,
		};

		private static readonly Button Btn_Ok = new("Run")
		{
			X = Pos.Right(Tf_Input) + 2,
			Y = Pos.Y(Tf_Input),
			ColorScheme = Styles.CS_Elem1
		};

		private static readonly Label Lbl_InputOk = new(NA)
		{
			X = Pos.Right(Btn_Ok),
			Y = Pos.Y(Btn_Ok),
			ColorScheme = Styles.CS_Elem4
		};

		public static readonly Button Btn_Clear = new Button("X")
		{
			X = Pos.Right(Lbl_InputOk),
			Y = 2

		};

		private static readonly ComboBox Cb_Engines = new(new Rect(3, 8, 15, 25), EngineNames)
		{
			// CanFocus = true,
			// ColorScheme             = GS.CS_Elem3,
		};

		private static readonly ListView Lv_Results = new(new Rect(20, 8, 25, 30), Program.Results)
		{
			X = Pos.Right(Cb_Engines),
			Y = Pos.Bottom(Btn_Ok),
			AutoSize = true
		};

		private static readonly Label Lbl_Query = new(">>>")
		{
			X = Pos.X(Lbl_Input),
			Y = Pos.Bottom(Lbl_Input),
			ColorScheme = Styles.CS_Elem2
		};

		private static readonly DataTable Dt_Config = new DataTable();

		private static readonly TableView Tv_Config = new(Dt_Config);

		static Gui2()
		{
			
		}

		public static void Run2()
		{
			Win.Add(Lbl_Input, Tf_Input, Btn_Ok, Lbl_InputOk, /*Cb_Engines,*/ Tf_Query,
			        Lbl_Query, Btn_Clear, Lv_Results, Cb_Engines
			);

			Top.Add(Win);
		}
	}

}