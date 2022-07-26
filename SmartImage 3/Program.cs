global using G = SmartImage_3.Gui;
global using GC = SmartImage_3.Gui.Constants;
global using GS = SmartImage_3.Gui.Styles;
using System.Data;
using System.Diagnostics;
using System.Text;
using Kantan.Cli;
using Kantan.Text;
using NStack;
using SmartImage.Lib;
using SmartImage.Lib.Searching;
using Terminal.Gui;
using Attribute = Terminal.Gui.Attribute;

namespace SmartImage_3;

public static class Program
{
	private static readonly SearchConfig            Config = new();
	private static readonly SearchClient            Client = new(Config);
	private static readonly CancellationTokenSource Cts    = new();
	private static readonly List<SearchResult>      _res   = new();

	private static bool _ok = false;

	private static readonly Label Lbl_Input = new($"Input:")
	{
		X           = 3,
		Y           = 2,
		ColorScheme = GS.CS_Elem2
	};

	private static readonly TextField Tf_Input = new(ustring.Empty)
	{
		X           = Pos.Right(Lbl_Input),
		Y           = Pos.Top(Lbl_Input),
		Width       = 50,
		ColorScheme = GS.CS_Win2,

		// AutoSize = true,
	};

	private static readonly Label Lbl_Query = new($">>>")
	{
		X           = Pos.X(Lbl_Input),
		Y           = Pos.Bottom(Lbl_Input),
		ColorScheme = GS.CS_Elem2
	};

	private static readonly TextField Tf_Query = new(ustring.Empty)
	{
		X           = Pos.X(Tf_Input),
		Y           = Pos.Bottom(Tf_Input),
		Width       = 50,
		ColorScheme = GS.CS_Win2,
		ReadOnly    = true,
		CanFocus    = false,

		// AutoSize = true,
	};

	private static readonly Button Btn_Ok = new("Run")
	{
		X           = Pos.Right(Tf_Input) + 2,
		Y           = Pos.Y(Tf_Input),
		ColorScheme = GS.CS_Elem1
	};

	private static readonly Label Lbl_InputOk = new(GC.SYM_NA)
	{
		X           = Pos.Right(Btn_Ok),
		Y           = Pos.Y(Btn_Ok),
		ColorScheme = GS.CS_Elem4
	};

	private static readonly ListView Lv_Engines = new(new Rect(3, 8, 15, 25), GC.EngineNames)
	{
		AllowsMultipleSelection = true,
		AllowsMarking           = true,
		CanFocus                = true,
		ColorScheme             = GS.CS_Elem3,
	};

	private static readonly Window Win = new(GC.NAME)
	{
		X = 0,
		Y = 1, // Leave one row for the toplevel menu - todo

		// By using Dim.Fill(), it will automatically resize without manual intervention
		Width       = Dim.Fill(),
		Height      = Dim.Fill(),
		ColorScheme = GS.CS_Win

	};

	private static DataTable Dt_Config = new DataTable()
	{
		Columns = { },
		Rows    = { }
	};

	private static readonly TableView Tv_Config = new(Dt_Config);

	private static readonly Toplevel Top = Application.Top;

	private static ustring _tfInputStrBuffer = null;

	public static async Task Main(string[] args)
	{
		Application.Init();

		Console.OutputEncoding = Encoding.Unicode;

		Top.Add(Win);

		Top.HotKey = Key.Null;
		Win.HotKey = Key.Null;

		Win.KeyPress          += Win_KeyPress;
		Btn_Ok.Clicked        += BtnOk_Clicked;
		Tf_Input.TextChanged  += TfInput_TextChanged;
		Tf_Input.KeyPress     += TfInput_KeyPress;
		Tf_Input.TextChanging += TfInput_TextChanging;

		// Lv_Engines.SelectedItemChanged += LvEngines_SelectedItemChanged;
		Lv_Engines.OpenSelectedItem += eventArgs =>
		{
			Debug.WriteLine($"{eventArgs.Value} {eventArgs.Item} osi");
		};

		Lv_Engines.KeyPress += eventArgs =>
		{
			switch (eventArgs.KeyEvent.Key) {
				case Key.Enter:
					for (int i = 0; i < Lv_Engines.Source.Length; i++) {
						if (Lv_Engines.Source.IsMarked(i)) {
							var v = Lv_Engines.Source.ToList()[i];

						}
					}

					break;

			}
		};

		Lv_Engines.SelectedItemChanged += eventArgs =>
		{
			Debug.WriteLine($"{eventArgs.Value} {eventArgs.Item} sic {Lv_Engines.Source}");
			var e = Enum.Parse<SearchEngineOptions>(eventArgs.Value.ToString());

			switch (e) {
				case SearchEngineOptions.None:
					Config.SearchEngines = SearchEngineOptions.None;
					break;
			}

			var prev = Config.SearchEngines;
			Config.SearchEngines |= e;

			Debug.WriteLine($"{prev} | {e} -> {Config.SearchEngines}");
		};

		Win.Add(
			Lbl_Input,
			Tf_Input,
			Btn_Ok,
			Lbl_InputOk,
			Lv_Engines,
			Tf_Query,
			Lbl_Query
		);

		// var source  = new CancellationTokenSource();
		// var task    = Client.RunSearchAsync(source, CancellationToken.None);
		// var wrapper = new ListWrapper(task.Result);
		// var view    = new ListView(wrapper) { X = Pos.Right(radioGroup) };

		Application.Run();
		Application.Shutdown();
	}

	private static void LvEngines_SelectedItemChanged(ListViewItemEventArgs obj)
	{
		var s     = obj.Value.ToString();
		var value = Enum.Parse<SearchEngineOptions>(s);

		switch (value) {
			case SearchEngineOptions.None:
				Config.SearchEngines = value;
				break;
			default:
				Config.SearchEngines |= value;
				break;
		}

		Debug.WriteLine($"{Config.SearchEngines}");

	}

	private static void Win_KeyPress(View.KeyEventEventArgs eventArgs)
	{
		switch (eventArgs.KeyEvent.Key) {
			case Key.F5:
				Console.Beep();
				Top.Redraw(Top.Bounds);
				Win.Redraw(Win.Bounds);
				//todo???
				break;
		}
	}

	private static async void BtnOk_Clicked()
	{
		if (_ok) {
			Debug.WriteLine($"{Client.Config}");
			var res = await Client.RunSearchAsync(Cts, CancellationToken.None);
			_res.AddRange(res);

		}
	}

	private static void TfInput_TextChanging(TextChangingEventArgs obj)
	{
		// Debug.WriteLine($"new {obj.NewText}");

	}

	private static void TfInput_TextChanged(ustring u)
	{
		// Debug.WriteLine($"change {u} buf: {_tfInputStrBuffer} actual {Tf_Input.Text}");

		/*_ok = false;
		Btn_Ok.Redraw(Btn_Ok.Bounds);
		Config.Query   = null;
		Btn_Ok.Enabled = false;*/

		if (ustring.IsNullOrEmpty(u) || ustring.IsNullOrEmpty(Tf_Input.Text)) {
			_ok = false;

			/*if (Config.Query is { IsUploaded: true }) {
				Config.Query.Dispose();
				Config.Query = null;
			}*/

			Lbl_InputOk.Text = GC.SYM_NA;
			Btn_Ok.Enabled   = false;
			Btn_Ok.Redraw(Btn_Ok.Bounds);
			Lbl_InputOk.Redraw(Lbl_InputOk.Bounds);
		}

		_tfInputStrBuffer = u;

	}

	private static async void TfInput_KeyPress(View.KeyEventEventArgs eventArgs)
	{
		switch (eventArgs.KeyEvent.Key) {
			case Key.Enter:
				await HandleInputQueryAsync();
				break;
		}
	}

	private static async Task HandleInputQueryAsync()
	{
		try {
			var s = Tf_Input.Text.ToString();
			var h = await ImageQuery.TryAllocHandleAsync(s);

			if (h != null) {
				var q = new ImageQuery(h);
				Lbl_InputOk.Text = GC.SYM_PROCESS;
				Lbl_InputOk.Redraw(Lbl_InputOk.Bounds);

				await q.UploadAsync();
				Config.Query = q;

				Tf_Query.Text = q.UploadUri.ToString();
				Tf_Query.Redraw(Tf_Query.Bounds);
				_ok = true;
			}
			else {
				_ok = false;

			}
		}
		catch (Exception e) {
			_ok = false;
		}
		finally { }

		if (_ok) {
			Lbl_InputOk.Text += GC.SYM_OK;

			Debug.WriteLine($"{Config.Query} - {_ok} - {Config.Query.UploadUri}", "Query");
		}
		else {
			Lbl_InputOk.Text = GC.SYM_ERR;
			Tf_Query.Text    = ustring.Empty;
			Tf_Query.Redraw(Tf_Query.Bounds);
		}

		Btn_Ok.Enabled = _ok;

		// if (ImageQuery.TryAllocHandleAsync(loginText.Text.ToString(), out var ux)) { }
		// else { }
	}
}