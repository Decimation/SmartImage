using System.Diagnostics;
using NStack;
using SmartImage.Lib;
using SmartImage.Mode.Shell.Assets;
using Terminal.Gui;

namespace SmartImage.Mode.Shell;

public sealed partial class ShellMode
{
	private async Task RunMain()
	{
		Pbr_Status.BidirectionalMarquee = false;
		Pbr_Status.ProgressBarStyle     = ProgressBarStyle.Continuous;
		Pbr_Status.Fraction             = 0;
		Pbr_Status.SetNeedsDisplay();

		var sw = Stopwatch.StartNew();

		m_runIdleTok = Application.MainLoop.AddIdle(() =>
		{
			Lbl_Status.Text = $"{ResultCount} | {sw.Elapsed.TotalSeconds:F3} sec";
			return true;
		});

		var run = RunSearchAsync();
		await run;

		sw.Stop();
		Application.MainLoop.RemoveIdle(m_runIdleTok);
	}

	private void Clear()
	{
		Tf_Input.ReadOnly = false;

		Tf_Input.DeleteAll();
		Tf_Input.ClearHistoryChanges();

		UI.SetLabelStatus(Lbl_InputOk, null);

		Lbl_InputOk.SetNeedsDisplay();

		Dt_Results.Clear();

		Query = SearchQuery.Null;
		IsReady.Reset();
		ResultCount = 0;

		Pbr_Status.Fraction = 0;

		Lbl_InputInfo.Text   = ustring.Empty;
		Lbl_QueryUpload.Text = ustring.Empty;
		Lbl_InputInfo2.Text  = ustring.Empty;
		Lbl_Status.Text      = ustring.Empty;
		Lbl_Status2.Text     = ustring.Empty;

		Tv_Results.SetNeedsDisplay();
		Tf_Input.SetFocus();
		Tf_Input.EnsureFocus();
		Btn_Cancel.Enabled = false;

		m_queue.Clear();

	}
}