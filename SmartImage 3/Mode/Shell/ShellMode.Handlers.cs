// Read S SmartImage ShellMode.Handlers.cs
// 2023-02-14 @ 12:13 AM

using System.Collections.Concurrent;
using System.Data;
using System.Diagnostics;
using System.Net;
using Flurl.Http;
using Kantan.Net.Utilities;
using Kantan.Text;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.FileIO;
using Novus.FileTypes;
using Novus.Win32.Structures.User32;
using NStack;
using SmartImage.App;
using SmartImage.Lib;
using SmartImage.Lib.Engines;
using SmartImage.Lib.Engines.Impl.Search;
using SmartImage.Lib.Results;
using SmartImage.Lib.Utilities;
using SmartImage.Mode.Shell.Assets;
using Terminal.Gui;
using Attribute = Terminal.Gui.Attribute;
using Clipboard = Novus.Win32.Clipboard;
using FileSystem = Novus.OS.FileSystem;

namespace SmartImage.Mode.Shell;

public sealed partial class ShellMode
{
	private const int INV = -1;

	private const int COL_URL      = 2;
	private const int COL_STATUS   = 1;
	private const int COL_METADATA = 12;

	private static readonly Dictionary<SearchEngineOptions, Color> ColorValues = new()
	{
		[SearchEngineOptions.SauceNao] = Color.Green,
		[SearchEngineOptions.EHentai]  = Color.Magenta,
		[SearchEngineOptions.Iqdb]     = Color.BrightGreen,
		[SearchEngineOptions.Ascii2D]  = Color.Cyan,
		[SearchEngineOptions.TraceMoe] = Color.Blue,

		[SearchEngineOptions.RepostSleuth] = Color.Brown,

		[SearchEngineOptions.ArchiveMoe] = Color.BrightBlue,
		[SearchEngineOptions.Yandex]     = Color.BrightRed,

		[SearchEngineOptions.GoogleImages] = Color.DarkGray,
		[SearchEngineOptions.KarmaDecay]   = Color.DarkGray,
		[SearchEngineOptions.Bing]         = Color.DarkGray,
		[SearchEngineOptions.TinEye]       = Color.DarkGray,
		[SearchEngineOptions.ImgOps]       = Color.DarkGray,

	};

	private static readonly ConcurrentDictionary<object, string>                Downloaded   = new();
	private static readonly ConcurrentDictionary<BaseSearchEngine, ColorScheme> EngineColors = new();
	private static readonly ConcurrentDictionary<DataRow, ColorScheme>              IndexColors  = new();
	private static readonly ConcurrentDictionary<Color, ColorScheme>            IndexColors2 = new();
	private static readonly ConcurrentDictionary<SearchResultItem, UniSource[]> Binary       = new();

	private static ColorScheme GetEngineColorScheme(BaseSearchEngine bse)
	{
		if (EngineColors.TryGetValue(bse, out var cs)) {
			return cs;

		}

		var cc = ColorValues[bse.EngineOption];

		if (!IndexColors2.TryGetValue(cc, out cs)) {

			var attrNormal = Attribute.Make(cc, Color.Black);
			var attrFocus  = Attribute.Make(Color.White, cc);

			cs = new ColorScheme
			{
				Normal = attrNormal,
				Focus  = attrFocus,

			}.NormalizeHot();
			IndexColors2.TryAdd(cc, cs);
		}

		EngineColors.TryAdd(bse, cs);

		return cs;
	}

	private async void Input_TextChanging(TextChangingEventArgs eventArgs)
	{
		if (_inputVerifying) {
			return;
		}

		if (!m_s.WaitOne(300)) {
			return;
		}

		var text = eventArgs.NewText?.ToString()?.TrimStart('\"');

		// Debug.WriteLine($"testing {text}", nameof(Input_TextChanging));

		// Application.MainLoop.Invoke(() => Task.Delay(TimeSpan.FromSeconds(1)));

		var sourceType = SearchQuery.IsValidSourceType(text);

		bool ok1 = false;

		if (sourceType) {
			// m_upload.WaitOne();
			/*if (_inputVerifying) {
				return;
			}*/

			ok1 = await TrySetQueryAsync(text);

			Btn_Run.Enabled = ok1;
			// Debug.WriteLine($"{nameof(Input_TextChanging)} :: ok");

			// m_upload.Release();
			/*if (ok && Config.AutoSearch && !Client.IsRunning) {
				Run_Clicked();
			}*/
		}

		var ok = ok1;

		m_s.Release();

		/*
			if (ok && Config.AutoSearch && !Client.IsRunning)
			{
				Run_Clicked();
			}
			*/

		Application.MainLoop.Invoke(() =>
		{
			if (ok && Config.AutoSearch && !Client.IsRunning) {
				Run_Clicked();
			}

		});
	}

	/// <summary>
	///     <see cref="Btn_Restart" />
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

		// m_clipboard.RemoveRange(1, m_clipboard.Count - 1);

		m_clipboard.Clear();

		foreach (var res in m_results) {
			res.Dispose();
		}

		m_results.Clear();

		Status = true;

		Btn_Restart.Enabled = false;
		Btn_Cancel.Enabled  = false;
		Btn_Run.Enabled     = true;
		Btn_Delete.Enabled  = false;

		m_token.Dispose();
		m_token = new();
		m_tokenu.Dispose();
		m_tokenu = new();

		Tf_Input.SetFocus();
		Tf_Input.EnsureFocus();
		Btn_Filter.Text = "Filter";
		_inputVerifying = false;
	}

	/// <summary>
	///     <see cref="Btn_Run" />
	/// </summary>
	private async void Run_Clicked()
	{
		Btn_Run.Enabled = false;
		// Btn_Delete.Enabled = false;

		var text = Tf_Input.Text;

		Debug.WriteLine($"Input: {text}", nameof(Run_Clicked));

		var ok = await TrySetQueryAsync(text);

		Btn_Cancel.Enabled = ok;
		Tv_Results.Visible = ok;

		if (!ok) {
			return;
		}

		await RunMainAsync();
	}

	#region Filter

	private static int _filterOrder;

	private const int FILTER_MAX = 4;

	private async void Filter_Clicked()
	{
		// TODO

		var res = m_results
			.Where(r => r.Status is SearchResultStatus.Success or SearchResultStatus.None)
			.SelectMany(r => r.AllResults);

		_filterOrder    = Math.Clamp(++_filterOrder, 0, FILTER_MAX);
		Btn_Filter.Text = $"Filter {_filterOrder}";

		if (_filterOrder >= 1) {
			res = res.Where(x => x.Score >= 3);
		}

		if (_filterOrder >= 2) {
			res = res.Where(x => SearchQuery.IsValidSourceType(x.Url));

		}

		var resl = new ConcurrentBag<SearchResultItem>();

		if (_filterOrder >= 3) {
			await Parallel.ForEachAsync(res.ToArray(), async (item, token) =>
			{
				using var r = await item.Url
					              .AllowAnyHttpStatus()
					              .OnError(x => x.ExceptionHandled = true)
					              .GetAsync(token);

				switch (r.ResponseMessage.StatusCode) {
					case HttpStatusCode.NotFound:
					case HttpStatusCode.UnavailableForLegalReasons:
					case HttpStatusCode.Unauthorized:
						return;
					default:
						resl.Add(item);
						break;
				}
			});
			res = resl;
		}

		if (_filterOrder == FILTER_MAX) {
			Btn_Filter.Enabled = false;

			var res3 = res as List<SearchResultItem> ?? res.ToList();
			var res2 = new ConcurrentBag<SearchResultItem>();

			await Parallel.ForEachAsync(res3, async (item, token) =>
			{
				var us = await item.LoadUniAsync(token).ConfigureAwait(false);

				if (us) {
					res2.Add(item);

				}
			});
			res = res2;

			_filterOrder = 0;
		}

		ret:
		IndexColors.Clear();
		Dt_Results.Clear();

		var resx = res as SearchResultItem[] ?? res.ToArray();
		var rg   = resx.GroupBy(r => r.Root);

		foreach (var gg in rg) {
			int i = 0;

			foreach (var sri in gg) {
				AddResultItemToTable(sri, i);
				i++;
				Tv_Results.Update();
			}

		}

		Btn_Filter.Enabled = true;

	}

	#endregion

	private void Queue_Checked(bool b)
	{
		QueueMode = !b;

		Btn_Queue.Enabled = QueueMode;
		Btn_Next.Enabled  = QueueMode;
	}

	/// <summary>
	///     <see cref="Btn_Browse" />
	/// </summary>
	private void Browse_Clicked()
	{
		if (Compat.IsWin) {
			Integration.KeepOnTop(false);

		}

		OpenFileNameFlags flags = 0x0;

		if (QueueMode) {
			flags |= OpenFileNameFlags.OFN_ALLOWMULTISELECT;
		}

		flags |= OpenFileNameFlags.OFN_EXPLORER | OpenFileNameFlags.OFN_FILEMUSTEXIST |
		         OpenFileNameFlags.OFN_LONGNAMES | OpenFileNameFlags.OFN_PATHMUSTEXIST;
		string[]? files;

		try {
			files = Integration.OpenFile(flags);
		}
		catch (Exception e) {
			Debug.WriteLine($"{e.Message}");
			return;
		}

		if (QueueMode) {
			foreach (string fs in files) {
				Queue.Enqueue(fs);
			}

			if (!IsQueryReady()) {
				NextQueue();

			}
		}

		else {
			var f = files.FirstOrDefault(); //todo

			if (!string.IsNullOrWhiteSpace(f)) {
				Tf_Input.DeleteAll();
				Debug.WriteLine($"Picked file: {f}", nameof(Browse_Clicked));
				Cancel_Clicked();
				Restart_Clicked(true);
				SetInputText(f);
				Btn_Run.SetFocus();

			}

		}

		if (Compat.IsWin) {
			Integration.KeepOnTop(Client.Config.OnTop);

		}
	}

	/// <summary>
	///     <see cref="Lbl_InputInfo" />
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

	private void Clear_Clicked()
	{
		Tf_Input.ReadOnly = false;
		Tf_Input.DeleteAll();
		Lbl_InputOk.SetLabelStatus(null);
		Lbl_InputOk.SetNeedsDisplay();
		Lbl_InputInfo.Text  = ustring.Empty;
		Lbl_InputInfo2.Text = ustring.Empty;
		Lbl_Status2.Text    = ustring.Empty;
		Btn_Run.Enabled     = true;
		// Btn_Run.Enabled     = false;
		Tf_Input.SetFocus();
		Btn_Delete.Enabled = false;
		IndexColors.Clear();
		m_clipboard.Clear();

	}

	private void Cancel_Clicked()
	{
		m_token.Cancel();
		m_tokenu.Cancel();
		Lbl_Status2.ColorScheme = UI.Cs_Lbl4;
		Lbl_Status2.Text        = R2.Inf_Cancel;
		Lbl_Status2.SetNeedsDisplay();
		Btn_Restart.Enabled = true;
		Application.MainLoop.RemoveIdle(m_runIdleTok);
		Tv_Results.SetFocus();
		Lbl_Status2.ColorScheme = UI.Cs_Lbl1;
		m_token.Dispose();
		m_token = new();

	}

	private void Delete_Clicked()
	{
		try {
			Clipboard.Close();
			// Restart_Clicked(true);

			var file = Tf_Input.Text.ToString();

			if (!string.IsNullOrWhiteSpace(file)) {
				file = file.CleanString();
				Query.Dispose();
				// Restart_Clicked(true);
				Debug.WriteLine($"{IsQueryReady()}");

				Microsoft.VisualBasic.FileIO.FileSystem.DeleteFile(file, UIOption.OnlyErrorDialogs,
				                                                   RecycleOption.SendToRecycleBin);
				Debug.WriteLine($"deleted {file}");
				// Clear();

				// Btn_Delete.Enabled = false;

				if (QueueMode) {
					Next_Clicked();
				}
				else {
					Cancel_Clicked();
					Clear_Clicked();

				}

			}

		}
		catch (Exception e) {
			AppInfo.ExceptionLog(e);

		}

	}

	private void Next_Clicked()
	{
		if (Client.IsRunning) {
			Cancel_Clicked();
		}

		// Dispose(false);

		Restart_Clicked(true);

		NextQueue();
	}

	private void NextQueue()
	{
		var tryDequeue = Queue.TryDequeue(out var n);

		if (tryDequeue) {
			// SetQuery(n);

			SetInputText(n);
			Btn_Queue.Text = $"Queue ({Queue.Count})";
		}
	}

	private static int Norm(int n, int n2 = 0) => n == INV ? n2 : n;

	#region

	/// <summary>
	///     <see cref="Tv_Results" />
	/// </summary>
	private void ResultTable_CellActivated(TableView.CellActivatedEventArgs args)
	{
		if (args.Table is not { }) {
			return;
		}

		try {
			var rows = args.Table.Rows;
			var cell = rows[args.Row][args.Col];

			if (cell is Url { } u) {
				HttpUtilities.TryOpenUrl(u);
			}

			var key = rows[args.Row][COL_URL];

			if (args.Col == 0 && Downloaded.ContainsKey(key)) {
				FileSystem.ExploreFile(Downloaded[key]);
			}

		}
		catch (Exception e) {
			Debug.WriteLine($"{e.Message}", nameof(ResultTable_CellActivated));
		}
	}

	private static ColorScheme ResultTable_RowColor(TableView.RowColorGetterArgs r)
	{
		// var ar = r.Table.Rows[r.RowIndex];

		// GC.TryStartNoGCRegion(10);

		if (IndexColors.TryGetValue(r.Table.Rows[r.RowIndex], out ColorScheme? cs)) { }
		else {
			cs = new ColorScheme();
		}

		// GC.EndNoGCRegion();
		return cs;

	}

	private async void ResultTable_KeyPress(View.KeyEventEventArgs eventArgs)
	{
		var kek = eventArgs.KeyEvent.Key;

		var k = kek & ~Key.CtrlMask;
		var (r, c) = (Tv_Results.SelectedRow, Tv_Results.SelectedColumn);

		(r, c) = (Norm(r), Norm(c));

		// NOTE: Column 2 contains the URL
		//index >= 0 && index < array.Length
		if (!(r >= 0 && r < Tv_Results.Table.Rows.Count)) {
			return;
		}

		var ctr = Tv_Results.Table.Rows;
		var cr  = ctr[r];
		Url v   = (cr[COL_URL]).ToString();

		switch (k) {
			case Key.D:
				eventArgs.Handled = true;
				string path;

				if (v.PathSegments is { Count: >= 1 }) {
					path = $"{v.PathSegments[^1]}";

				}
				else path = v.Path;

				var  path2 = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), path);
				bool ok    = false;

				if (File.Exists(path2) || Downloaded.ContainsKey(v)) {
					ok = true;
					goto open;
				}

				var u = await UniSource.TryGetAsync(v, whitelist: FileType.Image);

				if (u != null && u.FileTypes.Any()) {
					var f = File.OpenWrite(path2);

					if (u.Stream.CanSeek) {
						u.Stream.Position = 0;

					}

					await u.Stream.CopyToAsync(f);
					// f.Close();
					f.Dispose();
					u.Dispose();
					ok = true;

				}
				else {
					ok = false;
				}

				open:
				ustring status = ok ? UI.OK : UI.Err;
				string? val;

				if (ok) {
					// Novus.OS.FileSystem.ExploreFile(path2);
					val = path2;
				}
				else {
					val = null;
				}

				Downloaded.TryAdd(Tv_Results.Table.Rows[r][COL_URL], val);

				Tv_Results.Table.Rows[r][COL_STATUS] = $"{status}";

				break;
			case Key.M:
				eventArgs.Handled = true;

				var sri = FindResultItemForUrl(v);

				dynamic? d = sri?.Metadata;

				if (d is Array { Length: > 0 } dr) {

					var dl = new Dialog
					{
						Title    = "Metadata",
						AutoSize = false,
						Width    = Dim.Percent(60),
						Height   = Dim.Percent(45),
						/*Border = new Border()
						{
							// Background = default
						}*/
						// Height   = UI.Dim_80_Pct,
					};

					var lv = new ListView(dr)
					{
						Width  = Dim.Fill(),
						Height = Dim.Fill(),
						Border = new Border
						{
							BorderStyle     = BorderStyle.Rounded,
							BorderThickness = new Thickness(2)
						}
					};

					lv.OpenSelectedItem += args =>
					{
						var i = args.Value.ToString();
						HttpUtilities.TryOpenUrl(i);
					};
					dl.Add(lv);

					var btnOk = new Button("Ok")
					{
						ColorScheme = UI.Cs_Btn3
					};
					btnOk.Clicked += () => { Application.RequestStop(); };
					dl.AddButton(btnOk);
					Application.Run(dl);
				}
				else if (d is ChanPost p) {
					var dl = new Dialog
					{
						Title    = "Metadata",
						AutoSize = false,
						Width    = Dim.Percent(60),
						Height   = Dim.Percent(45),
						/*Border = new Border()
						{
							// Background = default
						}*/
						// Height   = UI.Dim_80_Pct,
						Text = p.Text
					};

					var btnOk = new Button("Ok")
					{
						ColorScheme = UI.Cs_Btn3
					};
					btnOk.Clicked += () => { Application.RequestStop(); };
					dl.AddButton(btnOk);
					Application.Run(dl);
				}

				break;
			case Key.S:
				//TODO: WIP
				eventArgs.Handled = true;

				sri = FindResultItemForUrl(v);

				if (sri == null) {
					break;
				}

				UniSource[] ih;

				if (!Binary.ContainsKey(sri)) {
					(int g, int s) = ParseDataRowGroup(cr);

					var ok1 = await sri.LoadUniAsync(m_token.Token);

					if (ok1 && sri.HasUni) {
						ih = sri.Uni;
					}
					else {
						break;
					}

					Binary.TryAdd(sri, ih);
					var sriDataRow = FindRowsForResult(sri.Root);
					var crIdx      = Array.IndexOf(sriDataRow, cr);
					var dtIdx      = Dt_Results.Rows.IndexOf(sriDataRow[crIdx]);

					/*(int ii, int jj) = ParseDataRowGroup(cr);

					var crg = sriDataRow.Where(x =>
					{
						var (i, j) = ParseDataRowGroup(x);
						return i == ii;
					});*/

					var xc = Dt_Results.Rows.Count;

					// Todo: this is really inefficient and unoptimized

					for (int j = 0; j < ih.Length; j++) {
						var sriOfIh = new SearchResultItem(sri.Root)
						{
							Url = ih[j].Value.ToString(),
							Uni = new[] { ih[j] }
						};
						sri.Sisters.Add(sriOfIh);

						var nr    = CreateRowForResultItem(sriOfIh, g,  s);
						var cs    = GetEngineColorScheme(sriOfIh.Root.Engine);
						var nrIdx = dtIdx + (j + 1);
						IndexColors[nr] = cs;

						Dt_Results.Rows.InsertAt(nr, nrIdx);
						Tv_Results.Update();
					}

					return;
				}
				else {
					ih = Binary[sri];
				}

				if (!ih.Any()) {
					break;
				}

				var dl2 = new Dialog
				{
					Title    = "Direct Images",
					AutoSize = false,
					Width    = Dim.Percent(80),
					Height   = Dim.Percent(75),
					/*Border = new Border()
					{
						// Background = default
					}*/
					// Height   = UI.Dim_80_Pct,
				};

				var dt2 = new DataTable()
				{
					Columns =
					{
						{ "Source" },
						{ "Url", typeof(Url) },
						{ "Info" }
					}
				};

				var e = Binary.GetEnumerator();

				while (e.MoveNext()) {
					var (kk, vv) = e.Current;

					foreach (UniSource uv in vv) {
						dt2.Rows.Add(kk.Root.Engine.Name, (Url) uv.Value.ToString(), $"{uv.FileTypes[0]}");
					}
				}

				var lv2 = new TableView(dt2)
				{
					Width  = Dim.Fill(),
					Height = Dim.Fill(),
					Border = new Border
					{
						BorderStyle     = BorderStyle.Rounded,
						BorderThickness = new Thickness(2)
					}
				};

				lv2.CellActivated += args =>
				{
					var i = (Url) args.Table.Rows[args.Row][1];
					HttpUtilities.TryOpenUrl(i);
				};
				dl2.Add(lv2);

				var btnOk2 = new Button("Ok")
				{
					ColorScheme = UI.Cs_Btn3
				};
				btnOk2.Clicked += () => { Application.RequestStop(); };
				dl2.AddButton(btnOk2);
				Application.Run(dl2);
				e.Dispose();
				break;
			default:
				eventArgs.Handled = false;
				break;
		}
	}

	private static (int ii, int jj) ParseDataRowGroup(DataRow cr)
	{
		var engSplit = cr[0].ToString().Split(' ');
		var eng      = engSplit[0];
		var ij       = (engSplit[1][1..].Split('.'));
		var ijk = (int.Parse(ij[0]), ij.Length > 1 ? int.Parse(ij[1]) : 0);
		return ijk;
	}

	#endregion

#if ALT
	private async void Reload_Clicked()
	{
		var q = Query;
		Cancel_Clicked();
		Restart_Clicked(true);
		// Query = q;
		// SetQuery(q.Uni.Value.ToString());
	}

	private static readonly ConcurrentDictionary<Url, ResultMeta> Scanned = new();

	private static ConcurrentDictionary<Url, bool> sx = new();

	public sealed record ResultMeta : IDisposable
	{
		public Url Url { get; init; }

		public string Message { get; set; }

		public UniSource[] Sources { get; set; }

		public void Dispose()
		{
			for (int i = 0; i < Sources.Length; i++) {
				Sources[i].Dispose();
			}
		}
	}

	private async void OnResultKeyPress(View.KeyEventEventArgs eventArgs)
	{
		var kek = eventArgs.KeyEvent.Key;
		Debug.WriteLine($"{eventArgs.KeyEvent} {eventArgs.KeyEvent.IsCtrl} {kek}");
		var k = kek & ~Key.CtrlMask;
		var (r, c) = (Tv_Results.SelectedRow, Tv_Results.SelectedColumn);

		// NOTE: Column 1 contains the URL
		c = 1;
		Url v = (Tv_Results.Table.Rows[r][c]).ToString();

		if (!Message.ContainsKey(v)) {
			Message[v] = $"?";

		}

		switch (k) {
			case Key.S:
			{
				if (await ScanResult(v)) return;

				break;
			}
			case Key.X:
				//TODO: WIP

				if (_keyPressHandling) {
					return;
				}

				_keyPressHandling = true;

				var res = m_results.SelectMany(e => e.Results).ToArray();
				var dr = new ConcurrentBag<SearchResultItem>();
				int ca = 0, cf = 0;
				Pbr_Status.Fraction = 0;
				Pbr_Status.ProgressBarStyle = ProgressBarStyle.MarqueeContinuous;

				await Parallel.ForEachAsync(res, async (result, token) =>
				{
					var u = await result.GetUniAsync();

					if (u) {
						dr.Add(result);
					}
					else {
						cf++;
						result?.Dispose();
					}

					Pbr_Status.Fraction = (((float) dr.Count) / (res.Length - cf));

					Pbr_Status.Pulse();

					return;
				});
				Lbl_Status2.Text = $"{dr.Count} | {res.Length}";

				var d1 = new Dialog()
				{
					Title = $"",
					AutoSize = false,
					Width = Dim.Percent(60),
					Height = Dim.Percent(55),
					// Height   = UI.Dim_80_Pct,
				};
				var drCpy = dr.ToArray();

				var lv1 = new ListView(drCpy)
				{
					Width = Dim.Fill(),
					Height = Dim.Percent(80),
					Border = new Border()
					{
						BorderStyle = BorderStyle.Rounded,
						BorderThickness = new Thickness(2)
					}
				};

				var bt = new Button("Ok") { };
				bt.Clicked += () => { Application.RequestStop(); };
				d1.Add(lv1);
				d1.AddButton(bt);
				lv1.SetFocus();
				Application.Run(d1);

				foreach (SearchResultItem item in dr) {
					item.Dispose();
				}

				foreach (SearchResultItem sr in drCpy) {
					sr.Dispose();
				}

				break;
			case Key.D:
				//TODO: WIP

				if (sx.TryGetValue(v, out var b) && b) {
					return;
				}

				sx.TryAdd(v, true);
				Message[v] = "Resolving";

				Pbr_Status.Pulse();
				var u = await UniSource.TryGetAsync(v, whitelist: FileType.Image);

				if (u != null && u.FileTypes.Any()) {
					var path = $"{v.PathSegments[^1]}";
					var path2 = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), path);

					if (File.Exists(path2)) {
						goto open;
					}

					var f = File.OpenWrite(path2);

					if (u.Stream.CanSeek) {
						u.Stream.Position = 0;

					}

					await u.Stream.CopyToAsync(f);
					// f.Close();
					f.Dispose();
					open:
					u.Dispose();
					Novus.OS.FileSystem.ExploreFile(path2);
					Message[v] = $"Downloaded";
				}
				else {
					Message[v] = "Invalid";
				}

				// Pbr_Status.Pulse();

				sx.TryRemove(v, out var x);

				break;
		}

		eventArgs.Handled = true;
	}

	private static async Task<bool> ScanResult(Url v)
	{

		if (sx.ContainsKey(v)) {
			Lbl_Status2.Text = $"Scanning...!";
			Lbl_Status2.SetNeedsDisplay();
			return true;
		}
		else {
			Lbl_Status2.Text = $"Scanning started...!";
			Lbl_Status2.SetNeedsDisplay();
			sx.TryAdd(v, true);

		}

		if (Scanned.ContainsKey(v)) {
			//todo
			Lbl_Status2.Text = Scanned[v].Message;
			Lbl_Status2.SetNeedsDisplay();
		}
		else {
			UniSource[] urls = null;

			urls = await NetUtil.ScanAsync(v);

			Scanned.TryAdd(v, new ResultMeta() { Url = v, Sources = urls, Message = "..." });

			Lbl_Status2.Text = Scanned[v].Message;
			Lbl_Status2.SetNeedsDisplay();

			var d = new Dialog()
			{
				Title = $"",
				AutoSize = false,
				Width = Dim.Percent(60),
				Height = Dim.Percent(55),
				// Height   = UI.Dim_80_Pct,
			};

			var lv = new ListView(Scanned[v].Sources.Select(e => $"{e.FileTypes[0]} {e.Value}").ToArray())
			{
				Width = Dim.Fill(),
				Height = Dim.Fill(),

				Border = new Border()
				{
					BorderStyle = BorderStyle.Rounded,
					BorderThickness = new Thickness(2)
				}
			};
			d.Add(lv);
			Application.Run(d);

			// var rmi = Scanned[v];
			// Scanned[v]          = urls;
			// Pbr_Status.Fraction = 0;

		}

		return false;
	}

	private void OnCellSelected(TableView.SelectedCellChangedEventArgs eventArgs)
	{
		// TODO: WIP
		var nr = (eventArgs.NewRow == INV ? eventArgs.OldRow : eventArgs.NewRow);
		var nc = eventArgs.NewCol == INV ? eventArgs.OldCol : eventArgs.NewCol;
		nr = Norm(nr);
		nc = Norm(nc);

		var cell = eventArgs.Table.Rows[nr][nc];
		var cell2 = eventArgs.Table.Rows[nr][INDEX];

		if (Message.ContainsKey(cell)) {
			Lbl_Status2.Text = Message[cell];
		}

		else if (Message.ContainsKey(cell2)) {
			Lbl_Status2.Text = Message[cell2];
		}
	}
#endif
}