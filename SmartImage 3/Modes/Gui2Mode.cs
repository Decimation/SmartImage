using NStack;
using SmartImage.Lib;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kantan.Net.Utilities;
using Terminal.Gui;
using Attribute = Terminal.Gui.Attribute;

// ReSharper disable InconsistentNaming

namespace SmartImage.Modes;

internal class Gui2Mode : BaseProgramMode
{
	#region Values

	private static ustring Err => ustring.Make(Application.Driver.HLine);

	private static ustring NA => ustring.Make(Application.Driver.RightDefaultIndicator);

	private static ustring OK => ustring.Make(Application.Driver.Checked);

	private static ustring PRC => ustring.Make(Application.Driver.Diamond);

	#endregion

	#region Controls

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
		X           = 1,
		Y           = 1,
		ColorScheme = Styles.CS_Elem2
	};

	private static readonly TextField Tf_Input = new(ustring.Empty)
	{
		X           = Pos.Right(Lbl_Input),
		Y           = Pos.Top(Lbl_Input),
		Width       = 50,
		ColorScheme = Styles.CS_Win2,
		AutoSize    = false
		// AutoSize = true,
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

	public static readonly Button Btn_Clear = new("X")
	{
		X = Pos.Right(Lbl_InputOk),
		Y = Pos.Y(Lbl_InputOk)

	};

	private static readonly ComboBox Cb_Engines = new(Cache.EngineOptions)
	{
		X        = Pos.Right(Btn_Clear),
		Y        = Pos.Y(Btn_Clear),
		AutoSize = true,
		Width    = 15,
		Height   = 25
	};

	private static readonly DataTable Dt_Results = new()
	{
		Columns = { "Engine" },
		Rows = {  }
	};

	private static readonly TableView Tv_Results = new()
	{
		X=Pos.Bottom(Tf_Input),
		Y = Pos.Y(Tf_Input) + 1,
		Width = 150,
		Height = 50,
		AutoSize = true
	};
	
	#endregion

	#region Overrides of ProgramMode

	public Gui2Mode() : base(SearchQuery.Null)
	{
		Application.Init();

		Btn_Ok.Clicked += async () =>
		{
			var text = Tf_Input.Text;

			Debug.WriteLine($"{text}");

			var sq = await SearchQuery.TryCreateAsync(text.ToString());

			Lbl_InputOk.Text = PRC;

			if (sq is { }) {
				await sq.UploadAsync();
			}
			else {
				Lbl_InputOk.Text = Err;
				return;
			}

			Debug.WriteLine($">> {sq}");

			Lbl_InputOk.Text = OK;

			Query  = sq;
			Status = true;
		};

		Btn_Clear.Clicked += () =>
		{
			try {
				Tf_Input.DeleteAll();
				Query            = SearchQuery.Null;
				Lbl_InputOk.Text = NA;
			}
			catch (Exception e) {
				Debug.WriteLine($"{e.Message}");
			}
		};

		Cb_Engines.OpenSelectedItem += args =>
		{
			Debug.WriteLine($"{args.Item} {args.Value}");
		};
		
		Tv_Results.Table = Dt_Results;

		Win.Add(Lbl_Input, Tf_Input, Btn_Ok, Lbl_InputOk,
		        /*Cb_Engines,*/ Btn_Clear, Cb_Engines, Tv_Results
		);
		
		Top.Add(Win);

	}

	public override async Task<object> RunAsync(string[] args)
	{

		return	Task.Run(() =>
		{
			Application.Run();
			return this;
		});
	}

	#region Overrides of BaseProgramMode

	#endregion
	public override async Task<bool> CanRun()
	{
		while (!Status) {
			
		}

		return true;
	}
	public override async Task PreSearchAsync(object? sender) { }

	public override async Task PostSearchAsync(object? sender, List<SearchResult> results1) { }

	public override async Task OnResult(object o, SearchResult r)
	{
		var textView = new TextView()
		{
			Text = $"{r.Engine.Name}"
		};
		textView.MouseClick += args =>
		{
			if (r is {First: {}}) {
				HttpUtilities.OpenUrl(r.First.Url);
			}
		};

		Dt_Results.Rows.Add(textView);
		Tv_Results.Redraw(Tv_Results.Bounds);
	}

	public override async Task OnComplete(object sender, List<SearchResult> e) { }

	public override async Task CloseAsync()
	{
		Application.Shutdown();

	}

	public override void Dispose() { }

	#endregion

	private static class Styles
	{
		private static readonly Attribute AT_GreenBlack        = Attribute.Make(Color.Green, Color.Black);
		private static readonly Attribute AT_RedBlack          = Attribute.Make(Color.Red, Color.Black);
		private static readonly Attribute AT_BrightYellowBlack = Attribute.Make(Color.BrightYellow, Color.Black);
		private static readonly Attribute AT_WhiteBlack        = Attribute.Make(Color.White, Color.Black);
		private static readonly Attribute AT_CyanBlack         = Attribute.Make(Color.Cyan, Color.Black);

		internal static readonly ColorScheme CS_Elem1 = new()
		{
			Normal    = AT_GreenBlack,
			Focus     = Attribute.Make(Color.BrightGreen, Color.Black),
			Disabled  = AT_BrightYellowBlack,
			HotNormal = AT_GreenBlack,
			HotFocus  = Attribute.Make(Color.BrightGreen, Color.Black)
		};

		internal static readonly ColorScheme CS_Elem2 = new()
		{
			Normal   = AT_CyanBlack,
			Disabled = Attribute.Make(Color.DarkGray, Color.Black)
		};

		internal static readonly ColorScheme CS_Elem3 = new()
		{
			Normal   = Attribute.Make(Color.BrightBlue, Color.Black),
			Focus    = Attribute.Make(Color.Cyan, Color.DarkGray),
			Disabled = Attribute.Make(Color.BrightBlue, Color.DarkGray)
		};

		internal static readonly ColorScheme CS_Elem4 = new()
		{
			Normal = Attribute.Make(Color.Blue, Color.Gray),
		};

		internal static readonly ColorScheme CS_Win = new()
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

		internal static readonly ColorScheme CS_Win2 = new()
		{
			Normal = Attribute.Make(Color.Black, Color.White),
			Focus  = Attribute.Make(background: Color.DarkGray, foreground: Color.White)
		};
	}
}