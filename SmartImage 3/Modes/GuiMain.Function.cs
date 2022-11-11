using System.Data;
using System.Diagnostics;
using Kantan.Console;
using Kantan.Net.Utilities;
using Novus.OS;
using NStack;
using SmartImage.App;
using SmartImage.Lib;
using SmartImage.Lib.Engines;
using Terminal.Gui;

namespace SmartImage.Modes;

public sealed partial class GuiMain
{
	#region Control functions

	private void OnClear()
	{
		Tf_Input.DeleteAll();
		Lbl_InputOk.Text = UI.NA;
		Lbl_InputOk.SetNeedsDisplay();
		Lbl_InputInfo.Text  = ustring.Empty;
		Lbl_InputInfo2.Text = ustring.Empty;
	}

	private void AboutDialog()
	{
		var d = new Dialog()
		{
			Text = $"{R2.Name} {Integration.Version} by {R2.Author}\n" +
			       $"Current directory: {Integration.CurrentAppFolder}",

			Title    = R2.Name,
			AutoSize = true,
			Width    = UI.Dim_30_Pct,
			Height   = UI.Dim_30_Pct,
		};

		void RefreshDialog()
		{
			d.SetNeedsDisplay();
		}

		var b1 = new Button()
		{
			AutoSize = true,
			Text     = $"Repo",
		};

		b1.Clicked += () =>
		{
			HttpUtilities.TryOpenUrl(R2.Repo_Url);
			RefreshDialog();
		};

		var b2 = new Button()
		{
			AutoSize = true,
			Text     = $"Wiki",
		};

		b2.Clicked += () =>
		{
			HttpUtilities.TryOpenUrl(R2.Wiki_Url);
			RefreshDialog();
		};

		var b3 = new Button()
		{
			Text     = $"Ok",
			AutoSize = true,
		};

		b3.Clicked += () =>
		{
			Application.RequestStop();
		};

		d.AddButton(b1);
		d.AddButton(b2);
		d.AddButton(b3);

		Application.Run(d);
	}

	private void OnConfigDialog()
	{
		var dlCfg = new Dialog("Configuration")
		{
			Text     = ustring.Empty,
			AutoSize = false,
			Width    = Dim.Percent(80),
			Height   = Dim.Percent(80),
		};

		var btnRefresh = new Button("Refresh") { };
		var btnSave    = new Button("Save") { };
		var btnOk      = new Button("Ok") { };

		DataTable dtConfig = Config.ToTable();

		const int WIDTH  = 15;
		const int HEIGHT = 17;

		Label lbSearchEngines = new(R1.S_SearchEngines)
		{
			X           = 0,
			Y           = 0,
			AutoSize    = true,
			ColorScheme = UI.Cs_Lbl1
		};

		ListView lvSearchEngines = new(ConsoleUtil.EngineOptions)
		{
			AllowsMultipleSelection = true,
			AllowsMarking           = true,
			AutoSize                = true,
			Width                   = WIDTH,
			Height                  = HEIGHT,

			Y = Pos.Bottom(lbSearchEngines)
		};

		Label lbPriorityEngines = new(R1.S_PriorityEngines)
		{
			X           = Pos.Right(lbSearchEngines) + 1,
			Y           = 0,
			AutoSize    = true,
			ColorScheme = UI.Cs_Lbl1
		};

		ListView lvPriorityEngines = new(ConsoleUtil.EngineOptions)
		{
			AllowsMultipleSelection = true,
			AllowsMarking           = true,
			AutoSize                = true,
			Width                   = WIDTH,
			Height                  = HEIGHT,

			Y = Pos.Bottom(lbPriorityEngines),
			X = Pos.Right(lvSearchEngines) + 1
		};

		var cfgInfo = new FileInfo(SearchConfig.Configuration.FilePath);

		Label lbConfig = new($"Config")
		{
			X           = Pos.Right(lbPriorityEngines) + 1,
			Y           = 0,
			AutoSize    = true,
			ColorScheme = UI.Cs_Lbl1
			// Height = 10,
		};

		var tvConfig = new TableView(dtConfig)
		{
			AutoSize = true,
			Y        = Pos.Bottom(lbConfig),
			X        = Pos.Right(lvPriorityEngines) + 1,
			Width    = Dim.Fill(WIDTH),
			Height   = 7,
		};

		CheckBox cbContextMenu = new(R2.Int_ContextMenu)
		{
			X           = Pos.X(tvConfig),
			Y           = Pos.Bottom(tvConfig) + 1,
			Width       = WIDTH,
			Height      = 1,
			ColorScheme = UI.Cs_Btn3
		};

		cbContextMenu.Toggled += b =>
		{
			Integration.HandleContextMenu(!b);
		};

		Label lbHelp = new($"{UI.Line} Arrow keys or mouse :: select option\n" +
		                   $"{UI.Line} Space bar or click :: toggle mark option\n" +
		                   $"{UI.Line} Enter :: save option")
		{
			AutoSize = true,

			X = 0,
			Y = Pos.Bottom(lvSearchEngines) + 1
		};

		CheckBox cbOnTop = new(R1.S_OnTop)
		{
			X           = Pos.Right(cbContextMenu) + 1,
			Y           = Pos.Y(cbContextMenu),
			AutoSize    = true,
			ColorScheme = UI.Cs_Btn3,
			Height      = 1,
		};

		cbOnTop.Toggled += b =>
		{
			Integration.KeepOnTop(!b);
			Config.OnTop = Integration.IsOnTop;
			ReloadDialog();
		};

		CheckBox cbAutoSearch = new(R1.S_AutoSearch)
		{
			X = Pos.Right(cbOnTop) + 1,
			Y = Pos.Y(cbOnTop),
			// Width  = WIDTH,
			Height      = 1,
			AutoSize    = true,
			ColorScheme = UI.Cs_Btn3

		};

		cbAutoSearch.Toggled += b =>
		{
			ReloadDialog();
		};

		lbConfig.Clicked += () =>
		{
			FileSystem.ExploreFile(cfgInfo.FullName);
		};

		// If only properties could be used as ref/pointers...

		lvSearchEngines.OpenSelectedItem += args1 =>
		{
			SearchEngineOptions e = Config.SearchEngines;
			OnEngineSelected(args1, ref e, lvSearchEngines);
			Config.SearchEngines = e;
			ReloadDialog();
		};

		lvPriorityEngines.OpenSelectedItem += args1 =>
		{
			SearchEngineOptions e = Config.PriorityEngines;
			OnEngineSelected(args1, ref e, lvPriorityEngines);
			Config.PriorityEngines = e;
			ReloadDialog();
		};

		void ReloadDialog()
		{
			tvConfig.Table = Config.ToTable();
			tvConfig.SetNeedsDisplay();
			dlCfg.SetNeedsDisplay();
		}

		lvSearchEngines.FromEnum(Config.SearchEngines);
		lvPriorityEngines.FromEnum(Config.PriorityEngines);
		cbContextMenu.Checked = Integration.IsContextMenuAdded;
		cbOnTop.Checked       = Config.OnTop;

		btnRefresh.Clicked += ReloadDialog;

		btnOk.Clicked += () =>
		{
			Application.RequestStop();
		};

		btnSave.Clicked += () =>
		{
			Config.Save();
			ReloadDialog();
		};

		dlCfg.Add(tvConfig, lvSearchEngines, lvPriorityEngines,
		          cbContextMenu, cbOnTop, lbConfig, lbSearchEngines, lbPriorityEngines,
		          lbHelp, cbAutoSearch);

		dlCfg.AddButton(btnRefresh);
		dlCfg.AddButton(btnOk);
		dlCfg.AddButton(btnSave);

		Application.Run(dlCfg);

		Tf_Input.SetFocus();
		Tf_Input.EnsureFocus();
	}

	private void ClearControls()
	{
		Tf_Input.DeleteAll();
		Tf_Input.ClearHistoryChanges();

		Query = SearchQuery.Null;

		Lbl_InputOk.Text = UI.NA;
		Lbl_InputOk.SetNeedsDisplay();

		Dt_Results.Clear();

		IsReady.Reset();
		ResultCount         = 0;
		Pbr_Status.Fraction = 0;

		Lbl_InputInfo.Text   = ustring.Empty;
		Lbl_QueryUpload.Text = ustring.Empty;
		Lbl_InputInfo2.Text  = ustring.Empty;
		Lbl_Status.Text      = ustring.Empty;

		Tv_Results.SetNeedsDisplay();
		Tf_Input.SetFocus();
		Tf_Input.EnsureFocus();
		Btn_Cancel.Enabled = false;
	}

	private void OnRestart()
	{
		if (!Client.IsComplete) {
			return;
		}

		ClearControls();

		Tv_Results.RowOffset    = 0;
		Tv_Results.ColumnOffset = 0;
		Dt_Results.Clear();
		Tv_Results.Visible = false;

		m_clipboard.Clear();

		Status              = ProgramStatus.Restart;
		Btn_Restart.Enabled = false;
		Btn_Cancel.Enabled  = false;
		Btn_Run.Enabled     = true;

		Token.Dispose();
		Token = new();

		Tf_Input.SetFocus();
		Tf_Input.EnsureFocus();
	}

	private void OnCellActivated(TableView.CellActivatedEventArgs args)
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
			Debug.WriteLine($"{e.Message}", nameof(OnCellActivated));
		}
	}

	private static void OnEngineSelected(ListViewItemEventArgs args, ref SearchEngineOptions e, ListView lv)
	{
		var val = (SearchEngineOptions) args.Value;

		var isMarked = lv.Source.IsMarked(args.Item);

		bool b = val == SearchEngineOptions.None;

		if (isMarked) {
			if (b) {
				e = val;

				for (int i = 1; i < lv.Source.Length; i++) {
					lv.Source.SetMark(i, false);
				}
			}
			else {
				e |= val;
			}
		}
		else {
			e &= ~val;
		}

		if (!b) {
			lv.Source.SetMark(0, false);
		}

		lv.FromEnum(e);

		ret:
		lv.SetNeedsDisplay();
		Debug.WriteLine($"{val} {args.Item} -> {e} {isMarked}", nameof(OnEngineSelected));
	}

	private async void OnRun()
	{
		Btn_Run.Enabled = false;
		// Btn_Cancel.Enabled = true;
		var text = Tf_Input.Text;

		Debug.WriteLine($"Input: {text}", nameof(OnRun));

		var ok = await SetQuery(text);
		Btn_Cancel.Enabled = ok;
		Tv_Results.Visible = ok;

		if (!ok) {
			return;
		}

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

		var run = RunSearchAsync(null);
		await run;

		sw.Stop();
		Application.MainLoop.RemoveIdle(m_runIdleTok);
	}

	private void OnCancel()
	{
		Token.Cancel();
		Lbl_InputInfo2.Text = Resources.Inf_Cancel;
		Lbl_InputInfo2.SetNeedsDisplay();
		Btn_Restart.Enabled = true;
		Application.MainLoop.RemoveIdle(m_runIdleTok);
		Tv_Results.SetFocus();
	}

	#endregion
}