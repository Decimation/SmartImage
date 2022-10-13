global using Url = Flurl.Url;
using NStack;
using SmartImage.Lib;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Media;
using System.Text;
using System.Threading.Tasks;
using AngleSharp.Dom;
using Kantan.Net.Utilities;
using Terminal.Gui;
using Attribute = Terminal.Gui.Attribute;
using Rune = System.Rune;

// ReSharper disable InconsistentNaming

namespace SmartImage.Modes;

public sealed class GuiMode : BaseProgramMode
{
	#region Values

	private static ustring Err => ustring.Make('!');

	private static ustring NA => ustring.Make(Application.Driver.RightDefaultIndicator);

	private static ustring OK => ustring.Make(Application.Driver.Checked);

	private static ustring PRC => ustring.Make(Application.Driver.Diamond);

	#endregion

	#region Controls

	private static readonly Toplevel Top = Application.Top;

	private static readonly Window Win = new(Resources.Name)
	{
		X = 0,
		Y = 1, // Leave one row for the toplevel menu - todo

		// By using Dim.Fill(), it will automatically resize without manual intervention
		Width       = Dim.Fill(),
		Height      = Dim.Fill(),
		ColorScheme = Styles.CS_Win

	};

	private static readonly Label Lbl_Input = new("Input:")
	{
		X           = 1,
		Y           = 1,
		ColorScheme = Styles.CS_Elem2
	};

	private static readonly TextField Tf_Input = new(ustring.Empty)
	{
		X           = Pos.Right(Lbl_Input),
		Y           = Pos.Top(Lbl_Input),
		Width       = 50,
		ColorScheme = Styles.CS_Win2,
		AutoSize    = false
		// AutoSize = true,
	};

	private static readonly Label Lbl_InputOk = new(NA)
	{
		X = Pos.Right(Tf_Input),
		Y = Pos.Y(Tf_Input),
		// ColorScheme = Styles.CS_Elem4
	};

	private static readonly Button Btn_Ok = new("Run")
	{
		X           = Pos.Right(Lbl_InputOk) + 2,
		Y           = Pos.Y(Tf_Input),
		ColorScheme = Styles.CS_Elem1
	};

	public static readonly Button Btn_Clear = new("X")
	{
		X = Pos.Right(Btn_Ok),
		Y = Pos.Y(Btn_Ok),
		ColorScheme = new()
		{
			Normal    = Attribute.Make(Color.BrightRed, Color.Black),
			Disabled  = Attribute.Make(Color.White, Color.Black),
			HotNormal = Attribute.Make(Color.White, Color.Black),
			HotFocus  = Attribute.Make(Color.Red, Color.Black),
			Focus     = Attribute.Make(Color.Red, Color.Black)
		}
	};

	/*private static readonly ComboBox Cb_Engines = new(Cache.EngineOptions)
	{
		X        = Pos.Right(Btn_Clear),
		Y        = Pos.Y(Btn_Clear),
		AutoSize = true,
		Width    = 15,
		Height   = 25
	};*/
	private static readonly ListView Lv_Engines = new ListView(Cache.EngineOptions)
	{
		AllowsMultipleSelection = true,
		AllowsMarking           = true,
		X                       = Pos.Right(Btn_Clear),
		Y                       = Pos.Y(Btn_Clear),
		AutoSize                = true,
		Width = 15,
		Height = Dim.Height(Tf_Input)
		/*Width                   = 15,
		Height                  = 25*/
	};

	private static readonly DataTable Dt_Results = new()
		{ };

	private static readonly TableView Tv_Results = new()
	{
		X      = Pos.Bottom(Tf_Input),
		Y      = Pos.Y(Tf_Input) + 1,
		Width  = 120,
		Height = 300,

		AutoSize = false
	};

	private static readonly ProgressBar Pbr_Status = new()
	{
		X                = Pos.Right(Lv_Engines),
		Y                = Pos.Y(Lv_Engines),
		Width            = 10,
		ProgressBarStyle = ProgressBarStyle.Continuous,
	};

	#endregion

	#region Overrides of ProgramMode

	public GuiMode() : base(SearchQuery.Null)
	{
		Application.Init();

		var col = new DataColumn[]
		{
			new("Engine", typeof(string)),
			new(nameof(SearchResultItem.Url), typeof(Flurl.Url)),
			new(nameof(SearchResultItem.Similarity), typeof(float)),
			new(nameof(SearchResultItem.Artist), typeof(string)),
			new(nameof(SearchResultItem.Description), typeof(string)),
			new(nameof(SearchResultItem.Source), typeof(string)),
			new(nameof(SearchResultItem.Title), typeof(string)),
			new(nameof(SearchResultItem.Site), typeof(string)),
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
			ColumnStyles                  = columnStyles,
		};

		Btn_Ok.Clicked += async () =>
		{
			var text = Tf_Input.Text;

			Debug.WriteLine($"{text}");

			SearchQuery sq;

			try {
				sq = await SearchQuery.TryCreateAsync(text.ToString());

				if (sq == null) { }
			}
			catch (Exception e) {
				sq = SearchQuery.Null;
			}

			Lbl_InputOk.Text = PRC;

			if (sq is { } && sq != SearchQuery.Null) {
				await sq.UploadAsync();
			}
			else {
				Lbl_InputOk.Text = Err;

				return;
			}

			Debug.WriteLine($">> {sq}");

			Lbl_InputOk.Text = OK;

			Query  = sq;
			Status = 1;

			IsReady.Set();
		};

		Btn_Clear.Clicked += () =>
		{
			try {
				Tf_Input.DeleteAll();
				Query            = SearchQuery.Null;
				Lbl_InputOk.Text = NA;
				Dt_Results.Clear();
				IsReady.Reset();
				ResultCount         = 0;
				Pbr_Status.Fraction = 0;
				Application.Refresh();
			}
			catch (Exception e) {
				Debug.WriteLine($"{e.Message}");
			}
		};

		for (int i = 1; i < Cache.EngineOptions.Length; i++) {
			Lv_Engines.Source.SetMark(i,true);
		}

		Lv_Engines.OpenSelectedItem += args =>
		{

			var val = (SearchEngineOptions) args.Value;

			var isMarked = Lv_Engines.Source.IsMarked(args.Item);

			if (isMarked) {
				if (val == SearchEngineOptions.None) {
					Config.SearchEngines = val;
				}

				else {
					Config.SearchEngines |= val;
				}
			}
			else {
				Config.SearchEngines &= ~val;
			}

			Debug.WriteLine($"{val} {args.Item} -> {Config.SearchEngines} {isMarked}");
		};
		
		Tv_Results.CellActivated += args =>
		{
			if (args.Table is not { }) {
				return;
			}

			try {
				var cell = args.Table.Rows[args.Row][args.Col];

				if (cell is Url { } u) {
					HttpUtilities.OpenUrl(u);
				}
			}
			catch (Exception e) { }
		};

		Tv_Results.Table = Dt_Results;
		
		Win.Add(Lbl_Input, Tf_Input, Btn_Ok, Lbl_InputOk,
		        Btn_Clear, Lv_Engines, Tv_Results, Pbr_Status
		);

		Top.Add(Win);

	}

	public override async Task<object> RunAsync(string[] args, object? sender = null)
	{
		// todo: what the fuck?

		// HACK: Application.Run blocks main thread and async/threads complicate things
		// and due to how the program modes are modeled, it makes things even more convoluted...

		ThreadPool.QueueUserWorkItem(c =>
		{

			Application.Run(errorHandler: exception =>
			{
				Debug.WriteLine($"{exception.Message}");
				return true;
			});

			IsExit.WaitOne();
		});

		var t = base.RunAsync(args, sender);

		await t;

		Application.Run();

		return null;
	}

	public override async Task PreSearchAsync(object? sender)
	{
	}

	public override async Task PostSearchAsync(object? sender, List<SearchResult> results1) { }

	public override Task OnResult(object o, SearchResult r)
	{
		for (int i = 0; i < r.Results.Count; i++) {
			SearchResultItem sri = r.Results[i];

			Dt_Results.Rows.Add($"{r.Engine.Name} #{i + 1}",
			                    sri.Url, sri.Similarity, sri.Artist, sri.Description, sri.Source,
			                    sri.Title, sri.Site);
		}

		// Tv_Results.Redraw(Tv_Results.Bounds);
		// Tv_Results.Redraw(Top.Bounds);

		Pbr_Status.Fraction = (float) ++ResultCount / (Client.Engines.Length);
		Application.Refresh();
		return Task.CompletedTask;
	}

	public override async Task OnComplete(object sender, List<SearchResult> e)
	{
		SystemSounds.Asterisk.Play();

	}

	public override Task CloseAsync()
	{
		Application.Shutdown();
		return Task.CompletedTask;
	}

	public override void Dispose() { }

	#endregion

	private static class Styles
	{
		private static readonly Attribute AT_GreenBlack        = Attribute.Make(Color.Green, Color.Black);
		private static readonly Attribute AT_RedBlack          = Attribute.Make(Color.Red, Color.Black);
		private static readonly Attribute AT_BrightYellowBlack = Attribute.Make(Color.BrightYellow, Color.Black);
		private static readonly Attribute AT_WhiteBlack        = Attribute.Make(Color.White, Color.Black);
		private static readonly Attribute AT_CyanBlack         = Attribute.Make(Color.Cyan, Color.Black);

		internal static readonly ColorScheme CS_Elem1 = new()
		{
			Normal    = AT_GreenBlack,
			Focus     = Attribute.Make(Color.BrightGreen, Color.Black),
			Disabled  = AT_BrightYellowBlack,
			HotNormal = AT_GreenBlack,
			HotFocus  = Attribute.Make(Color.BrightGreen, Color.Black)
		};

		internal static readonly ColorScheme CS_Elem2 = new()
		{
			Normal   = AT_CyanBlack,
			Disabled = Attribute.Make(Color.DarkGray, Color.Black)
		};

		internal static readonly ColorScheme CS_Elem3 = new()
		{
			Normal   = Attribute.Make(Color.BrightBlue, Color.Black),
			Focus    = Attribute.Make(Color.Cyan, Color.DarkGray),
			Disabled = Attribute.Make(Color.BrightBlue, Color.DarkGray)
		};

		internal static readonly ColorScheme CS_Elem4 = new()
		{
			Normal = Attribute.Make(Color.Blue, Color.Gray),
		};

		internal static readonly ColorScheme CS_Win = new()
		{
			Normal    = AT_WhiteBlack,
			Focus     = AT_CyanBlack,
			Disabled  = Attribute.Make(Color.Gray, Color.Black),
			HotNormal = AT_WhiteBlack,
			HotFocus  = AT_CyanBlack
		};

		internal static readonly ColorScheme CS_Title = new()
		{
			Normal = AT_RedBlack,
			Focus  = Attribute.Make(Color.BrightRed, Color.Black)
		};

		internal static readonly ColorScheme CS_Win2 = new()
		{
			Normal = Attribute.Make(Color.Black, Color.White),
			Focus  = Attribute.Make(background: Color.DarkGray, foreground: Color.White)
		};
	}
}