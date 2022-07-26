global using G = SmartImage_3.Gui;
global using GC = SmartImage_3.Gui.Constants;
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
		AutoSize    = true,
		Y           = 2,
		ColorScheme = Gui.CS_Elem2
	};

	private static readonly TextField TF_Input = new(ustring.Empty)
	{
		X           = Pos.Right(Lbl_Input),
		Y           = Pos.Top(Lbl_Input),
		Width       = 50,
		ColorScheme = Gui.CS_Win2
		// AutoSize = true,

	};

	private static readonly Button Btn_Ok = new("Run")
	{
		X           = Pos.Right(TF_Input) + 2,
		Y           = Pos.Y(TF_Input),
		ColorScheme = Gui.CS_Elem1
	};

	private static readonly Label Lbl_InputOk = new(Gui.Constants.SYM_NA)
	{
		X           = Pos.Right(Btn_Ok),
		Y           = Pos.Y(Btn_Ok),
		ColorScheme = Gui.CS_Win2
	};

	private static readonly ListView Lv_Engines = new(new Rect(5, 5, 15, 15), Gui.Constants.EngineNames)
	{
		AllowsMultipleSelection = true,
		AllowsMarking           = true,
		ColorScheme             = Gui.CS_Elem3,
	};

	private static readonly Window Win = new(GC.NAME)
	{
		X = 0,
		Y = 1, // Leave one row for the toplevel menu - todo

		// By using Dim.Fill(), it will automatically resize without manual intervention
		Width       = Dim.Fill(),
		Height      = Dim.Fill(),
		ColorScheme = Gui.CS_Win

	};

	private static readonly Toplevel Top = Application.Top;

	private static ustring TF_InputStr = null;

	public static async Task Main(string[] args)
	{
		Application.Init();

		Console.OutputEncoding = Encoding.Unicode;

		var f = new MainLoop(new FakeMainLoop());
		f.AddTimeout(TimeSpan.FromMilliseconds(500), Callback);

		Win.KeyPress += eventArgs =>
		{
			switch (eventArgs.KeyEvent.Key) {
				case Key.F5:
					Console.Beep();
					Top.Redraw(Top.Bounds);
					Win.Redraw(Win.Bounds);
					break;
			}
		};

		Top.Add(Win);

		Btn_Ok.Clicked += async () =>
		{

			if (_ok) {
				var res = await Client.RunSearchAsync(Cts, CancellationToken.None);
				_res.AddRange(res);

			}
		};

		TF_Input.TextChanged += HandleInputChanged;

		TF_Input.KeyPress += HandleInputKeys;

		Win.Add(
			// The ones with my favorite layout system, Computed
			Lbl_Input, TF_Input,

			// The ones laid out like an australopithecus, with Absolute positions:
			Btn_Ok,
			Lbl_InputOk,
			Lv_Engines

			// new ListView(_res)
		);

		// var source  = new CancellationTokenSource();
		// var task    = Client.RunSearchAsync(source, CancellationToken.None);
		// var wrapper = new ListWrapper(task.Result);
		// var view    = new ListView(wrapper) { X = Pos.Right(radioGroup) };

		Application.Run();
		Application.Shutdown();
	}

	private static bool Callback(MainLoop loop)
	{
		ProcessInputForQuery().Wait();
		return true;
	}

	private static void HandleInputChanged(ustring u)
	{
		TF_InputStr = u;

		/*_ok = false;
		Btn_Ok.Redraw(Btn_Ok.Bounds);
		Config.Query   = null;
		Btn_Ok.Enabled = false;*/

		if (ustring.IsNullOrEmpty(u)) {
			_ok = false;
			Config.Query.Dispose();
			Config.Query     = null;
			Lbl_InputOk.Text = Gui.Constants.SYM_NA;
			Btn_Ok.Enabled   = false;
		}
	}

	private static async void HandleInputKeys(View.KeyEventEventArgs eventArgs)
	{
		switch (eventArgs.KeyEvent.Key) {
			case Key.Enter:

				await ProcessInputForQuery();

				break;
		}
	}

	private static async Task ProcessInputForQuery()
	{
		try {
			var s = TF_Input.Text.ToString();
			var h = await ImageQuery.TryAllocHandleAsync(s);

			if (h != null) {
				var q = new ImageQuery(h);
				Lbl_InputOk.Text = Gui.Constants.SYM_PROCESS;
				Lbl_InputOk.Redraw(Lbl_InputOk.Bounds);

				await q.UploadAsync();
				Config.Query = q;

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
			Lbl_InputOk.Text += Gui.Constants.SYM_OK;

			Debug.WriteLine($"{Config.Query} - {_ok} - {Config.Query.UploadUri}", "Query");
		}
		else {
			Lbl_InputOk.Text = Gui.Constants.SYM_ERR;
		}

		Btn_Ok.Enabled = _ok;

		// if (ImageQuery.TryAllocHandleAsync(loginText.Text.ToString(), out var ux)) { }
		// else { }
	}
}