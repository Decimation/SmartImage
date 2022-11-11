global using R2 = SmartImage.Resources;
global using R1 = SmartImage.Lib.Resources;
global using Url = Flurl.Url;
using System.Collections;
using System.Data;
using System.Diagnostics;
using System.Dynamic;
using System.Media;
using System.Runtime.Versioning;
using Kantan.Net.Utilities;
using Kantan.Text;
using Novus;
using Novus.FileTypes;
using Novus.OS;
using NStack;
using SmartImage.App;
using SmartImage.Lib;
using SmartImage.Shell;
using Terminal.Gui;
using Window = Terminal.Gui.Window;

// ReSharper disable InconsistentNaming
#pragma warning disable IDE0060
namespace SmartImage;

public sealed partial class GuiMain : IDisposable
{
	// NOTE: DO NOT REARRANGE FIELD ORDER
	// NOTE: Static initialization order is nondeterminant with partial classes

	#region Controls

	private static readonly Toplevel Top = Application.Top;

	private static readonly Window Win = new(Resources.Name)
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
		X = Pos.Right(Tf_Input) + 1,
		Y = Pos.Y(Tf_Input),
		// ColorScheme = Styles.CS_Elem4
	};

	private static readonly Button Btn_Run = new("Run")
	{
		X           = Pos.Right(Lbl_InputOk) + 1,
		Y           = Pos.Y(Tf_Input),
		ColorScheme = UI.Cs_Btn1x,

	};

	private static readonly Button Btn_Clear = new("Clear")
	{
		X               = Pos.Right(Btn_Run),
		Y               = Pos.Y(Btn_Run),
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
		ColorScheme = UI.Cs_Btn1,

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

	#endregion

	#region Fields/properties

	private object m_cbCallbackTok;

	private Func<bool>? m_runIdleTok;

	private readonly List<ustring> m_clipboard;

	private bool m_autoSearch;

	private static readonly TimeSpan    TimeoutTimeSpan = TimeSpan.FromSeconds(1.5);
	private static readonly SoundPlayer m_sndHint       = new SoundPlayer(R2.hint);

	#region

	public SearchQuery Query { get; internal set; }

	public SearchConfig Config => Client.Config;

	public SearchClient Client { get; init; }

	internal ProgramStatus Status { get; set; }

	public string[] Args { get; init; }

	private int ResultCount { get; set; }

	internal ManualResetEvent IsReady { get; set; }

	private CancellationTokenSource Token { get; set; }

	#endregion

	#endregion

	public GuiMain(string[] args)
	{
		Args    = args;
		Token   = new();
		Query   = SearchQuery.Null;
		Client  = new SearchClient(new SearchConfig());
		IsReady = new ManualResetEvent(false);

		// QueryMat = null;

		Client.OnResult   += OnResult;
		Client.OnComplete += OnComplete;

		// Application.Init();

		ProcessArgs();
		ApplyConfig();

		/*
		 * Check if clipboard contains valid query input
		 */

		m_cbCallbackTok = Application.MainLoop.AddTimeout(TimeoutTimeSpan, ClipboardCallback);

		m_clipboard = new List<ustring>();

		Mb_Menu.Menus = new MenuBarItem[]
		{
			new("_About", null, AboutDialog),
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

		Tv_Results.Border  = UI.Br_1;
		Tv_Results.Table   = Dt_Results;
		Tv_Results.Visible = false;

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

			Func<string, bool> f = _ => false;

			if (Query.Uni.IsFile) {
				f = FileSystem.ExploreFile;
			}
			else if (Query.Uni.IsUri) {
				f = HttpUtilities.TryOpenUrl;
			}

			f(Query.Uni.Value);
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

	public Task<object?> RunAsync(object? sender = null)
	{
		Application.Run();
		return Task.FromResult(Status == ProgramStatus.Restart ? (object) true : null);
	}

	private void PreSearch(object? sender)
	{
		Tf_Input.SetFocus();
		Tv_Results.Visible = true;
	}

	private void PostSearch(object? sender, List<SearchResult> results1)
	{

		if (Client.IsComplete) {
			Btn_Run.Enabled    = false;
			Btn_Cancel.Enabled = false;
		}
	}

	private void OnResult(object o, SearchResult r)
	{
		Application.MainLoop.Invoke(() =>
		{
			Dt_Results.Rows.Add($"{r.Engine.Name} (Raw)", r.RawUrl, 0, null, $"{r.Status}",
			                    null, null, null, null, null, null, null);

			for (int i = 0; i < r.Results.Count; i++) {
				SearchResultItem sri = r.Results[i];

				object? meta = sri.Metadata switch
				{
					string[] rg      => rg.QuickJoin(),
					Array rg         => rg.QuickJoin(),
					ICollection c    => c.QuickJoin(),
					string s         => s,
					ExpandoObject eo => eo.QuickJoin(),
					_                => null,

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

	private void OnComplete(object sender, List<SearchResult> e)
	{
		if (OperatingSystem.IsWindows()) {
			OnCompleteWin(sender, e);
		}

		Btn_Restart.Enabled = true;
		Btn_Cancel.Enabled  = false;

	}

	[SupportedOSPlatform(Global.OS_WIN)]
	private void OnCompleteWin(object sender, List<SearchResult> e)
	{
		m_sndHint.Play();
		var x=SearchClient.Optimize(e).AsParallel().Where(r => r.Score >= 5).Select(async r =>
		{
			return await UniFile.TryGetAsync(r.Url);
		});

		AppToast.ShowToast(sender, e);
	}

	public void Close()
	{
		m_sndHint.Dispose();
		Application.Shutdown();
	}

	private void ProcessArgs()
	{
		var e = Args.GetEnumerator();

		m_autoSearch = false;

		while (e.MoveNext()) {
			var val = e.Current;
			if (val is not string s) continue;

			if (s == R2.Arg_AutoSearch) {
				/*e.MoveNext();
				_ = e.Current?.ToString();*/

				// IsReady.Set();

			}

			if (s == R2.Arg_Input) {
				e.MoveNext();
				var s2 = e.Current?.ToString();

				if (SearchQuery.IsIndicatorValid(s2)) {
					SetInputText(s2);

				}
			}
			// Debug.WriteLine($"{s}");

		}
	}

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

		Pbr_Status.BidirectionalMarquee = true;
		Pbr_Status.ProgressBarStyle     = ProgressBarStyle.MarqueeContinuous;

		try {
			Pbr_Status.Pulse();
			sq = await SearchQuery.TryCreateAsync(text.ToString());
			Pbr_Status.Pulse();
		}
		catch (Exception e) {
			sq = SearchQuery.Null;

			Lbl_InputInfo.Text = $"Error: {e.Message}";

		}

		Lbl_InputOk.Text = UI.PRC;

		if (sq is { } && sq != SearchQuery.Null) {
			try {
				Pbr_Status.Pulse();

				var t = sq.UploadAsync();
				Pbr_Status.Pulse();

				var u = await t;
				Pbr_Status.Pulse();

				Lbl_QueryUpload.Text = u.ToString();

			}
			catch (Exception e) {
				Debug.WriteLine($"{e.Message}", nameof(SetQuery));
			}

		}
		else {
			Lbl_InputOk.Text     = UI.Err;
			Lbl_InputInfo.Text   = "Error: invalid input";
			Btn_Run.Enabled      = true;
			Lbl_QueryUpload.Text = ustring.Empty;
			Pbr_Status.Fraction  = 0;

			return false;
		}

		Debug.WriteLine($">> {sq} {Config}", nameof(SetQuery));

		Lbl_InputOk.Text = UI.OK;

		Query  = sq;
		Status = ProgramStatus.Signal;

		Lbl_InputInfo.Text = $"{sq}";

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
			if (Integration.ReadClipboard(out var str) &&
			    !IsInputValidIndicator() && !m_clipboard.Contains(str)) {
				SetInputText(str);
				// Lbl_InputOk.Text   = UI.Clp;
				Lbl_InputInfo.Text = Resources.Inf_Clipboard;

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

	private async Task<object?> RunSearchAsync(object? sender = null)
	{
		var now = Stopwatch.StartNew();

		PreSearch(sender);

		Status = ProgramStatus.None;
		IsReady.WaitOne();

		var results = await Client.RunSearchAsync(Query, Token.Token);

		now.Stop();

		Status = ProgramStatus.Signal;

		PostSearch(sender, results);

		return null;
	}

	public void Dispose()
	{
		Client.Dispose();
		Query.Dispose();
		Token.Dispose();
	}
}

public enum ProgramStatus
{
	None,
	Signal,
	Restart
}