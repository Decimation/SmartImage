global using Url = Flurl.Url;
using NStack;
using SmartImage.Lib;
using System;
using System.Collections;
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
using Rune = System.Rune;
using System.Reflection;
using Kantan.Utilities;
using Novus.Win32;
using SmartImage.App;
using static Novus.Win32.SysCommand;

// ReSharper disable InconsistentNaming

namespace SmartImage.Modes;

public sealed partial class GuiMode : BaseProgramMode
{
	#region Values

	private static ustring Err => ustring.Make('!');

	private static ustring NA => ustring.Make(Application.Driver.RightDefaultIndicator);

	private static ustring OK => ustring.Make(Application.Driver.Checked);

	private static ustring PRC => ustring.Make(Application.Driver.Diamond);

	#endregion

	#region Controls

	// NOTE: DO NOT REARRANGE FIELD ORDER

	private static readonly Toplevel Top = Application.Top;

	private static readonly Window Win = new(Resources.Name)
	{
		X = 0,
		Y = 1,
		// Leave one row for the toplevel menu - todo

		// By using Dim.Fill(), it will automatically resize without manual intervention
		Width       = Dim.Fill(),
		Height      = Dim.Fill(),
		ColorScheme = Styles.Cs_Win,

	};

	private static readonly MenuBar Mb_Menu = new(new MenuBarItem[]
		{
			new("_Help", null, () =>
			{
				var about = new Dialog("About")
				{
					Text     = "Press enter",
					AutoSize = true,
				};
				var button = new Button("Ok") { };
				button.Clicked += () => Application.RequestStop();
				about.AddButton(button);
				Application.Run(about);
			})
		})
		{ };

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
		AutoSize    = false
		// AutoSize = true,
	};

	private static readonly Label Lbl_InputOk = new(NA)
	{
		X = Pos.Right(Tf_Input) + 1,
		Y = Pos.Y(Tf_Input),
		// ColorScheme = Styles.CS_Elem4
	};

	private static readonly Button Btn_Run = new("Run")
	{
		X           = Pos.Right(Lbl_InputOk) + 1,
		Y           = Pos.Y(Tf_Input),
		ColorScheme = Styles.Cs_Elem1
	};

	private static readonly Button Btn_Clear = new("Clear")
	{
		X               = Pos.Right(Btn_Run),
		Y               = Pos.Y(Btn_Run),
		HotKey          = Key.Null,
		HotKeySpecifier = default,
		ColorScheme = new()
		{
			Normal    = Styles.Atr_BrightRed_Black,
			Disabled  = Styles.Atr_White_Black,
			HotNormal = Styles.Atr_BrightRed_Black,
			HotFocus  = Styles.Atr_Red_Black,
			Focus     = Styles.Atr_Red_Black
		}
	};

	private static readonly Button Btn_Restart = new("Restart")
	{
		X       = Pos.Right(Btn_Clear),
		Y       = Pos.Y(Btn_Clear),
		Enabled = false,
	};

	private static readonly ListView Lv_Engines = new(Cache.EngineOptions)
	{
		AllowsMultipleSelection = true,
		AllowsMarking           = true,
		X                       = Pos.Right(Btn_Restart) + 1,
		Y                       = Pos.Y(Btn_Restart),
		AutoSize                = true,
		Width                   = 15,
		Height                  = Dim.Height(Tf_Input)
	};

	private static readonly Label Lbl_InputInfo = new()
	{
		X      = Pos.Bottom(Tf_Input),
		Y      = 1,
		Width  = 15,
		Height = Dim.Height(Lv_Engines)
	};

	private static readonly DataTable Dt_Results = new()
		{ };

	private static readonly TableView Tv_Results = new()
	{
		X = Pos.X(Lbl_Input),
		Y = Pos.Bottom(Lbl_InputInfo),

		AutoSize = true
	};

	private static readonly ProgressBar Pbr_Status = new()
	{
		X                = Pos.Right(Lv_Engines),
		Y                = Pos.Y(Lv_Engines),
		Width            = 10,
		ProgressBarStyle = ProgressBarStyle.Continuous,
	};

	private static readonly ListView Lv_Integration = new(Integration.IntegrationNames)
	{
		X                       = Pos.X(Lv_Engines),
		Y                       = Pos.Bottom(Lv_Engines),
		Width                   = 15,
		Height                  = Dim.Height(Tf_Input),
		AllowsMarking           = true,
		AllowsMultipleSelection = true
	};

	#endregion

	#region Overrides of ProgramMode

	public GuiMode(string[] args) : base(args, SearchQuery.Null)
	{
		Application.Init();
		Cache.SetConsoleMenu();

		ProcessArgs();

		Top.Add(Mb_Menu);

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

			ColumnStyles = columnStyles,
		};

		Tv_Results.Width  = Console.WindowWidth - 4;
		Tv_Results.Height = Console.WindowHeight;

		Btn_Run.Clicked     += OnRun;
		Btn_Restart.Clicked += OnRestart;
		Btn_Clear.Clicked   += OnClear;

		for (int i = 1; i < Cache.EngineOptions.Length; i++) {
			Lv_Engines.Source.SetMark(i, true);
		}

		Lv_Engines.OpenSelectedItem += OnEngineSelected;
		Lv_Engines.ScrollDown(Cache.EngineOptions.Length);

		Tv_Results.CellActivated += OnCellActivated;

		Tv_Results.Table = Dt_Results;

		Lv_Integration.OpenSelectedItem += OnIntegrationSelected;

		EnsureUICongruency();

		Win.Add(Lbl_Input, Tf_Input, Btn_Run, Lbl_InputOk,
		        Btn_Clear, Lv_Engines, Tv_Results, Pbr_Status, Lbl_InputInfo, Btn_Restart,
		        Lv_Integration
		);

		Top.Add(Win);
	}

	private void EnsureUICongruency()
	{
		var list = Lv_Integration.Source.ToList().Cast<string>().ToArray();

		for (var i = 0; i < Lv_Integration.Source.Count; i++) {
			var b = list[i] == Resources.Int_ContextMenu;

			if (b) {
				Lv_Integration.Source.SetMark(i, Integration.IsContextMenuAdded);
			}
		}
	}

	private void ProcessArgs()
	{
		var enumer = Args.GetEnumerator();

		while (enumer.MoveNext()) {
			var val = enumer.Current as string;

			if (val == Resources.Arg_Input) {
				enumer.MoveNext();
				Tf_Input.Text = enumer.Current.ToString();
			}
		}
	}

	private void OnIntegrationSelected(ListViewItemEventArgs eventArgs)
	{
		var marked = Lv_Integration.Source.IsMarked(eventArgs.Item);
		var value  = (string) eventArgs.Value;

		if (value == Resources.Int_ContextMenu) {
			App.Integration.HandleContextMenu(marked);
		}

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

	public override void PostSearch(object? sender, List<SearchResult> results1) { }

	public override void OnResult(object o, SearchResult r)
	{
		Application.MainLoop.Invoke(() =>
		{
			for (int i = 0; i < r.Results.Count; i++) {
				SearchResultItem sri = r.Results[i];

				Dt_Results.Rows.Add($"{r.Engine.Name} #{i + 1}",
				                    sri.Url, sri.Similarity, sri.Artist, sri.Description, sri.Source,
				                    sri.Title, sri.Site);
			}

			Pbr_Status.Fraction = (float) ++ResultCount / (Client.Engines.Length);

		});

	}

	public override void OnComplete(object sender, List<SearchResult> e)
	{
		SystemSounds.Asterisk.Play();
		Btn_Restart.Enabled = true;

	}

	public override void Close()
	{
		Application.Shutdown();
	}

	public override void Dispose() { }

	#endregion

	#region Control functions

	private void OnCellActivated(TableView.CellActivatedEventArgs args)
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
	}

	private void OnEngineSelected(ListViewItemEventArgs args)
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

		/*var selected = EnumHelper.GetSetFlags<SearchEngineOptions>(Config.SearchEngines, false);

		for (int se = 0; se < Lv_Engines.Source.Count; se++) {
			Lv_Engines.Source.SetMark(se, selected.Contains(val));
		}*/

		/*var selected = EnumHelper.GetSetFlags(Config.SearchEngines, false);
		var list     = Lv_Engines.Source.ToList().Cast<SearchEngineOptions>().ToList();

		for (int se = 0; se < Lv_Engines.Source.Count; se++) {
			var a =Lv_Engines.Source.IsMarked(se);
			var x = list[se];

			switch (a) {
				case true when selected.Contains(x):
					continue;
				case true:
				{
					if (!selected.Contains(x)) {
						Lv_Engines.Source.SetMark(se, false);
					}

					break;
				}
			}

		}*/

		Debug.WriteLine($"{val} {args.Item} -> {Config.SearchEngines} {isMarked}");
	}

	private void OnRestart()
	{
		if (!Client.IsComplete) {
			return;
		}

		OnClear();
		Dt_Results.Clear();
		Status              = ProgramStatus.Restart;
		Btn_Restart.Enabled = false;
	}

	private async void OnRun()
	{
		var text = Tf_Input.Text;

		Debug.WriteLine($"{text}", nameof(OnRun));

		SearchQuery sq;

		try {
			sq = await SearchQuery.TryCreateAsync(text.ToString());
		}
		catch (Exception e) {
			sq = SearchQuery.Null;

			Lbl_InputInfo.Text = $"Error: {e.Message}";
		}

		Lbl_InputOk.Text = PRC;

		if (sq is { } && sq != SearchQuery.Null) {
			var u = await sq.UploadAsync();
		}
		else {
			Lbl_InputOk.Text   = Err;
			Lbl_InputInfo.Text = "Error: invalid input";
			return;
		}

		Debug.WriteLine($">> {sq}", nameof(OnRun));

		Lbl_InputOk.Text = OK;

		Query  = sq;
		Status = ProgramStatus.Signal;

		Lbl_InputInfo.Text = $"{(sq.IsFile ? "File" : "Uri")} : {sq.FileTypes.First()}";

		IsReady.Set();

		var run = base.RunAsync(null);

		await run;
	}

	private void OnClear()
	{
		try {
			Tf_Input.DeleteAll();

			Query = SearchQuery.Null;

			Lbl_InputOk.Text = NA;
			Dt_Results.Clear();

			IsReady.Reset();
			ResultCount         = 0;
			Pbr_Status.Fraction = 0;

			Lbl_InputInfo.Text = ustring.Empty;

			Application.Refresh();
		}
		catch (Exception e) {
			Debug.WriteLine($"{e.Message}", nameof(OnClear));
		}
	}

	#endregion
}