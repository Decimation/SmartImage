// Read S SmartImage ShellMode.Handlers.cs
// 2023-02-14 @ 12:13 AM

using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics;
using Kantan.Net.Utilities;
using Kantan.Text;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic.FileIO;
using Novus.OS;
using NStack;
using SmartImage.App;
using SmartImage.Lib;
using SmartImage.Mode.Shell.Assets;
using Terminal.Gui;
using Clipboard = Novus.Win32.Clipboard;
using Microsoft.VisualBasic.FileIO;
using Novus.Win32.Structures.User32;
using SmartImage.Lib.Utilities;
using FileSystem = Novus.OS.FileSystem;

namespace SmartImage.Mode.Shell;

public sealed partial class ShellMode
{
	/// <summary>
	///     <see cref="Tv_Results" />
	/// </summary>
	private void Result_CellActivated(TableView.CellActivatedEventArgs args)
	{
		if (args.Table is not { }) {
			return;
		}

		try {
			var cell = args.Table.Rows[args.Row][args.Col];

			if (cell is Url { } u) {
				HttpUtilities.TryOpenUrl(u);
			}

		}
		catch (Exception e) {
			Debug.WriteLine($"{e.Message}", nameof(Result_CellActivated));
		}
	}

	private async void Input_TextChanging(TextChangingEventArgs tc)
	{
		var text = tc.NewText;

		Debug.WriteLine($"testing {text}", nameof(Input_TextChanging));

		Application.MainLoop.Invoke(() => Task.Delay(TimeSpan.FromSeconds(1)));

		var sourceType = SearchQuery.IsValidSourceType(text.ToString());

		if (sourceType) {
			var ok = await SetQuery(text);
			Btn_Run.Enabled = ok;
			Debug.WriteLine($"{nameof(Input_TextChanging)} :: ok");
		}
	}

	/// <summary>
	///     <see cref="Btn_Restart" />
	/// </summary>
	private void Restart_Clicked(bool force = false)
	{
		if (!Client.IsComplete && !force) {
			return;
		}

		Clear();

		Tv_Results.RowOffset    = 0;
		Tv_Results.ColumnOffset = 0;
		Dt_Results.Clear();
		Tv_Results.Visible = false;

		// m_clipboard.RemoveRange(1, m_clipboard.Count - 1);

		m_clipboard.Clear();
		m_results.Clear();

		Status = true;

		Btn_Restart.Enabled = false;
		Btn_Cancel.Enabled  = false;
		Btn_Run.Enabled     = true;
		Btn_Delete.Enabled  = false;

		m_token.Dispose();
		m_token = new();

		Tf_Input.SetFocus();
		Tf_Input.EnsureFocus();

	}

	/// <summary>
	///     <see cref="Btn_Run" />
	/// </summary>
	private async void Run_Clicked()
	{
		Btn_Run.Enabled = false;
		// Btn_Delete.Enabled = false;

		var text = Tf_Input.Text;

		Debug.WriteLine($"Input: {text}", nameof(Run_Clicked));

		var ok = await SetQuery(text);

		Btn_Cancel.Enabled = ok;
		Tv_Results.Visible = ok;

		if (!ok) {
			return;
		}

		await RunMainAsync();
	}

	private void Queue_Dialog()
	{
		var d = new Dialog()
		{
			AutoSize = false,
			Width    = Dim.Percent(60),
			Height   = Dim.Percent(50),
			// Height   = UI.Dim_80_Pct,
		};

		var cpy = Queue.ToList();

		var tf = new TextField()
		{

			Width  = Dim.Fill(),
			Height = 3,
		};

		var lv = new ListView(cpy)
		{
			Width  = Dim.Fill(),
			Height = Dim.Fill(),
			Y      = Pos.Bottom(tf)
		};

		var btnRm = new Button("Remove")
			{ };

		lv.KeyPress += args =>
		{
			if (args.KeyEvent.Key == Key.DeleteChar) {
				Debug.WriteLine($"{args}!!!");

			}
		};

		btnRm.Clicked += () =>
		{
			var cpy2 = lv.Source.ToList();

			if (lv.SelectedItem < cpy2.Count && lv.SelectedItem >= 0) {
				var i = (string) cpy2[lv.SelectedItem];
				Debug.WriteLine($"{i}");
				cpy.Remove(i);
				// Queue.Clear();
				Queue = new ConcurrentQueue<string>(cpy);
				lv.SetFocus();

			}
		};

		var btnRmAll = new Button("Clear")
			{ };

		btnRmAll.Clicked += () =>
		{
			lv.Source = new ListWrapper(Array.Empty<string>());
			Queue.Clear();
			lv.SetFocus();
		};

		tf.TextChanging += a =>
		{

			var s = a.NewText.ToString();

			if (SearchQuery.IsValidSourceType(s)) {
				Queue.Enqueue(s);
				lv.Source = new ListWrapper(Queue.ToList());
				tf.DeleteAll();
				tf.Text  = ustring.Empty;
				a.Cancel = true;
				tf.SetFocus();
				tf.SetNeedsDisplay();
			}
		};

		d.Add(tf, lv);
		d.AddButton(btnRm);
		d.AddButton(btnRmAll);

		Application.Run(d);
	}

	private void Queue_Checked(bool b)
	{
		QueueMode = !b;

		Btn_Queue.Enabled = QueueMode;
		Btn_Next.Enabled  = QueueMode;
	}

	/// <summary>
	///     <see cref="Btn_Browse" />
	/// </summary>
	private void Browse_Clicked()
	{
		Integration.KeepOnTop(false);

		OFN flags = 0x0;

		if (QueueMode) {
			flags |= OFN.OFN_ALLOWMULTISELECT;
		}

		flags |= OFN.OFN_EXPLORER | OFN.OFN_FILEMUSTEXIST | OFN.OFN_LONGNAMES | OFN.OFN_PATHMUSTEXIST;
		string[]? files;

		try {
			files = Integration.OpenFile(flags);
		}
		catch (Exception e) {
			Debug.WriteLine($"{e.Message}");
			return;
		}

		if (QueueMode) {
			foreach (string fs in files) {
				Queue.Enqueue(fs);
			}

			NextQueue();
		}

		else {
			var f = files.FirstOrDefault(); //todo

			if (!string.IsNullOrWhiteSpace(f)) {
				Tf_Input.DeleteAll();
				Debug.WriteLine($"Picked file: {f}", nameof(Browse_Clicked));

				SetInputText(f);
				Btn_Run.SetFocus();

			}

		}

		Integration.KeepOnTop(Client.Config.OnTop);
	}

	/// <summary>
	///     <see cref="Lbl_InputInfo" />
	/// </summary>
	private void InputInfo_Clicked()
	{
		if (!IsQueryReady()) {
			return;
		}

		Func<string, bool> f = _ => false;

		if (Query.Uni.IsFile) {
			f = FileSystem.ExploreFile;
		}
		else if (Query.Uni.IsUri) {
			f = HttpUtilities.TryOpenUrl;
		}

		var v = f(Query.Uni.Value.ToString()!);
	}

	private static void Clear_Clicked()
	{
		Tf_Input.ReadOnly = false;
		Tf_Input.DeleteAll();
		UI.SetLabelStatus(Lbl_InputOk, null);
		Lbl_InputOk.SetNeedsDisplay();
		Lbl_InputInfo.Text  = ustring.Empty;
		Lbl_InputInfo2.Text = ustring.Empty;
		Lbl_Status2.Text    = ustring.Empty;
		Btn_Run.Enabled     = true;
		// Btn_Run.Enabled     = false;
		Tf_Input.SetFocus();
		Btn_Delete.Enabled = false;

	}

	private void Cancel_Clicked()
	{
		m_token.Cancel();
		Lbl_Status2.Text = R2.Inf_Cancel;
		Lbl_Status2.SetNeedsDisplay();
		Btn_Restart.Enabled = true;
		Application.MainLoop.RemoveIdle(m_runIdleTok);
		Tv_Results.SetFocus();

	}

	private void Delete_Clicked()
	{
		try {
			Clipboard.Close();
			// Restart_Clicked(true);

			var file = Tf_Input.Text.ToString();

			if (!string.IsNullOrWhiteSpace(file)) {
				file = file.CleanString();
				Query.Dispose();
				// Restart_Clicked(true);
				Debug.WriteLine($"{IsQueryReady()}");

				Microsoft.VisualBasic.FileIO.FileSystem.DeleteFile(file, UIOption.OnlyErrorDialogs,
				                                                   RecycleOption.SendToRecycleBin);
				Debug.WriteLine($"deleted {file}");
				// Clear();
				Cancel_Clicked();
				Clear_Clicked();
				// Btn_Delete.Enabled = false;
			}

		}
		catch (Exception e) {

			File.WriteAllText("crash.log", $"{e.Message} {e.Source} {e.StackTrace}");

		}
		finally {

			// Restart_Clicked(true);
		}

	}

	private void Next_Clicked()
	{
		Restart_Clicked(true);
		NextQueue();
	}

	private void NextQueue()
	{
		var tryDequeue = Queue.TryDequeue(out var n);

		if (tryDequeue) {
			// SetQuery(n);

			SetInputText(n);
		}
	}
}