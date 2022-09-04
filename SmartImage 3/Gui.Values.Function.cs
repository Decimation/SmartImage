using System.Diagnostics;
using Kantan.Text;
using NStack;
using SmartImage.Lib;
using Terminal.Gui;
using static Terminal.Gui.View;
using P = SmartImage.Program;

namespace SmartImage;

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

					var res =
						(await Program.Client.RunSearchAsync(Program._query, Program.Cts.Token)).ToList();
					Program.Results.AddRange(res);
					await Lv_Results.SetSourceAsync(res);

					Lv_Results.Redraw(Lv_Results.Bounds);

					foreach (var _res in res) {
						Debug.WriteLine($"{_res}");
					}

					Lv_Results.SetNeedsDisplay();

				}
			}

			public static void TfInput_TextChanging(TextChangingEventArgs obj)
			{
				// Debug.WriteLine($"new {obj.NewText}");

			}

			public static void TfInput_TextChanged(ustring u)
			{
				// Debug.WriteLine($"change {u} buf: {_tfInputStrBuffer} actual {Tf_Input.Text}");

				if (ustring.IsNullOrEmpty(u) || ustring.IsNullOrEmpty(Tf_Input.Text)) {
					_ok = false;

					Lbl_InputOk.Text = NA;
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

							var q = await SearchQuery.TryCreateAsync(s);

							if (q != null) {
								Lbl_InputOk.Text = PRC;
								Lbl_InputOk.Redraw(Lbl_InputOk.Bounds);

								await q.UploadAsync();
								Program._query = q;

								Tf_Query.Text = q.Upload.ToString();
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
							Lbl_InputOk.Text += OK;

							Debug.WriteLine($"{Program._query} - {_ok} - {Program._query.Upload}", "Query");
						}
						else {
							Lbl_InputOk.Text = Err;
							Tf_Query.Text    = ustring.Empty;
							Tf_Query.Redraw(Tf_Query.Bounds);
						}

						Btn_Ok.Enabled = _ok;

						// if (ImageQuery.TryAllocHandleAsync(loginText.Text.ToString(), out var ux)) { }
						// else { }
						break;
				}
			}

			private static void LvEngines_SelectedItemChanged(ListViewItemEventArgs eventArgs)
			{
				// Debug.WriteLine($"{eventArgs.Value} {eventArgs.Item} osi");

				var value = eventArgs.Value;

				if (value == null) {
					return;
				}

				var result = new List<string>();

				for (int i = 0; i < EngineNames.Length; i++) {
					if (Lv_Engines.Source.IsMarked(i)) {
						result.Add(EngineNames[i].ToString());
					}
				}

				Debug.WriteLine(
					$"{value} {eventArgs.Item} sic {Lv_Engines.Source} {Lv_Engines.Source.IsMarked(eventArgs.Item)} || {result.QuickJoin()}");

				var e = Enum.Parse<SearchEngineOptions>(value.ToString());

				switch (e) {
					case SearchEngineOptions.None:
						Program.Config.Engines = e;
						break;
					default:
						Program.Config.Engines |= e;
						break;
				}

				// var prev = P.Config.SearchEngines;
				// P.Config.SearchEngines |= e;
				// Lv_Engines.Source.SetMark(eventArgs.Item, true);

				// Debug.WriteLine($"{prev} | {e} -> {P.Config.SearchEngines}");
				Debug.WriteLine($"{Program.Config.Engines}");
			}

			private static void LvEngines_KeyPress(KeyEventEventArgs eventArgs)
			{
				Debug.WriteLine($"{eventArgs.KeyEvent} {Program.Config.Engines} {Lv_Engines.SelectedItem}");
			}

			private static void LvEngines_OpenSelectedItem(ListViewItemEventArgs eventArgs)
			{
				Debug.WriteLine($"osi {eventArgs.Item} {eventArgs.Value}");
			}

			private static void BtnClear_KeyPress(KeyEventEventArgs args)
			{
				switch (args.KeyEvent) { }
			}

			private static void BtnClear_Clicked()
			{
				Tf_Input.DeleteAll();
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

				Cb_Engines.OpenSelectedItem    += LvEngines_OpenSelectedItem;
				Cb_Engines.KeyPress            += LvEngines_KeyPress;
				Cb_Engines.SelectedItemChanged += LvEngines_SelectedItemChanged;

				Win.Add(Lbl_Input, Tf_Input, Btn_Ok, Lbl_InputOk, /*Cb_Engines,*/ Tf_Query,
				        Lbl_Query, Btn_Clear, Lv_Results, Cb_Engines
				);
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