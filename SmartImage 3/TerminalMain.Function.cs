using System.Data;
using System.Diagnostics;
using System.Numerics;
using Kantan.Console;
using Kantan.Net.Utilities;
using Novus.FileTypes;
using Novus.OS;
using NStack;
using SmartImage.App;
using SmartImage.Lib;
using SmartImage.Lib.Engines;
using SmartImage.Lib.Engines.Search;
using SmartImage.Shell;
using Terminal.Gui;

namespace SmartImage;

public sealed partial class TerminalMain
{
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

		var b1 = UI.CreateLinkButton(d, "Repo", R2.Repo_Url);
		var b2 = UI.CreateLinkButton(d, "Wiki", R2.Wiki_Url);
		var b3 = UI.CreateLinkButton(d, "Ok", null, () => Application.RequestStop());

		d.AddButton(b1);
		d.AddButton(b2);
		d.AddButton(b3);

		Application.Run(d);
	}

	/// <summary>
	/// <see cref="Btn_Config"/>
	/// </summary>
	private void ConfigDialog()
	{
		var dlCfg = new Dialog("Configuration")
		{
			Text     = ustring.Empty,
			AutoSize = false,
			Width    = UI.Dim_80_Pct,
			Height   = UI.Dim_80_Pct,
		};

		var btnRefresh = new Button("Refresh") { };
		var btnSave    = new Button("Save") { };
		var btnOk      = new Button("Ok") { };

		const int WIDTH  = 15;
		const int HEIGHT = 17;

		/*============================================================================*\
			Engines
		\*============================================================================*/

		Label lbSearchEngines = new(R1.S_SearchEngines)
		{
			X           = 0,
			Y           = 0,
			AutoSize    = true,
			ColorScheme = UI.Cs_Lbl1
		};

		ListView lvSearchEngines = new(UI.EngineOptions)
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

		ListView lvPriorityEngines = new(UI.EngineOptions)
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

		lbConfig.Clicked += () =>
		{
			FileSystem.ExploreFile(cfgInfo.FullName);
		};

		DataTable dtConfig = Config.ToTable();

		var tvConfig = new TableView(dtConfig)
		{
			AutoSize = true,
			Y        = Pos.Bottom(lbConfig),
			X        = Pos.Right(lvPriorityEngines) + 1,
			Width    = Dim.Fill(WIDTH),
			Height   = 9,
		};

		void ReloadDialog()
		{
			tvConfig.Table = Config.ToTable();
			tvConfig.SetNeedsDisplay();
			dlCfg.SetNeedsDisplay();
		}

		lvSearchEngines.OpenSelectedItem += args1 =>
		{
			SearchEngineOptions e = Config.SearchEngines;
			UI.OnEngineSelected(args1, ref e, lvSearchEngines);
			Config.SearchEngines = e;
			ReloadDialog();
		};

		lvPriorityEngines.OpenSelectedItem += args1 =>
		{
			SearchEngineOptions e = Config.PriorityEngines;
			UI.OnEngineSelected(args1, ref e, lvPriorityEngines);
			Config.PriorityEngines = e;
			ReloadDialog();
		};

		lvSearchEngines.FromEnum(Config.SearchEngines);
		lvPriorityEngines.FromEnum(Config.PriorityEngines);

		/*============================================================================*\
			Checkboxes
		\*============================================================================*/

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
			m_autoSearch = b;
			ReloadDialog();
		};

		cbContextMenu.Checked = Integration.IsContextMenuAdded;
		cbOnTop.Checked       = Config.OnTop;

		/*============================================================================*\
			Eh username/password
		\*============================================================================*/

		Label lbEhUsername = new(R1.S_EhUsername)
		{
			X           = Pos.X(cbContextMenu),
			Y           = Pos.Bottom(cbContextMenu) + 1,
			CanFocus    = false,
			ColorScheme = UI.Cs_Lbl1
		};

		TextField tfEhUsername = new()
		{
			X      = Pos.Right(lbEhUsername) + 1,
			Y      = Pos.Y(lbEhUsername),
			Width  = WIDTH * 2,
			Height = 1,
		};

		tfEhUsername.TextChanging += args =>
		{
			Config.EhUsername = args.NewText.ToString();
			ReloadDialog();
		};

		Label lbEhPassword = new(R1.S_EhPassword)
		{
			X           = Pos.X(lbEhUsername),
			Y           = Pos.Bottom(lbEhUsername),
			CanFocus    = false,
			ColorScheme = UI.Cs_Lbl1
		};

		TextField tfEhPassword = new()
		{
			X      = Pos.Right(lbEhPassword) + 1,
			Y      = Pos.Y(lbEhPassword),
			Width  = WIDTH * 2,
			Height = 1,
		};

		tfEhPassword.TextChanging += args =>
		{
			Config.EhPassword = args.NewText.ToString();
			ReloadDialog();
		};

		/*============================================================================*\
		    Register
		\*============================================================================*/

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
		          lbHelp, cbAutoSearch, lbEhUsername, tfEhUsername, lbEhPassword, tfEhPassword);

		dlCfg.AddButton(btnRefresh);
		dlCfg.AddButton(btnOk);
		dlCfg.AddButton(btnSave);

		Application.Run(dlCfg);

		Tf_Input.SetFocus();
		Tf_Input.EnsureFocus();
	}

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

	/// <summary>
	/// <see cref="Btn_Restart"/>
	/// </summary>
	private void Restart_Clicked()
	{
		if (!Client.IsComplete) {
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

		f(Query.Uni.Value.ToString());
	}

	private static void Clear_Clicked()
	{
		Tf_Input.DeleteAll();
		UI.SetLabelStatus(Lbl_InputOk, null);
		Lbl_InputOk.SetNeedsDisplay();
		Lbl_InputInfo.Text  = ustring.Empty;
		Lbl_InputInfo2.Text = ustring.Empty;
		Lbl_Status2.Text    = ustring.Empty;
	}

	private void Cancel_Clicked()
	{
		m_token.Cancel();
		Lbl_Status2.Text = Resources.Inf_Cancel;
		Lbl_Status2.SetNeedsDisplay();
		Btn_Restart.Enabled = true;
		Application.MainLoop.RemoveIdle(m_runIdleTok);
		Tv_Results.SetFocus();
	}

	private void Clear()
	{
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
	}

	private async void Input_TextChanging(TextChangingEventArgs tc)
	{
		var text = tc.NewText;

		Debug.WriteLine($"testing {text}", nameof(Input_TextChanging));

		if (SearchQuery.IsValidSourceType(text.ToString())) {
			var ok = await SetQuery(text);
			Btn_Run.Enabled = ok;
		}
	}
}