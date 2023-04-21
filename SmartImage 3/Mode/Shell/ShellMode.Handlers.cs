// Read Stanton SmartImage ShellMain.Handlers.cs
// 2023-01-13 @ 11:29 PM

using System.Diagnostics;
using Kantan.Net.Utilities;
using Microsoft.VisualBasic.FileIO;
using Novus.OS;
using NStack;
using SmartImage.App;
using SmartImage.Lib;
using SmartImage.Mode.Shell.Assets;
using Terminal.Gui;
using Clipboard = Novus.Win32.Clipboard;
using Microsoft.VisualBasic.FileIO;
using FileSystem = Novus.OS.FileSystem;

namespace SmartImage.Mode.Shell;

public sealed partial class ShellMode
{
	/// <summary>
	/// <see cref="Tv_Results"/>
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

		// Debug.WriteLine($"testing {text}", nameof(Input_TextChanging));

		if (SearchQuery.IsValidSourceType(text.ToString())) {
			var ok = await SetQuery(text);
			Btn_Run.Enabled = ok;
		}
	}

	/// <summary>
	/// <see cref="Btn_Restart"/>
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

		m_clipboard.Clear();
		m_results.Clear();

		Status = true;

		Btn_Restart.Enabled = false;
		Btn_Cancel.Enabled  = false;
		Btn_Run.Enabled     = true;
		Btn_Delete.Enabled     = false;

		m_token.Dispose();
		m_token = new();

		Tf_Input.SetFocus();
		Tf_Input.EnsureFocus();
	}

	/// <summary>
	/// <see cref="Btn_Run"/>
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

		await RunMain();
	}

	/// <summary>
	/// <see cref="Btn_Browse"/>
	/// </summary>
	private void Browse_Clicked()
	{
		Integration.KeepOnTop(false);
		var f = Integration.OpenFile();

		if (!string.IsNullOrWhiteSpace(f)) {
			Tf_Input.DeleteAll();
			Debug.WriteLine($"Picked file: {f}", nameof(Browse_Clicked));

			SetInputText(f);
			Btn_Run.SetFocus();

		}

		Integration.KeepOnTop(Client.Config.OnTop);
	}

	/// <summary>
	/// <see cref="Lbl_InputInfo"/>
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

		Tf_Input.SetFocus();
		// Btn_Delete.Enabled = false;
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

	private void On_Delete()
	{
		Clipboard.Close();
		// Restart_Clicked(true);

		var file = Tf_Input.Text.ToString();

		if (!string.IsNullOrWhiteSpace(file)) {
			Query.Dispose();
			Debug.WriteLine($"{IsQueryReady()}");

			Microsoft.VisualBasic.FileIO.FileSystem.DeleteFile(file, UIOption.OnlyErrorDialogs,
			                                                   RecycleOption.SendToRecycleBin);
			Debug.WriteLine($"deleted {file}");
			Clear();
		}

	}
}