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
using Clipboard = Novus.Win32.Clipboard;
using Window = Terminal.Gui.Window;

// ReSharper disable IdentifierTypo

// ReSharper disable InconsistentNaming
#pragma warning disable IDE0060
namespace SmartImage.Mode.Shell;

public sealed partial class ShellMode : IDisposable, IMode
{
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
	};

	private static readonly MenuBar Mb_Menu = new()
	{
		CanFocus = false,
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
		ColorScheme = UI.Cs_Btn2,
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
		Enabled     = false
	};

	private static readonly Button Btn_Queue = new("Queue")
	{
		X = Pos.Right(Btn_Next),
		Y = Pos.Y(Btn_Next),

		Height      = Dim.Height(Btn_Run),
		ColorScheme = UI.Cs_Btn1

	};

	private static readonly Button Btn_Delete = new("Delete")
	{
		X = Pos.X(Btn_Cancel),
		Y = Pos.Bottom(Btn_Cancel),

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

	#endregion

	#region Fields/properties

	private object m_cbCallbackTok;

	private Func<bool>? m_runIdleTok;

	private readonly List<ustring> m_clipboard;

	private bool m_autoSearch;

	private readonly ConcurrentBag<SearchResult> m_results;

	private CancellationTokenSource m_token;
	private CancellationTokenSource m_tokenu;

	private static int m_seq;

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

	public ConcurrentQueue<string> Queue { get; private set; }

	public SearchQuery Query { get; internal set; }

	public SearchConfig Config => Client.Config;

	public SearchClient Client { get; init; }

	internal bool? Status { get; set; }

	public string[] Args { get; init; }

	// public int ResultCount { get; private set; }

	public int ResultCount => m_results.Count;

	internal ManualResetEvent IsReady { get; set; }

	#endregion

	#endregion

	public ShellMode(string[] args)
	{
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

		// Application.Init();

		ProcessArgs();
		ApplyConfig();

		/*
		 * Check if clipboard contains valid query input
		 */

		// m_cbCallbackTok = Application.MainLoop.AddTimeout(TimeoutTimeSpan, ClipboardCallback);
		// UseClipboard = true;

		m_clipboard = new List<ustring>();

		Mb_Menu.Menus = new MenuBarItem[]
		{
			new("_About", null, AboutDialog),
		};

		Top.Add(Mb_Menu);

		var col = new DataColumn[]
		{
			new("Engine", typeof(string)),

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

		Tv_Results.Border  = UI.Br_1;
		Tv_Results.Table   = Dt_Results;
		Tv_Results.Visible = false;

		Tv_Results.CellActivated += Result_CellActivated;
		Btn_Run.Clicked          += Run_Clicked;
		Btn_Restart.Clicked      += () => Restart_Clicked(false);
		Btn_Clear.Clicked        += Clear_Clicked;
		Btn_Config.Clicked       += ConfigDialog;
		Btn_Cancel.Clicked       += Cancel_Clicked;
		Btn_Browse.Clicked       += Browse_Clicked;
		Lbl_InputInfo.Clicked    += InputInfo_Clicked;
		Tf_Input.TextChanging    += Input_TextChanging;
		Btn_Delete.Clicked       += Delete_Clicked;
		Cb_Queue.Toggled         += Queue_Checked;
		Btn_Queue.Clicked        += Queue_Clicked;
		Btn_Next.Clicked         += Next_Clicked;

		Lbl_QueryUpload.Clicked += () =>
		{
			HttpUtilities.TryOpenUrl(Query.Upload);
		};
		Btn_Queue.Enabled = false;

		Win.Add(Lbl_Input, Tf_Input, Btn_Run, Lbl_InputOk,
		        Btn_Clear, Tv_Results, Pbr_Status, Lbl_InputInfo, Lbl_QueryUpload,
		        Btn_Restart, Btn_Config, Lbl_InputInfo2, Btn_Cancel, Lbl_Status, Btn_Browse,
		        Lbl_Status2, Btn_Delete, Btn_Queue, Cb_Queue, Btn_Next
		);

		Top.Add(Win);
		Top.HotKey = Key.F5;

		Top.Resized += size =>
		{
			Top.SetNeedsDisplay();
			Top.Redraw(Top.Bounds);
		};

		if (m_autoSearch) {
			Btn_Run.OnClicked();
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

	}

	private void PostSearch()
	{
		if (Client.IsComplete) {
			Btn_Run.Enabled    = false;
			Btn_Cancel.Enabled = false;
		}
	}

	#region SearchClient callbacks

	private void OnResult(object o, SearchResult result)
	{
		m_results.Add(result);

		Application.MainLoop.Invoke(() =>
		{
			Dt_Results.Rows.Add($"{result.Engine.Name} (Raw)",
			                    result.RawUrl, 0, 0, null, $"{result.Status}",
			                    null, null, null, null, null, null);

			for (int i = 0; i < result.Results.Count; i++) {
				SearchResultItem sri = result.Results[i];

				object? meta = sri.Metadata switch
				{
					string[] rg      => rg.QuickJoin(),
					Array rg         => rg.QuickJoin(),
					ICollection c    => c.QuickJoin(),
					string s         => s,
					ExpandoObject eo => eo.QuickJoin(),
					_                => null,

				};

				Dt_Results.Rows.Add($"{result.Engine.Name} #{i + 1}",
				                    sri.Url, sri.Score, sri.Similarity, sri.Artist, sri.Description, sri.Source,
				                    sri.Title, sri.Site, sri.Width, sri.Height, meta);
			}

			// Interlocked.Increment(ref ResultCount);
			Pbr_Status.Fraction = (float) m_results.Count / (Client.Engines.Length);

			// Pbr_Status.Fraction = (float) ++ResultCount / (Client.Engines.Length);
			Tv_Results.SetNeedsDisplay();
			Pbr_Status.SetNeedsDisplay();
		});

	}

	private void OnComplete(object sender, SearchResult[] results)
	{
		Btn_Restart.Enabled = true;
		Btn_Cancel.Enabled  = false;
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

	public void Close()
	{
		if (Compat.IsWin) {
			Player.Dispose();
		}

		Application.Shutdown();
	}

	private void ProcessArgs()
	{
		m_autoSearch = Args.Contains(R2.Arg_AutoSearch);

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

	internal void SetInputText(ustring s)
	{
		Tf_Input.Text = s;

	}

	private async Task<bool> SetQuery(ustring text)
	{
		// Btn_Run.Enabled = false;

		if (IsQueryReady() && Query.Uni.Value as string == text) {
			Debug.WriteLine($"Already loaded {text}", nameof(SetQuery));
			// Btn_Run.Enabled = true;
			return true;
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
		}

		UI.SetLabelStatus(Lbl_InputOk, null);

		if (sq is { } && sq != SearchQuery.Null) {
			try {

				/*Btn_Cancel.Enabled = true;

				Btn_Cancel.Clicked += () =>
				{
					m_tokenu.Cancel();

				};*/

				using CancellationTokenSource cts = new();
				Lbl_Status2.Text = $"Uploading...";

				UI.QueueProgress(cts, Pbr_Status);

				var u = await sq.UploadAsync(ct: m_tokenu.Token);

				cts.Cancel();

				Lbl_QueryUpload.Text = u.ToString();
				Lbl_Status2.Text     = ustring.Empty;

				// Btn_Cancel.Clicked   += Cancel_Clicked;

			}
			catch (Exception e) {
				Debug.WriteLine($"{e.Message}", nameof(SetQuery));
				Lbl_InputInfo.Text = $"Error: {e.Message}";
				Lbl_Status2.Text   = ustring.Empty;
				// Btn_Run.Enabled    = false;

			}

		}
		else {
			Lbl_InputInfo.Text = "Error: invalid input";

			UI.SetLabelStatus(Lbl_InputOk, false);
			Btn_Run.Enabled      = false;
			Lbl_QueryUpload.Text = ustring.Empty;
			Pbr_Status.Fraction  = 0;
			Lbl_Status2.Text     = ustring.Empty;
			Btn_Delete.Enabled   = false;

			return false;
		}

		Debug.WriteLine($">> {sq} {Config}", nameof(SetQuery));

		UI.SetLabelStatus(Lbl_InputOk, true);

		Query = sq;

		Status = false;

		Lbl_InputInfo.Text = $"{sq}";

		IsReady.Set();
		Btn_Run.Enabled     = false;
		Pbr_Status.Fraction = 0;
		// Btn_Delete.Enabled = true;

		Tf_Input.ReadOnly  = true;
		Btn_Delete.Enabled = true;

		return true;
	}

	private async Task<object?> RunSearchAsync()
	{
		PreSearch();

		Status = null;
		IsReady.WaitOne();

		var results = await Client.RunSearchAsync(Query, m_token.Token);

		Status = false;

		PostSearch();

		return null;
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

			int curSeq  = Clipboard.SequenceNumber;
			int prevSeq = m_seq;
			m_seq = curSeq;

			if (curSeq != prevSeq) {
				Debug.WriteLine($"Sequence changed", nameof(ClipboardCallback));
			}
			else {
				goto r2;
			}

			var s = Tf_Input.Text.ToString().CleanString();

			var rc = Integration.ReadClipboard(out var str);

			var b = !m_clipboard.Contains(str);

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

				SetInputText(str);
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
			return false;
		}
		catch (Exception e) {
			Debug.WriteLine($"{e.Message}", nameof(ClipboardCallback));
		}

		finally { }

		r2:
		// Debug.WriteLine($"{UseClipboard}");
		return true;
		// return UseClipboard;
	}

	public void Dispose()
	{
		Client.Dispose();
		Query.Dispose();
		m_token.Dispose();
		m_tokenu.Dispose();
		Queue.Clear();
		m_results.Clear();
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

	private void Clear()
	{
		Tf_Input.ReadOnly = false;

		Tf_Input.DeleteAll();
		Tf_Input.ClearHistoryChanges();

		UI.SetLabelStatus(Lbl_InputOk, null);

		Lbl_InputOk.SetNeedsDisplay();

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
	}
}