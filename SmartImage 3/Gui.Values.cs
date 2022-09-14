using System.Data;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using Kantan.Text;
using Novus.Utilities;
using NStack;
using SmartImage.Lib;
using Terminal.Gui;
using Attribute = System.Attribute;

// ReSharper disable InconsistentNaming

namespace SmartImage;

public static partial class Gui
{
	/// <summary>
	/// Contains values for Gui controls, each of which has functionality defined by <see cref="Functions"/>
	/// </summary>
	public static partial class Values
	{
		private static View[] All
		{
			//todo
			get { return TerminalHelper.GetViewFields(typeof(Values)); }
		}

		private static ustring Err => ustring.Make(Application.Driver.HLine);

		private static ustring NA => ustring.Make(Application.Driver.RightDefaultIndicator);

		private static ustring OK  => ustring.Make(Application.Driver.Checked);
		private static ustring PRC => ustring.Make(Application.Driver.Diamond);

		private static readonly SearchEngineOptions[] EngineNames = Enum.GetValues<SearchEngineOptions>();

		private static readonly Toplevel Top = Application.Top;

		private static readonly Window Win = new(Resources.Name)
		{
			X = 0,
			Y = 1, // Leave one row for the toplevel menu - todo

			// By using Dim.Fill(), it will automatically resize without manual intervention
			Width       = Dim.Fill(),
			Height      = Dim.Fill(),
			ColorScheme = Styles.CS_Win

		};

		private static readonly Label Lbl_Input = new("Input:")
		{
			X           = 3,
			Y           = 2,
			ColorScheme = Styles.CS_Elem2
		};

		private static readonly TextField Tf_Input = new(ustring.Empty)
		{
			X           = Pos.Right(Lbl_Input),
			Y           = Pos.Top(Lbl_Input),
			Width       = 50,
			ColorScheme = Styles.CS_Win2,

			// AutoSize = true,
		};

		private static readonly TextField Tf_Query = new(ustring.Empty)
		{
			X           = Pos.X(Tf_Input),
			Y           = Pos.Bottom(Tf_Input),
			Width       = 50,
			ColorScheme = Styles.CS_Win2,
			ReadOnly    = true,
			CanFocus    = false,

			// AutoSize = true,
		};

		private static readonly ListView Lv_Engines = new(new Rect(3, 8, 15, 25), EngineNames)
		{
			AllowsMultipleSelection = true,
			AllowsMarking           = true,
			CanFocus                = true,
			// ColorScheme             = GS.CS_Elem3,
		};

		private static readonly Button Btn_Ok = new("Run")
		{
			X           = Pos.Right(Tf_Input) + 2,
			Y           = Pos.Y(Tf_Input),
			ColorScheme = Styles.CS_Elem1
		};

		private static readonly Label Lbl_InputOk = new(NA)
		{
			X           = Pos.Right(Btn_Ok),
			Y           = Pos.Y(Btn_Ok),
			ColorScheme = Styles.CS_Elem4
		};

		public static readonly Button Btn_Clear = new Button("X")
		{
			X = Pos.Right(Lbl_InputOk),
			Y = 2

		};

		private static readonly ComboBox Cb_Engines = new(new Rect(3, 8, 15,25),EngineNames)
		{
			// CanFocus = true,
			// ColorScheme             = GS.CS_Elem3,
		};
		private static readonly ListView Lv_Results = new(new Rect(20, 8, 25, 30),Program.Results)
		{
			X= Pos.Right(Cb_Engines),
			Y=Pos.Bottom(Btn_Ok), AutoSize = true
		};

		private static readonly Label Lbl_Query = new(">>>")
		{
			X           = Pos.X(Lbl_Input),
			Y           = Pos.Bottom(Lbl_Input),
			ColorScheme = Styles.CS_Elem2
		};

		private static readonly DataTable Dt_Config = new DataTable();

		private static readonly TableView Tv_Config = new(Dt_Config);

		private static ustring? _tfInputStrBuffer = null;

		static Values()
		{
			Trace.WriteLine($"Init", nameof(Values));

			RuntimeHelpers.RunClassConstructor(typeof(Styles).TypeHandle);
			RuntimeHelpers.RunClassConstructor(typeof(Functions).TypeHandle);

			Top.Add(Win);

			Top.HotKey        = Key.Null;
			Win.HotKey        = Key.Null;
			Debug.WriteLine($"{All.Length}");
			
		}
	}
}