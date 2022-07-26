global using G = SmartImage_3.Gui;
global using GC = SmartImage_3.Gui.Constants;
global using GS = SmartImage_3.Gui.Styles;
#nullable disable
#pragma warning disable CS0168
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

// ReSharper disable InconsistentNaming

namespace SmartImage_3;

public static class Program
{
	#region

	private static readonly SearchConfig Config = new();
	private static readonly SearchClient Client = new(Config);

	private static          ImageQuery              _query;
	private static          bool                    _ok  = false;
	private static readonly CancellationTokenSource Cts  = new();
	private static readonly List<SearchResult>      _res = new();

	#endregion

	#region Controls

	//todo

	//todo

	#endregion

	public static async Task Main(string[] args)
	{
		Application.Init();

		Console.OutputEncoding = Encoding.Unicode;

		Gui.Top.Add(Gui.Win);

		Gui.Top.HotKey = Key.Null;
		Gui.Win.HotKey   = Key.Null;

		Gui.Win.KeyPress         += Win_KeyPress;
		Gui.Btn_Ok.Clicked       += BtnOk_Clicked;
		Gui.Tf_Input.TextChanged += TfInput_TextChanged;
		Gui.Tf_Input.KeyPress    += TfInput_KeyPress;
		Gui.Tf_Input.TextChanging  += TfInput_TextChanging;

		// Lv_Engines.SelectedItemChanged += LvEngines_SelectedItemChanged;
		Gui.Lv_Engines.OpenSelectedItem += eventArgs =>
		{
			Debug.WriteLine($"{eventArgs.Value} {eventArgs.Item} osi");
		};

		Gui.Lv_Engines.KeyPress += eventArgs =>
		{
			Debug.WriteLine($"{eventArgs.KeyEvent}");
		};

		Gui.Lv_Engines.SelectedItemChanged += eventArgs =>
		{
			var value = eventArgs.Value;

			if (value == null) {
				return;
			}

			Debug.WriteLine($"{value} {eventArgs.Item} sic {Gui.Lv_Engines.Source}");
			var e = Enum.Parse<SearchEngineOptions>(value.ToString());

			switch (e) {
				case SearchEngineOptions.None:
					Config.SearchEngines = SearchEngineOptions.None;
					break;
			}

			var prev = Config.SearchEngines;
			Config.SearchEngines |= e;

			Debug.WriteLine($"{prev} | {e} -> {Config.SearchEngines}");
		};

		Gui.Win.Add(Gui.Lbl_Input, Gui.Tf_Input, Gui.Btn_Ok, Gui.Lbl_InputOk, Gui.Lv_Engines, Gui.Tf_Query, Gui.Lbl_Query
		);
		
		Application.Run();
		Application.Shutdown();
	}

	/*private static void LvEngines_SelectedItemChanged(ListViewItemEventArgs obj)
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

	}*/

	private static void Win_KeyPress(View.KeyEventEventArgs eventArgs)
	{
		switch (eventArgs.KeyEvent.Key) {
			case Key.F5:
				Console.Beep();
				Gui.Top.Redraw(Gui.Top.Bounds);
				Gui.Win.Redraw(Gui.Win.Bounds);
				//todo???
				break;
		}
	}

	private static async void BtnOk_Clicked()
	{
		if (_ok) {
			Debug.WriteLine($"{Client.Config}");
			var res = await Client.RunSearchAsync(_query, Cts, CancellationToken.None);
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

		if (ustring.IsNullOrEmpty(u) || ustring.IsNullOrEmpty(Gui.Tf_Input.Text)) {
			_ok = false;

			/*if (Config.Query is { IsUploaded: true }) {
				Config.Query.Dispose();
				Config.Query = null;
			}*/

			Gui.Lbl_InputOk.Text = GC.SYM_NA;
			Gui.Btn_Ok.Enabled     = false;
			Gui.Btn_Ok.Redraw(Gui.Btn_Ok.Bounds);
			Gui.Lbl_InputOk.Redraw(Gui.Lbl_InputOk.Bounds);
		}

		Gui._tfInputStrBuffer = u;

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
			var s = Gui.Tf_Input.Text.ToString();
			var h = await ImageQuery.TryAllocHandleAsync(s);

			if (h != null) {
				var q = new ImageQuery(h);
				Gui.Lbl_InputOk.Text = GC.SYM_PROCESS;
				Gui.Lbl_InputOk.Redraw(Gui.Lbl_InputOk.Bounds);

				await q.UploadAsync();
				_query = q;

				Gui.Tf_Query.Text = q.UploadUri.ToString();
				Gui.Tf_Query.Redraw(Gui.Tf_Query.Bounds);
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
			Gui.Lbl_InputOk.Text += GC.SYM_OK;

			Debug.WriteLine($"{_query} - {_ok} - {_query.UploadUri}", "Query");
		}
		else {
			Gui.Lbl_InputOk.Text = GC.SYM_ERR;
			Gui.Tf_Query.Text      = ustring.Empty;
			Gui.Tf_Query.Redraw(Gui.Tf_Query.Bounds);
		}

		Gui.Btn_Ok.Enabled = _ok;

		// if (ImageQuery.TryAllocHandleAsync(loginText.Text.ToString(), out var ux)) { }
		// else { }
	}
}