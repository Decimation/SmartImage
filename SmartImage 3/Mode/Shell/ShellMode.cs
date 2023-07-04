global using R2 = SmartImage.Resources;
global using R1 = SmartImage.Lib.Resources;
global using Url = Flurl.Url;
using System.Collections;
using System.Collections.Concurrent;
using System.Data;
using System.Diagnostics;
using System.Dynamic;
using System.Media;
using System.Runtime.Versioning;
using JetBrains.Annotations;
using Kantan.Net.Utilities;
using Kantan.Text;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic.FileIO;
using Novus.FileTypes;
using Novus.Win32;
using NStack;
using SmartImage.App;
using SmartImage.Lib;
using SmartImage.Lib.Results;
using SmartImage.Lib.Utilities;
using SmartImage.Mode.Shell.Assets;
using SmartImage.Utilities;
using Terminal.Gui;
using Attribute = Terminal.Gui.Attribute;
using Clipboard = Novus.Win32.Clipboard;
using Window = Terminal.Gui.Window;
using Kantan.Monad;

// ReSharper disable IdentifierTypo

// ReSharper disable InconsistentNaming
#pragma warning disable IDE0060
namespace SmartImage.Mode.Shell;

public sealed partial class ShellMode : IDisposable, IMode
{
	#region

	private const int CON_WIDTH = 150;

	private const int CON_HEIGHT = 35;

	#endregion

	// NOTE: DO NOT REARRANGE FIELD ORDER
	// NOTE: Static initialization order is nondeterminant with partial classes

	#region Controls

	static ShellMode() { }

	private static readonly Toplevel Top = Application.Top;

	private static readonly Window Win = new(R2.Name)
	{
		X           = 0,
		Y           = 1,
		Width       = Dim.Fill(),
		Height      = Dim.Fill(),
		ColorScheme = UI.Cs_Win,
		CanFocus    = false
	};

	private static readonly MenuBar Mb_Menu = new()
	{
		CanFocus    = false,
		ColorScheme = UI.Cs_Win2,

	};

	private static readonly Label Lbl_Input = new("Input:")
	{
		X           = 1,
		Y           = 0,
		ColorScheme = UI.Cs_Elem2
	};

	private static readonly TextField Tf_Input = new(ustring.Empty)
	{
		X           = Pos.Right(Lbl_Input),
		Y           = Pos.Top(Lbl_Input),
		Width       = 50,
		ColorScheme = UI.Cs_Win2,
		AutoSize    = false,
		// AutoSize = true,
	};

	private static readonly Label Lbl_InputOk = new(UI.NA)
	{
		X           = Pos.Right(Tf_Input) + 1,
		Y           = Pos.Y(Tf_Input),
		ColorScheme = UI.Cs_NA
	};

	private static readonly Button Btn_Run = new("Run")
	{
		X           = Pos.Right(Lbl_InputOk) + 1,
		Y           = Pos.Y(Tf_Input),
		ColorScheme = UI.Cs_Btn1x,
		// Enabled = false
		HotKey = Key.r
	};

	private static readonly Button Btn_Browse = new("Browse")
	{
		X               = Pos.Right(Btn_Run),
		Y               = Pos.Y(Btn_Run),
		HotKey          = Key.Null,
		HotKeySpecifier = default,
		ColorScheme     = UI.Cs_Btn1,
	};

	private static readonly Button Btn_Clear = new("Clear")
	{
		X               = Pos.Right(Btn_Browse),
		Y               = Pos.Y(Btn_Browse),
		HotKey          = Key.Null,
		HotKeySpecifier = default,
		ColorScheme     = UI.Cs_Btn1,
	};

	private static readonly Button Btn_Restart = new("Restart")
	{
		X           = Pos.Right(Btn_Clear),
		Y           = Pos.Y(Btn_Clear),
		Enabled     = false,
		ColorScheme = UI.Cs_Btn1,

	};

	private static readonly Button Btn_Cancel = new("Cancel")
	{
		X           = Pos.Right(Btn_Restart),
		Y           = Pos.Y(Btn_Restart),
		Enabled     = false,
		ColorScheme = UI.Cs_Btn_Cancel,

	};

	private static readonly Button Btn_Config = new("Config")
	{
		X           = Pos.Right(Btn_Cancel),
		Y           = Pos.Y(Btn_Cancel),
		Enabled     = true,
		ColorScheme = UI.Cs_Btn2x,
	};

	private static readonly Label Lbl_InputInfo = new()
	{
		X           = Pos.Bottom(Tf_Input),
		Y           = 1,
		Width       = 10,
		Height      = Dim.Height(Tf_Input),
		ColorScheme = UI.Cs_Lbl2

	};

	private static readonly Label Lbl_QueryUpload = new()
	{
		X           = Pos.Right(Lbl_InputInfo) + 1,
		Y           = 1,
		Width       = 10,
		Height      = Dim.Height(Lbl_InputInfo),
		ColorScheme = UI.Cs_Lbl2

	};

	private static readonly Label Lbl_InputInfo2 = new()
	{
		X           = Pos.Right(Lbl_QueryUpload) + 1,
		Y           = Pos.Y(Lbl_QueryUpload),
		Width       = 10,
		Height      = Dim.Height(Lbl_QueryUpload),
		ColorScheme = UI.Cs_Lbl1
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
		X                    = Pos.Right(Btn_Config) + 1,
		Y                    = Pos.Y(Tf_Input),
		Width                = 10,
		ProgressBarStyle     = ProgressBarStyle.Continuous,
		BidirectionalMarquee = false,
		ProgressBarFormat    = ProgressBarFormat.SimplePlusPercentage
	};

	private static readonly Label Lbl_Status = new()
	{
		X           = Pos.Right(Pbr_Status) + 1,
		Y           = Pos.Y(Pbr_Status),
		Width       = 15,
		Height      = Dim.Height(Lbl_InputInfo),
		ColorScheme = UI.Cs_Lbl1

	};

	private static readonly Label Lbl_Status2 = new()
	{
		X           = Pos.X(Lbl_Status),
		Y           = Pos.Bottom(Lbl_Status),
		Width       = 15,
		Height      = Dim.Height(Lbl_InputInfo),
		ColorScheme = UI.Cs_Lbl1

	};

	private static readonly CheckBox Cb_Queue = new()
	{
		X = Pos.X(Lbl_InputOk),
		Y = Pos.Bottom(Lbl_InputOk),
	};

	private static readonly Button Btn_Next = new("Next")
	{
		X = Pos.Right(Cb_Queue),
		Y = Pos.Y(Cb_Queue),

		Height      = Dim.Height(Btn_Cancel),
		ColorScheme = UI.Cs_Btn1x,
		// Enabled     = false
	};

	private static readonly Button Btn_Queue = new("Queue")
	{
		X = Pos.Right(Btn_Next),
		Y = Pos.Y(Btn_Next),

		Height      = Dim.Height(Btn_Run),
		ColorScheme = UI.Cs_Btn1

	};

	private static readonly Button Btn_Filter = new("Filter")
	{
		X = Pos.Right(Btn_Queue),
		Y = Pos.Y(Btn_Queue),

		Height      = Dim.Height(Btn_Run),
		ColorScheme = UI.Cs_Btn1,
		Enabled     = false
	};

	private static readonly Button Btn_Delete = new("Delete")
	{
		X = Pos.X(Btn_Cancel),
		Y = Pos.Bottom(Btn_Cancel),

		Height      = Dim.Height(Btn_Cancel),
		ColorScheme = UI.Cs_Btn_Cancel,
		Enabled     = false
	};

	private static readonly Button Btn_Reload = new("Reload")
	{
		X = Pos.X(Btn_Restart),
		Y = Pos.Bottom(Btn_Restart),

		Height      = Dim.Height(Btn_Cancel),
		ColorScheme = UI.Cs_Btn_Cancel,
		Enabled     = false
	};

	#endregion

	#region Static

	private static readonly TimeSpan TimeoutTimeSpan = TimeSpan.FromSeconds(1.5);

	[SupportedOSPlatform(Compat.OS)]
	private static readonly SoundPlayer Player = new(R2.hint);

	private static readonly ILogger Logger = LogUtil.Factory.CreateLogger(nameof(ShellMode));

	private static  bool _inputVerifying;
	internal static bool _clipboardFile;
	private static  int  _sequence;

	#endregion

	#region Fields/properties

	private object m_cbCallbackTok;

	private Func<bool>? m_runIdleTok;

	private readonly List<ustring> m_clipboard;

	private readonly ConcurrentBag<SearchResult> m_results;

	private CancellationTokenSource m_token;
	private CancellationTokenSource m_tokenu;

	private readonly Semaphore m_s = new(1, 1);

	#region

	public bool UseClipboard
	{
		get { return Config.Clipboard; }
		set
		{
			Config.Clipboard = value;

			if (Config.Clipboard) {
				m_cbCallbackTok = Application.MainLoop.AddTimeout(TimeoutTimeSpan, ClipboardCallback);

			}
			else {
				m_cbCallbackTok = Application.MainLoop.RemoveTimeout(m_cbCallbackTok);
				m_clipboard.Clear();
			}

		}
	}

	public bool QueueMode { get; private set; }

	private readonly ConcurrentQueue<string> Queue;

	public SearchQuery Query { get; internal set; }

	public SearchConfig Config => Client.Config;

	public SearchClient Client { get; init; }

	private bool? Status { get; set; }

	public string[] Args { get; init; }

	// public int ResultCount { get; private set; }

	public int ResultCount => m_results.Count;

	private ManualResetEvent IsReady { get; set; }

	#endregion

	#endregion

	public ShellMode(string[] args)
	{
		if (Compat.IsWin) {
			try {
				Console.SetWindowSize(CON_WIDTH, CON_HEIGHT);
			}
			catch (Exception e) {
				Debug.WriteLine($"{e.Message}");
			}

		}

		Args     = args;
		m_token  = new();
		m_tokenu = new();
		Query    = SearchQuery.Null;
		Client   = new SearchClient(new SearchConfig());
		IsReady  = new ManualResetEvent(false);
		Queue    = new();

		m_results = new();

		Client.OnResult   += OnResult;
		Client.OnComplete += OnComplete;

		if (Compat.IsWin) {
			Client.OnComplete += OnCompleteWin;
		}

		ProcessArgs();
		ApplyConfig();

		/*
		 * Check if clipboard contains valid query input
		 */

		UseClipboard = Config.Clipboard;

		m_clipboard = new List<ustring>();

		Mb_Menu.Menus = new MenuBarItem[]
		{
			new("_About", null, AboutDialog)
				{ },
			new("_Info", null, InfoDialog)
				{ },
		};

		Top.Add(Mb_Menu);

		var col = new DataColumn[]
		{
			new("Engine", typeof(string)),
			new("*", typeof(string)),

			new(nameof(SearchResultItem.Url), typeof(Url)),
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

		/*
		columnStyles[col[COL_STATUS]].MinAcceptableWidth = 2;
		columnStyles[col[COL_STATUS]].MinWidth           = 1;
		columnStyles[col[COL_STATUS]].MaxWidth           = 2;
		columnStyles[col[COL_URL]].MaxWidth              = 60;
		columnStyles[col[COL_METADATA]].MaxWidth         = 25;
		*/

		Tv_Results.Style = new TableView.TableStyle()
		{
			ShowHorizontalScrollIndicators = true,
			AlwaysShowHeaders              = true,

			RowColorGetter = ResultTable_RowColor,

			ShowHorizontalHeaderUnderline = true,
			ShowHorizontalHeaderOverline  = true,

			ColumnStyles = columnStyles,
		};

		Tv_Results.Border  = UI.Br_1;
		Tv_Results.Table   = Dt_Results;
		Tv_Results.Visible = false;

		Tv_Results.KeyPress      += ResultTable_KeyPress;
		Tv_Results.CellActivated += ResultTable_CellActivated;
		Btn_Run.Clicked          += Run_Clicked;
		Btn_Restart.Clicked      += () => Restart_Clicked(false);
		Btn_Clear.Clicked        += Clear_Clicked;
		Btn_Config.Clicked       += ConfigDialog;
		Btn_Cancel.Clicked       += Cancel_Clicked;
		Btn_Browse.Clicked       += Browse_Clicked;
		Lbl_InputInfo.Clicked    += InputInfo_Clicked;
		Tf_Input.TextChanging    += Input_TextChanging;

		Btn_Delete.Clicked += Delete_Clicked;
		Cb_Queue.Toggled   += Queue_Checked;
		Btn_Queue.Clicked  += Queue_Dialog;
		Btn_Next.Clicked   += Next_Clicked;

		Lbl_QueryUpload.Clicked += () =>
		{
			HttpUtilities.TryOpenUrl(Query.Upload);
		};

		Btn_Queue.Enabled = QueueMode;
		Cb_Queue.Checked  = QueueMode;
		Btn_Next.Enabled  = QueueMode;

		Btn_Filter.Clicked += Filter_Clicked;

		Win.Add(Lbl_Input, Tf_Input, Btn_Run, Lbl_InputOk,
		        Btn_Clear, Tv_Results, Pbr_Status, Lbl_InputInfo, Lbl_QueryUpload,
		        Btn_Restart, Btn_Config, Lbl_InputInfo2, Btn_Cancel, Lbl_Status, Btn_Browse,
		        Lbl_Status2, Btn_Delete, Btn_Queue, Cb_Queue, Btn_Next, Btn_Filter
		);

		Top.Add(Win);
		// Top.HotKey = Key.F5;

		Top.Resized += size =>
		{
			Top.SetNeedsDisplay();
			Top.Redraw(Top.Bounds);
		};

		Application.RootKeyEvent = RootKeyEvent;

		if (Config.AutoSearch && !string.IsNullOrWhiteSpace(Tf_Input.Text.ToString())) {
			Btn_Run.OnClicked();
		}

		if (QueueMode) {
			Next_Clicked();
		}

		/*var tok = Application.MainLoop.AddIdle(() =>
		{
			if (m_autoSearch && IsQueryReady()) {
				Btn_Run.OnClicked();
				return false;
			}

			return true;
		});*/

	}

	public Task<object?> RunAsync(object? sender = null)
	{
		Application.Run();
		return Task.FromResult<object?>(Status);
	}

	private void PreSearch()
	{
		Tf_Input.SetFocus();
		Tv_Results.Visible = true;
		Lbl_Status2.Text   = "Searching...";

	}

	private void PostSearch()
	{
		if (Client.IsComplete) {
			Btn_Run.Enabled    = false;
			Btn_Cancel.Enabled = false;
			Btn_Filter.Enabled = true;
		}

	}

	#region

	private void OnResult(object o, SearchResult result)
	{
		m_results.Add(result);

		Application.MainLoop.Invoke(() =>
		{
			AddResultToTable(result);
		});

	}

	private void AddResultToTable(SearchResult result)
	{
		int st = Dt_Results.Rows.Count;

		var cs = GetColor(result.Engine);

		IndexColors[st] = cs;

		Dt_Results.Rows.Add($"{result.Engine.Name} (Raw)", string.Empty,
		                    result.RawUrl, 0, null, null, $"{result.Status}",
		                    null, null, null, null, null, null);

		// Message[result.RawUrl] = "?";
		// var rawSri = new SearchResultItem(result) { Url = result.RawUrl, Similarity = null };

		for (int i = 0; i < result.Results.Count; i++) {
			SearchResultItem sri = result.Results[i];

			AddResultItemToTable(sri, i);

			for (int j = 0; j < sri.Sisters.Count; j++) {
				AddResultItemToTable(sri.Sisters[j], i, j + 1);
			}
		}

		// Interlocked.Increment(ref ResultCount);
		Pbr_Status.Fraction = (float) m_results.Count / (Client.Engines.Length);

		// Pbr_Status.Fraction = (float) ++ResultCount / (Client.Engines.Length);
		Tv_Results.SetNeedsDisplay();
		Pbr_Status.SetNeedsDisplay();

	}

	private static void AddResultItemToTable(SearchResultItem sri, int i, int j = 0)
	{

		object? meta = sri.Metadata switch
		{
			/*string[] rg      => rg.QuickJoin(),
			Array rg         => rg.QuickJoin(),
			ICollection c    => c.QuickJoin(),*/
			string s1        => s1,
			ExpandoObject eo => eo.QuickJoin(),
			_                => null,

		};

		if (sri.Similarity is { } and 0) {
			sri.Similarity = null;
		}

		var cs = GetColor(sri.Root.Engine);
		var st = Dt_Results.Rows.Count;

		IndexColors[st] = cs;

		string s = $"{sri.Root.Engine.Name} #{i + 1}";

		if (j != 0) {
			s += $".{j}";

		}

		Dt_Results.Rows.Add(s, string.Empty, sri.Url, sri.Score, sri.Similarity, sri.Artist, sri.Description,
		                    sri.Source, sri.Title, sri.Site, sri.Width, sri.Height, meta);
	}

	private void OnComplete(object sender, SearchResult[] results)
	{
		Btn_Restart.Enabled     = true;
		Btn_Cancel.Enabled      = false;
		Lbl_Status2.ColorScheme = UI.Cs_Lbl1_Success;
		Lbl_Status2.Text        = R2.Inf_Complete;
		Btn_Browse.Enabled      = true;

	}

	[SupportedOSPlatform(Compat.OS)]
	private void OnCompleteWin(object sender, SearchResult[] results)
	{

		if (!Config.Silent) {
			Player.Play();
			Native.FlashWindow(ConsoleUtil.HndWindow);

		}

		/*var u  = m_results.SelectMany(r => r.Results).ToArray();
		var di = (await SearchClient.GetDirectImagesAsync(u)).ToArray();

		// await AppNotification.ShowAsync(sender, di);

		foreach (UniSource uniFile in di) {
			uniFile.Dispose();
		}*/

	}

	#endregion

	private async Task<object?> RunSearchAsync()
	{
		PreSearch();

		Status = null;
		IsReady.WaitOne();

		// var results = await Client.RunSearchAsync(Query, m_token.Token);
		await Client.RunSearchAsync(Query, token: m_token.Token);

		Status = false;

		PostSearch();

		return null;
	}

	private void ProcessArgs()
	{
		if (Args.Any()) {
			Config.AutoSearch |= Args.Contains(R2.Arg_AutoSearch);

		}

		var e = Args.GetEnumerator();

		while (e.MoveNext()) {
			var val = e.Current;
			if (val is not string s) continue;

			if (s == R2.Arg_Input) {
				e.MoveNext();
				var s2 = e.Current?.ToString();

				if (SearchQuery.IsValidSourceType(s2)) {
					SetInputText(s2);
				}
			}

			else if (s == R2.Arg_Queue) {
				while (e.MoveNext()) {
					Queue.Enqueue(e.Current.ToString());
				}

				QueueMode = true;

			}
			// Debug.WriteLine($"{s}");

		}
	}

	private bool IsQueryReady()
	{
		return Query != SearchQuery.Null && Url.IsValid(Query.Upload);
	}

	private void ApplyConfig()
	{
		if (Compat.IsWin) {
			Integration.KeepOnTop(Config.OnTop);
		}
	}

	private async Task RunMainAsync()
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
		// Lbl_Status.Text = $"{ResultCount} | {sw.Elapsed.TotalSeconds:F3} sec {UI.OK}";

		Application.MainLoop.RemoveIdle(m_runIdleTok);
	}

	#region

	internal void SetInputText(ustring s)
	{
		Tf_Input.Text = s;

	}

	private async Task<bool> TrySetQueryAsync(ustring text)
	{

		// TODO: IMPROVE

		// Btn_Run.Enabled = false;
		_inputVerifying = true;
		SearchQuery sq = await TryGetQueryAsync(text);

		Lbl_InputOk.SetLabelStatus(null);

		bool ok;

		if (sq  != SearchQuery.Null) {
			bool ret;

			if (Query == sq) {
				// return true;
				goto ret;
			}
			try {

				/*Btn_Cancel.Enabled = true;

				Btn_Cancel.Clicked += () =>
				{
					m_tokenu.Cancel();

				};*/

				// Btn_Cancel.Enabled = true;

				using CancellationTokenSource cts = new();

				Lbl_Status2.Text = $"Uploading...";

				UI.QueueProgress(cts, Pbr_Status);

				var u = await sq.UploadAsync(ct: m_tokenu.Token);

				cts.Cancel();

				Lbl_QueryUpload.Text = u.ToString();
				Lbl_Status2.Text     = ustring.Empty;

				// Btn_Cancel.Clicked   += Cancel_Clicked;
				ret = true;
			}
			catch (Exception e) {
				Debug.WriteLine($"{e.Message}", nameof(TrySetQueryAsync));
				Lbl_InputInfo.Text = $"Error: {e.Message}";
				Lbl_Status2.Text   = ustring.Empty;
				// Btn_Run.Enabled    = false;
				Btn_Reload.Enabled = false;
				ret                = false;
			}

			ok = ret;

		}
		else {
			Lbl_InputInfo.Text = "Error: invalid input";

			Lbl_InputOk.SetLabelStatus(false);
			Btn_Run.Enabled      = false;
			Lbl_QueryUpload.Text = ustring.Empty;
			Pbr_Status.Fraction  = 0;
			Lbl_Status2.Text     = ustring.Empty;
			Btn_Delete.Enabled   = false;
			Btn_Reload.Enabled   = false;
			_inputVerifying      = false;
			return false;
		}
		ret:
		Debug.WriteLine($">> {sq} {Config}", nameof(TrySetQueryAsync));

		Lbl_InputOk.SetLabelStatus(true);

		Query = sq;

		Status = false;

		Lbl_InputInfo.Text = $"{sq}";

		IsReady.Set();
		Btn_Run.Enabled     = false;
		Pbr_Status.Fraction = 0;
		// Btn_Delete.Enabled = true;

		Btn_Browse.Enabled = QueueMode;

		Tf_Input.ReadOnly  = true;
		Btn_Delete.Enabled = true;
		Btn_Reload.Enabled = true;
		_inputVerifying    = false;

		return true;
	}

	private async Task<SearchQuery> TryGetQueryAsync(ustring text)
	{
		if (IsQueryReady() && Query.Uni.Value as string == text) {
			Debug.WriteLine($"Already loaded {text}", nameof(TrySetQueryAsync));
			// Btn_Run.Enabled = true;
			return Query;
		}

		SearchQuery sq;

		Pbr_Status.BidirectionalMarquee = true;
		Pbr_Status.ProgressBarStyle     = ProgressBarStyle.MarqueeContinuous;

		try {
			Pbr_Status.Pulse();

			Lbl_Status2.Text = $"Verifying...";

			lock (Btn_Run) {
				Btn_Run.Enabled = false;
			}

			sq = await SearchQuery.TryCreateAsync(text.ToString());

			// Btn_Run.Enabled = false;

			Pbr_Status.Pulse();
		}
		catch (Exception e) {
			sq = SearchQuery.Null;

			Lbl_InputInfo.Text = $"Error: {e.Message}";
			Lbl_Status2.Text   = ustring.Empty;
			Btn_Reload.Enabled = false;

		}

		return sq;
	}

	#endregion

	#region

	private bool RootKeyEvent(KeyEvent ke)
	{
		bool c   = ke.IsCtrl;
		bool s   = false;
		var  key = ke.Key & ~Key.CtrlMask;

		if (c) {
			switch (key) {
				case Key.C:
					if (Btn_Cancel.Enabled) {
						Btn_Cancel.OnClicked();

					}

					// Cancel_Clicked();
					break;
				case Key.R:
					// Restart_Clicked();
					if (Btn_Restart.Enabled) {
						Btn_Restart.OnClicked();
					}

					break;
				case Key.B:
					Browse_Clicked();
					break;
				case Key.N:
					/*if (QueueMode) {
						Next_Clicked();
					}*/
					if (Btn_Next.Enabled) {
						Btn_Next.OnClicked();

					}

					break;
				default:
					s = false;
					goto ret;
			}

			s = true;

		}

		ret:
		return s;
	}

	private bool ClipboardCallback(MainLoop c)
	{
		// Debug.WriteLine($"executing timeout {nameof(ClipboardCallback)} {c} {UseClipboard} {Clipboard.SequenceNumber}");

		try {
			/*
			 * Don't set input if:
			 *	- Input is already ready
			 *	- Clipboard history contains it already
			 */
			if ((IsQueryReady()&&!string.IsNullOrWhiteSpace(Tf_Input.Text.ToString())) || _inputVerifying) {
				Debug.WriteLine($"Ignoring...");
				goto r1;
			}

			int curSeq  = Clipboard.SequenceNumber;
			int prevSeq = _sequence;
			_sequence = curSeq;

			if (curSeq != prevSeq) {
				Debug.WriteLine($"Sequence changed", nameof(ClipboardCallback));
			}
			else {
				goto r2;
			}

			// var s = Tf_Input.Text.ToString().CleanString();

			var    rc  = Integration.ReadClipboard(out var strs);
			string str = strs[0];

			if (strs.Length > 1) {
				QueueMode = true;
				Queue_Checked(!true);
				Cb_Queue.Checked = true;

				foreach (string str1 in strs[1..]) {
					Queue.Enqueue(str1);
				}
			}

			// var b = !m_clipboard.Contains(str);
			var b = strs.Any(s => !m_clipboard.Contains(s));

			if ( /*!SearchQuery.IsValidSourceType(s)
			    &&*/ rc && b
			    /*&& (m_prevSeq != sequenceNumber)*/) {

				/*bool vl = SearchQuery.IsValidSourceType(str);

				if (vl) {
					var sq = SearchQuery.TryCreateAsync(str);
					sq.Wait();
					if (sq.Result.Uni.FileTypes
					    .All(e => e.IsType(FileType.MT_IMAGE))) { }
				}

				Debug.WriteLine($"{str}");*/
				_clipboardFile = true;

				SetInputText(str);
				_clipboardFile = false;

				// Lbl_InputOk.Text   = UI.Clp;
				Lbl_InputInfo.Text = R2.Inf_Clipboard;

				m_clipboard.Add(str);

				if (Compat.IsWin) {
					ConsoleUtil.FlashTaskbar();
				}

			}

			// note: wtf?
			r1:
			c.RemoveTimeout(m_cbCallbackTok);
			m_cbCallbackTok = c.AddTimeout(TimeoutTimeSpan, ClipboardCallback);
			_clipboardFile  = false;
			return false;
		}
		catch (Exception e) {
			Debug.WriteLine($"{e.Message}", nameof(ClipboardCallback));
		}

		finally { }

		r2:
		_clipboardFile = false;
		// Debug.WriteLine($"{UseClipboard}");
		return true;
		// return UseClipboard;
	}

	#endregion

	private void Clear()
	{
		_inputVerifying   = false;
		Tf_Input.ReadOnly = false;

		Tf_Input.DeleteAll();
		Tf_Input.ClearHistoryChanges();

		Lbl_InputOk.SetLabelStatus(null);

		Lbl_InputOk.SetNeedsDisplay();

		Lbl_Status2.ColorScheme = UI.Cs_Lbl1;

		Dt_Results.Clear();

		Query?.Dispose();
		Query = SearchQuery.Null;
		IsReady.Reset();
		// ResultCount = 0;
		// m_results.Clear();
		Pbr_Status.Fraction = 0;

		Lbl_InputInfo.Text   = ustring.Empty;
		Lbl_QueryUpload.Text = ustring.Empty;
		Lbl_InputInfo2.Text  = ustring.Empty;
		Lbl_Status.Text      = ustring.Empty;
		Lbl_Status2.Text     = ustring.Empty;

		Tv_Results.SetNeedsDisplay();
		Tf_Input.SetFocus();
		Tf_Input.EnsureFocus();
		// Btn_Run.Enabled    = false;
		Btn_Cancel.Enabled = false;

		// Queue.Clear();
		m_results.Clear();

		using var e = Binary.GetEnumerator();

		while (e.MoveNext()) {
			var (k, v) = e.Current;

			foreach (UniSource uv in v) {
				uv.Dispose();
			}
		}

		Binary.Clear();

		_inputVerifying = false;
	}

	public void Close()
	{
		if (Compat.IsWin) {
			Player.Dispose();
		}

		Application.Shutdown();
	}

	public void Dispose()
	{
		Client.Dispose();
		Query.Dispose();
		m_token.Dispose();
		m_tokenu.Dispose();

		Queue.Clear();

		using var e = Binary.GetEnumerator();

		while (e.MoveNext()) {
			var (k, v) = e.Current;

			foreach (UniSource uv in v) {
				uv.Dispose();
			}
		}

		Binary.Clear();

		m_results.Clear();
	}

	internal SearchResultItem? FindResultByUrl(Url v)
	{
		using var e = m_results.GetEnumerator();

		while (e.MoveNext()) {
			var c = e.Current;

			for (int i = 0; i < c.Results.Count; i++) {
				var sr = c.Results[i];

				if (v.Equals(sr.Url)) {
					return sr;
				}

				for (int j = 0; j < sr.Sisters.Count; j++) {
					sr = sr.Sisters[j];

					if (v.Equals(sr.Url)) {
						return sr;
					}
				}
			}
		}

		return null;
	}
}