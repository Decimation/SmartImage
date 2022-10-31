﻿global using Url = Flurl.Url;
using NStack;
using SmartImage.Lib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Dynamic;
using System.Linq;
using System.Media;
using System.Text;
using System.Threading.Tasks;
using AngleSharp.Dom;
using Kantan.Net.Utilities;
using Terminal.Gui;
using Rune = System.Rune;
using System.Reflection;
using Flurl.Http;
using Kantan.Utilities;
using Novus.FileTypes;
using Novus.Utilities;
using Novus.Win32;
using SmartImage.App;
using static Novus.Win32.SysCommand;
using Window = Terminal.Gui.Window;
using System.Xml.Linq;
using Kantan.Console;
using Kantan.Text;
using Novus.OS;
using Attribute = Terminal.Gui.Attribute;
using Color = Terminal.Gui.Color;
using SmartImage.UI;

// ReSharper disable InconsistentNaming

namespace SmartImage.Modes;

public sealed class GuiMode : BaseProgramMode
{
	// NOTE: DO NOT REARRANGE FIELD ORDER

	#region Controls

	private static readonly Toplevel Top = Application.Top;

	private static readonly Window Win = new(Resources.Name)
	{
		X           = 0,
		Y           = 1,
		Width       = Dim.Fill(),
		Height      = Dim.Fill(),
		ColorScheme = Styles.Cs_Win,
	};

	private static readonly MenuBar Mb_Menu = new()
	{
		CanFocus = false,
	};

	private static readonly Label Lbl_Input = new("Input:")
	{
		X           = 1,
		Y           = 0,
		ColorScheme = Styles.Cs_Elem2
	};

	private static readonly TextField Tf_Input = new(ustring.Empty)
	{
		X           = Pos.Right(Lbl_Input),
		Y           = Pos.Top(Lbl_Input),
		Width       = 50,
		ColorScheme = Styles.Cs_Win2,
		AutoSize    = false,
		// AutoSize = true,
	};

	private static readonly Label Lbl_InputOk = new(Values.NA)
	{
		X = Pos.Right(Tf_Input) + 1,
		Y = Pos.Y(Tf_Input),
		// ColorScheme = Styles.CS_Elem4
	};

	private static readonly Button Btn_Run = new("Run")
	{
		X           = Pos.Right(Lbl_InputOk) + 1,
		Y           = Pos.Y(Tf_Input),
		ColorScheme = Styles.Cs_Btn1x,

	};

	private static readonly Button Btn_Clear = new("Clear")
	{
		X               = Pos.Right(Btn_Run),
		Y               = Pos.Y(Btn_Run),
		HotKey          = Key.Null,
		HotKeySpecifier = default,
		ColorScheme     = Styles.Cs_Btn1,
	};

	private static readonly Button Btn_Restart = new("Restart")
	{
		X           = Pos.Right(Btn_Clear),
		Y           = Pos.Y(Btn_Clear),
		Enabled     = false,
		ColorScheme = Styles.Cs_Btn1,

	};

	private static readonly Button Btn_Cancel = new("Cancel")
	{
		X           = Pos.Right(Btn_Restart),
		Y           = Pos.Y(Btn_Restart),
		Enabled     = false,
		ColorScheme = Styles.Cs_Btn2,

	};

	private static readonly Button Btn_Config = new("Config")
	{
		X           = Pos.Right(Btn_Cancel),
		Y           = Pos.Y(Btn_Cancel),
		Enabled     = true,
		ColorScheme = Styles.Cs_Btn2,
	};

	private static readonly Label Lbl_InputInfo = new()
	{
		X      = Pos.Bottom(Tf_Input),
		Y      = 1,
		Width  = 15,
		Height = Dim.Height(Tf_Input)
	};

	private static readonly Label Lbl_QueryUpload = new()
	{
		X      = Pos.Right(Lbl_InputInfo) + 1,
		Y      = 1,
		Width  = 15,
		Height = Dim.Height(Lbl_InputInfo)
	};

	private static readonly Label Lbl_InputInfo2 = new()
	{
		X      = Pos.Right(Lbl_QueryUpload) + 1,
		Y      = Pos.Y(Lbl_QueryUpload),
		Width  = 15,
		Height = Dim.Height(Lbl_QueryUpload)
	};

	private static readonly DataTable Dt_Results = new()
		{ };

	private static readonly TableView Tv_Results = new()
	{
		X             = Pos.X(Lbl_Input),
		Y             = Pos.Bottom(Lbl_InputInfo),
		Width         = Dim.Fill(),
		Height        = Dim.Fill(),
		AutoSize      = true,
		FullRowSelect = true,

	};

	private static readonly ProgressBar Pbr_Status = new()
	{
		X                = Pos.Right(Btn_Config) + 1,
		Y                = Pos.Y(Tf_Input),
		Width            = 10,
		ProgressBarStyle = ProgressBarStyle.Continuous,
	};

	private static readonly Label Lbl_Status = new()
	{
		X      = Pos.Right(Pbr_Status) + 1,
		Y      = Pos.Y(Pbr_Status),
		Width  = 15,
		Height = Dim.Height(Lbl_InputInfo)
	};

	#endregion

	private object m_cbCallbackTok;

	private Func<bool>? m_runIdleTok;

	private readonly List<ustring> m_clipboard;

	private static readonly TimeSpan TimeoutTimeSpan = TimeSpan.FromSeconds(1.5);

	#region Overrides of ProgramMode

	public GuiMode(string[] args) : base(args, SearchQuery.Null)
	{
		// Application.Init();

		ProcessArgs();
		ApplyConfig();

		/*
		 * Check if clipboard contains valid query input
		 */

		m_cbCallbackTok = Application.MainLoop.AddTimeout(TimeoutTimeSpan, ClipboardCallback);

		m_clipboard = new List<ustring>();

		/*m_tok = Application.MainLoop.AddIdle(() =>
		{
			return ClipboardCallback(null);
		});*/

		Mb_Menu.Menus = new MenuBarItem[]
		{
			// new("_Config", null, ConfigDialog),
		};

		Top.Add(Mb_Menu);

		var col = new DataColumn[]
		{
			new("Engine", typeof(string)),

			new(nameof(SearchResultItem.Url), typeof(Flurl.Url)),
			new(nameof(SearchResultItem.Score), typeof(int)),
			new(nameof(SearchResultItem.Similarity), typeof(double)),
			new(nameof(SearchResultItem.Artist), typeof(string)),
			new(nameof(SearchResultItem.Description), typeof(string)),
			new(nameof(SearchResultItem.Source), typeof(string)),
			new(nameof(SearchResultItem.Title), typeof(string)),
			new(nameof(SearchResultItem.Site), typeof(string)),
			new(nameof(SearchResultItem.Width), typeof(double)),
			new(nameof(SearchResultItem.Height), typeof(double)),
			new(nameof(SearchResultItem.Metadata), typeof(object)),
		};

		Dt_Results.Columns.AddRange(col);

		var columnStyle = new TableView.ColumnStyle()
		{
			Alignment = TextAlignment.Left,
		};

		var columnStyles = col.ToDictionary(k => k, e => columnStyle);

		columnStyles[col[1]].MaxWidth = 50;

		Tv_Results.Style = new TableView.TableStyle()
		{
			ShowHorizontalScrollIndicators = true,
			AlwaysShowHeaders              = true,

			RowColorGetter = args =>
			{
				// var eng=args.Table.Rows[args.RowIndex]["Engine"];
				return null;
			},

			ShowHorizontalHeaderUnderline = true,
			ShowHorizontalHeaderOverline  = true,

			ColumnStyles = columnStyles,
		};

		Tv_Results.Border = Styles.Br_1;
		Tv_Results.Table  = Dt_Results;

		Tv_Results.CellActivated += OnCellActivated;
		Btn_Run.Clicked          += OnRun;
		Btn_Restart.Clicked      += OnRestart;
		Btn_Clear.Clicked        += OnClear;
		Btn_Config.Clicked       += OnConfigDialog;
		Btn_Cancel.Clicked       += OnCancel;

		Lbl_InputInfo.Clicked += () =>
		{
			if (!IsQueryReady()) {
				return;
			}

			if (Query.IsFile) {
				FileSystem.ExploreFile(Query.Value);
			}
			else if (Query.IsUrl) {
				HttpUtilities.TryOpenUrl(Query.Value);
			}

		};

		Lbl_QueryUpload.Clicked += () =>
		{
			HttpUtilities.TryOpenUrl(Query.Upload);
		};

		Win.Add(Lbl_Input, Tf_Input, Btn_Run, Lbl_InputOk,
		        Btn_Clear, Tv_Results, Pbr_Status, Lbl_InputInfo, Lbl_QueryUpload, Btn_Restart, Btn_Config,
		        Lbl_InputInfo2, Btn_Cancel, Lbl_Status
		);

		Top.Add(Win);

	}

	public override Task<object?> RunAsync(object? sender = null)
	{
		Application.Run();
		return Task.FromResult(Status == ProgramStatus.Restart ? (object) true : null);
	}

	public override void PreSearch(object? sender)
	{
		Tf_Input.SetFocus();
	}

	public override void PostSearch(object? sender, List<SearchResult> results1)
	{
		if (Client.IsComplete) {
			Btn_Run.Enabled = false;
		}
	}

	public override void OnResult(object o, SearchResult r)
	{
		Application.MainLoop.Invoke(() =>
		{
			Dt_Results.Rows.Add($"{r.Engine.Name} (Raw)", r.RawUrl, 0, null, null,
			                    r.Status.ToString(), null, null, null, null, null, null);

			for (int i = 0; i < r.Results.Count; i++) {
				SearchResultItem sri = r.Results[i];

				object? meta = sri.Metadata switch
				{
					string[] rg   => rg.QuickJoin(),
					Array rg      => rg.QuickJoin(),
					ICollection c => c.QuickJoin(),
					string s      => s,
					_             => null,
				};

				Dt_Results.Rows.Add($"{r.Engine.Name} #{i + 1}",
				                    sri.Url, sri.Score, sri.Similarity, sri.Artist, sri.Description, sri.Source,
				                    sri.Title, sri.Site, sri.Width, sri.Height, meta);
			}

			Pbr_Status.Fraction = (float) ++ResultCount / (Client.Engines.Length);
			Tv_Results.SetNeedsDisplay();
			Pbr_Status.SetNeedsDisplay();
		});

	}

	public override void OnComplete(object sender, List<SearchResult> e)
	{
		SystemSounds.Asterisk.Play();
		Btn_Restart.Enabled = true;
		Btn_Cancel.Enabled  = false;

	}

	public override void Close()
	{
		Application.Shutdown();
	}

	public override void Dispose()
	{
		base.Dispose();
	}

	protected override void ProcessArg(object? val, IEnumerator e)
	{

		if (val is string s && s == Resources.Arg_Input) {
			e.MoveNext();
			var s2 = e.Current?.ToString();

			if (SearchQuery.IsIndicatorValid(s2)) {
				SetInputText(s2);
			}
		}
	}

	#endregion

	//note: ideally some of these computations aren't necessary and can be stored as respective fields but this is to ensure program correctness

	private bool IsQueryReady()
	{
		return Query != SearchQuery.Null && Url.IsValid(Query.Upload);
	}

	private bool IsInputValidIndicator()
	{
		return SearchQuery.IsIndicatorValid(Tf_Input.Text.ToString());
	}

	private void ApplyConfig()
	{
		Integration.KeepOnTop(Config.OnTop);
	}

	internal void SetInputText(ustring s)
	{
		Tf_Input.Text = s;
	}

	private async Task<bool> SetQuery(ustring text)
	{
		SearchQuery sq;

		try {
			sq = await SearchQuery.TryCreateAsync(text.ToString());

		}
		catch (Exception e) {
			sq = SearchQuery.Null;

			Lbl_InputInfo.Text = $"Error: {e.Message}";
		}

		Lbl_InputOk.Text = Values.PRC;

		if (sq is { } && sq != SearchQuery.Null) {

			try {
				var u = await sq.UploadAsync();
				Lbl_QueryUpload.Text = u.ToString();
			}
			catch (Exception e) {
				Debug.WriteLine($"{e.Message}", nameof(SetQuery));

			}

		}
		else {
			Lbl_InputOk.Text     = Values.Err;
			Lbl_InputInfo.Text   = "Error: invalid input";
			Btn_Run.Enabled      = true;
			Lbl_QueryUpload.Text = ustring.Empty;

			return false;
		}

		Debug.WriteLine($">> {sq} {Config}", nameof(SetQuery));

		Lbl_InputOk.Text = Values.OK;

		Query = sq;
		// QueryMat = Mat.FromImageData(Query.Stream.ToByteArray()); // todo: advances stream position?
		Status = ProgramStatus.Signal;

		Lbl_InputInfo.Text = $"[{(sq.IsFile ? "File" : "Uri")}] ({sq.FileTypes.First()})";

		IsReady.Set();
		Btn_Run.Enabled = false;

		return true;
	}

	private bool ClipboardCallback(MainLoop c)
	{
		try {
			/*
			 * Don't set input if:
			 *	- Input is already semiprimed
			 *	- Clipboard history contains it already
			 */
			if (Integration.ReadClipboard(out var str) && !IsInputValidIndicator() && !m_clipboard.Contains(str)) {
				SetInputText(str);
				// Lbl_InputOk.Text   = Values.Clp;
				Lbl_InputInfo.Text = $"Clipboard data";

				m_clipboard.Add(str);
			}

			// note: wtf?
			c.RemoveTimeout(m_cbCallbackTok);
			m_cbCallbackTok = c.AddTimeout(TimeoutTimeSpan, ClipboardCallback);

			return false;
		}
		catch (Exception e) {
			Debug.WriteLine($"{e.Message}", nameof(ClipboardCallback));
		}

		return true;
	}

	#region Control functions

	private void OnClear()
	{
		Tf_Input.DeleteAll();
		Lbl_InputOk.Text = Values.NA;
		Lbl_InputOk.SetNeedsDisplay();
		Lbl_InputInfo.Text = ustring.Empty;
		Lbl_InputInfo2.Text = ustring.Empty;
	}

	private void OnConfigDialog()
	{
		var about = new Dialog("Configuration")
		{
			Text     = ustring.Empty,
			AutoSize = false,
		};

		var btnRefresh = new Button("Refresh") { };
		var btnSave    = new Button("Save") { };
		var btnOk      = new Button("Ok") { };

		DataTable dtConfig = Config.ToTable();

		const int WIDTH  = 15;
		const int HEIGHT = 20;

		ListView lvSearchEngines = new(ConsoleUtil.EngineOptions)
		{
			AllowsMultipleSelection = true,
			AllowsMarking           = true,
			AutoSize                = true,
			Width                   = WIDTH,
			Height                  = HEIGHT,
		};

		ListView lvPriorityEngines = new(ConsoleUtil.EngineOptions)
		{
			AllowsMultipleSelection = true,
			AllowsMarking           = true,
			AutoSize                = true,
			Width                   = WIDTH,
			Height                  = HEIGHT,
			X                       = Pos.Right(lvSearchEngines)
		};

		CheckBox cbContextMenu = new(R2.Int_ContextMenu)
		{
			Y      = Pos.Bottom(lvSearchEngines),
			Width  = 15,
			Height = 1,
		};

		cbContextMenu.Toggled += b =>
		{
			Integration.HandleContextMenu(!b);
		};

		CheckBox cbOnTop = new(R1.S_OnTop)
		{
			X      = Pos.Right(cbContextMenu),
			Y      = Pos.Bottom(lvPriorityEngines),
			Width  = 15,
			Height = 1,
		};

		var tvConfig = new TableView(dtConfig)
		{
			AutoSize = true,
			X        = Pos.Right(lvPriorityEngines),
			Width    = Dim.Fill(WIDTH),
			Height   = 10,
		};

		cbOnTop.Toggled += b =>
		{
			Integration.KeepOnTop(!b);
			Config.OnTop = Integration.IsOnTop;
			ReloadDialog();
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
			about.SetNeedsDisplay();
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

		about.Add(tvConfig, lvSearchEngines, lvPriorityEngines,
		          cbContextMenu, cbOnTop);

		about.AddButton(btnRefresh);
		about.AddButton(btnOk);
		about.AddButton(btnSave);

		Application.Run(about);

		Tf_Input.SetFocus();
		Tf_Input.EnsureFocus();
	}

	private void ClearControls()
	{
		Tf_Input.DeleteAll();
		Tf_Input.ClearHistoryChanges();

		Query = SearchQuery.Null;
		
		Lbl_InputOk.Text = Values.NA;
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

		/*Application.MainLoop.Invoke(() =>
		{ });*/

		/*try {
			// Application.Refresh();
		}
		catch (Exception e) {
			Debug.WriteLine($"{e.Message}", nameof(ClearControls));
		}*/
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

		m_clipboard.Clear();

		Status              = ProgramStatus.Restart;
		Btn_Restart.Enabled = false;
		Btn_Run.Enabled     = true;

		Token.Dispose();
		Token = new();

		Tf_Input.SetFocus();
		Tf_Input.EnsureFocus();

		/*Application.MainLoop.Invoke(() =>
		{ });*/
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
		Btn_Run.Enabled    = false;
		Btn_Cancel.Enabled = true;
		var text = Tf_Input.Text;

		Debug.WriteLine($"{text}", nameof(OnRun));
		var ok = await SetQuery(text);

		if (!ok) {
			return;
		}

		var sw = Stopwatch.StartNew();

		m_runIdleTok = Application.MainLoop.AddIdle(() =>
		{
			Lbl_Status.Text = $"{ResultCount} | {sw.Elapsed.TotalSeconds:F3} sec";
			return true;
		});

		var run = base.RunAsync(null);
		await run;

		sw.Stop();
		Application.MainLoop.RemoveIdle(m_runIdleTok);
	}

	private void OnCancel()
	{
		Token.Cancel();
		Lbl_InputInfo2.Text = $"Canceled";
		Lbl_InputInfo2.SetNeedsDisplay();
		Btn_Restart.Enabled = true;
		Application.MainLoop.RemoveIdle(m_runIdleTok);
	}

	#endregion
}