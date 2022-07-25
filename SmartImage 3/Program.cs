using System.Diagnostics;
using Kantan.Text;
using NStack;
using SmartImage.Lib;
using SmartImage.Lib.Searching;
using Terminal.Gui;

namespace SmartImage_3;

public static class Program
{
	private static SearchConfig            _cfg = new();
	private static SearchClient            _sc  = new(_cfg);
	private static CancellationTokenSource _cts = new();
	private static List<SearchResult>      _res = new();
	private static bool                    _ok  = false;

	public static async Task Main(string[] args)
	{

		Application.Init();
		var top = Application.Top;

		// Creates the top-level window to show
		var win = new Window("MyApp")
		{
			X = 0,
			Y = 1, // Leave one row for the toplevel menu

			// By using Dim.Fill(), it will automatically resize without manual intervention
			Width  = Dim.Fill(),
			Height = Dim.Fill()
		};

		top.Add(win);

		// Creates a menubar, the item "New" has a help menu.
		var menu = new ListView(new ListWrapper(new[] { "a" }));
		top.Add(menu);

		static bool Quit()
		{
			var n = MessageBox.Query(50, 7,
			                         "Quit Demo",
			                         "Are you sure you want to quit this demo?",
			                         "Yes", "No");
			return n == 0;
		}

		var login = new Label($"Input:")
		{
			X        = 3,
			AutoSize = true,
			Y        = 2
		};

		var loginText = new TextField("")
		{
			X     = Pos.Right(login),
			Y     = Pos.Top(login),
			Width = 40,
			// AutoSize = true,

		};

		ustring buf = null;

		loginText.TextChanged += (ustring u) =>
		{
			buf = u;
		};

		loginText.KeyPress += async eventArgs =>
		{
			switch (eventArgs.KeyEvent.Key) {
				case Key.Enter:

					var s          = loginText.Text.ToString();
					var h          = await ImageQuery.TryAllocHandleAsync(s);

					if (h!= null) {
						var q = new ImageQuery(h);
						_cfg.Query = q;

						_ok = true;
					}

					Debug.WriteLine($"{_cfg.Query} - {_ok}", "Query");
					// if (ImageQuery.TryAllocHandleAsync(loginText.Text.ToString(), out var ux)) { }
					// else { }

					break;
			}
		};

		// Add some controls, 
		var radioGroup =
			new RadioGroup(Enum.GetNames<SearchEngineOptions>().Select(x => ustring.Make(x.ToString())).ToArray())
			{
				X = 3,
				Y = 8,
			};

		var btnOk = new Button(3, 14, "Ok")
			{ };

		btnOk.Clicked += async () =>
		{

			if (_ok) {
				var res = await _sc.RunSearchAsync(_cts, CancellationToken.None);
				_res.AddRange(res);

			}
		};

		var btnCancel = new Button(10, 14, "Cancel");

		win.Add(
			// The ones with my favorite layout system, Computed
			login, loginText,

			// The ones laid out like an australopithecus, with Absolute positions:
			new CheckBox(3, 6, "Restart"),
			radioGroup,
			btnOk,
			btnCancel,
			new ListView(_res),
			new Label(3, 18, "Press F9 or ESC plus 9 to activate the menubar")
		);

		// var source  = new CancellationTokenSource();
		// var task    = _sc.RunSearchAsync(source, CancellationToken.None);
		// var wrapper = new ListWrapper(task.Result);
		// var view    = new ListView(wrapper) { X = Pos.Right(radioGroup) };

		Application.Run();
		Application.Shutdown();
	}
}