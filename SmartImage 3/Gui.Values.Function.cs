using System.Diagnostics;
using NStack;
using SmartImage.Lib.Searching;
using Terminal.Gui;
using static Terminal.Gui.View;
using P = SmartImage_3.Program;

namespace SmartImage_3;

public static partial class Gui
{
	/// <summary>
	/// Contains functionality for controls defined within <see cref="Values"/>
	/// </summary>
	public partial class Values
	{
		private static bool _ok;

		private static class Functions
		{
			public static void Win_KeyPress(KeyEventEventArgs eventArgs)
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

			public static async void BtnOk_Clicked()
			{
				if (_ok) {
					Debug.WriteLine($"{Program.Client.Config}");
					var res = await Program.Client.RunSearchAsync(Program._query, Program.Cts, CancellationToken.None);
					Program._res.AddRange(res);

				}
			}

			public static void TfInput_TextChanging(TextChangingEventArgs obj)
			{
				// Debug.WriteLine($"new {obj.NewText}");

			}

			public static void TfInput_TextChanged(ustring u)
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

					Lbl_InputOk.Text = R.Sym_NA;
					Btn_Ok.Enabled   = false;
					Btn_Ok.Redraw(Btn_Ok.Bounds);
					Lbl_InputOk.Redraw(Lbl_InputOk.Bounds);
				}

				_tfInputStrBuffer = u;

			}

			public static async void TfInput_KeyPress(KeyEventEventArgs eventArgs)
			{
				switch (eventArgs.KeyEvent.Key) {
					case Key.Enter:
						Task ret;

						try {
							var s = Tf_Input.Text.ToString();
							var h = await ImageQuery.TryAllocHandleAsync(s);

							if (h != null) {
								var q = new ImageQuery(h);
								Lbl_InputOk.Text = Resources.Sym_PRC;
								Lbl_InputOk.Redraw(Lbl_InputOk.Bounds);

								await q.UploadAsync();
								Program._query = q;

								Tf_Query.Text = q.UploadUri.ToString();
								Tf_Query.Redraw(Tf_Query.Bounds);
								_ok = true;
							}
							else {
								_ok = false;

							}
						}
						catch (Exception e) {
							Debug.WriteLine($"{e.Message}", nameof(TfInput_KeyPress));
							_ok = false;
						}

						if (_ok) {
							Lbl_InputOk.Text += Resources.Sym_OK;

							Debug.WriteLine($"{Program._query} - {_ok} - {Program._query.UploadUri}", "Query");
						}
						else {
							Lbl_InputOk.Text = Resources.Sym_ERR;
							Tf_Query.Text    = ustring.Empty;
							Tf_Query.Redraw(Tf_Query.Bounds);
						}

						Btn_Ok.Enabled = _ok;

						// if (ImageQuery.TryAllocHandleAsync(loginText.Text.ToString(), out var ux)) { }
						// else { }
						break;
				}
			}

			static Functions()
			{
				Trace.WriteLine("Init", nameof(Functions));

				Win.KeyPress += Win_KeyPress;

				Btn_Ok.Clicked += BtnOk_Clicked;

				Tf_Input.TextChanged  += TfInput_TextChanged;
				Tf_Input.KeyPress     += TfInput_KeyPress;
				Tf_Input.TextChanging += TfInput_TextChanging;

				Btn_Clear.Clicked  += BtnClear_Clicked;
				Btn_Clear.KeyPress += BtnClear_KeyPress;

				Lv_Engines.OpenSelectedItem    += LvEngines_OpenSelectedItem;
				Lv_Engines.KeyPress            += LvEngines_KeyPress;
				Lv_Engines.SelectedItemChanged += LvEngines_SelectedItemChanged;

				Win.Add(Lbl_Input, Tf_Input, Btn_Ok, Lbl_InputOk, Lv_Engines, Tf_Query,
				        Lbl_Query, Btn_Clear
				);
			}

			private static void LvEngines_SelectedItemChanged(ListViewItemEventArgs eventArgs) { }

			private static void LvEngines_KeyPress(KeyEventEventArgs eventArgs)
			{
				Debug.WriteLine($"{eventArgs.KeyEvent} {P.Config.SearchEngines} {Lv_Engines.SelectedItem}");
			}

			private static void LvEngines_OpenSelectedItem(ListViewItemEventArgs eventArgs)
			{
				Debug.WriteLine($"{eventArgs.Value} {eventArgs.Item} osi");

				var value = eventArgs.Value;

				if (value == null) {
					return;
				}

				Debug.WriteLine($"{value} {eventArgs.Item} sic {Lv_Engines.Source}");
				var e = Enum.Parse<SearchEngineOptions>(value.ToString());

				switch (e) {
					case SearchEngineOptions.None:
						P.Config.SearchEngines = SearchEngineOptions.None;
						break;
				}

				var prev = P.Config.SearchEngines;
				P.Config.SearchEngines |= e;

				Debug.WriteLine($"{prev} | {e} -> {P.Config.SearchEngines}");
			}

			private static void BtnClear_KeyPress(KeyEventEventArgs args)
			{
				switch (args.KeyEvent) { }
			}

			private static void BtnClear_Clicked()
			{
				Tf_Input.DeleteAll();
			}
		}

		/*private static void LvEngines_SelectedItemChanged(ListViewItemEventArgs obj)
		{
			var s     = obj.Values.ToString();
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
	}
}