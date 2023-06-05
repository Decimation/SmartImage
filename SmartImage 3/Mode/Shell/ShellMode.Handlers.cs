// Read S SmartImage ShellMode.Handlers.cs
// 2023-02-14 @ 12:13 AM

using Color = Terminal.Gui.Color;
using System.Collections;
using System.Collections.Concurrent;
using System.Data;
using System.Diagnostics;
using System.Reflection;
using AngleSharp.Dom;
using Kantan.Net.Utilities;
using Kantan.Text;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic.FileIO;
using Novus.OS;
using NStack;
using SmartImage.App;
using SmartImage.Lib;
using SmartImage.Mode.Shell.Assets;
using Terminal.Gui;
using Clipboard = Novus.Win32.Clipboard;
using Microsoft.VisualBasic.FileIO;
using Novus.FileTypes;
using Novus.Win32.Structures.User32;
using SmartImage.Lib.Engines;
using SmartImage.Lib.Results;
using SmartImage.Lib.Utilities;
using Attribute = Terminal.Gui.Attribute;
using Command = Terminal.Gui.Command;
using FileSystem = Novus.OS.FileSystem;

namespace SmartImage.Mode.Shell;

public sealed partial class ShellMode
{
	private const int INV = -1;

	private const int COL_URL    = 2;
	private const int COL_STATUS = 1;

	private static readonly ConcurrentDictionary<object, string> Downloaded = new();

	private async void Input_TextChanging(TextChangingEventArgs tc)
	{
		var text = tc.NewText.ToString().TrimStart('\"');

		// Debug.WriteLine($"testing {text}", nameof(Input_TextChanging));

		// Application.MainLoop.Invoke(() => Task.Delay(TimeSpan.FromSeconds(1)));

		var sourceType = SearchQuery.IsValidSourceType(text);

		if (sourceType) {
			// m_upload.WaitOne();
			/*if (_inputVerifying) {
				return;
			}*/

			var ok = await TrySetQueryAsync(text);

			Btn_Run.Enabled = ok;
			Debug.WriteLine($"{nameof(Input_TextChanging)} :: ok");

			// m_upload.Release();
			if (ok && Config.AutoSearch && !Client.IsRunning) {
				Run_Clicked();
			}
		}
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

		_keyPressHandling = false;
		_inputVerifying   = false;
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

	private static void Clear_Clicked()
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

			File.WriteAllText("crash.log", $"{e.Message} {e.Source} {e.StackTrace}");

		}
		finally {

			// Restart_Clicked(true);
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
		}
	}

	private static readonly Dictionary<BaseSearchEngine, ColorScheme> Colors = new()
		{ };

	private static int Norm(int n, int n2 = 0) => n == INV ? n2 : n;

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

	private ColorScheme? ResultTable_RowColor(TableView.RowColorGetterArgs r)
	{
		// var eng=args.Table.Rows[args.RowIndex]["Engine"];

		ColorScheme? cs = null;

		var ar = r.Table.Rows[r.RowIndex].ItemArray;

		var eng = ar[0];

		if (eng == null) {
			goto ret;
		}

		var eng2 = Client.Engines.FirstOrDefault(f => eng.ToString().Contains(f.Name));

		if (eng2 == null) {
			goto ret;
		}

		if (!Colors.ContainsKey(eng2)) {
			var colors = Enum.GetValues<Color>();

			var cc = colors[Array.IndexOf(UI.EngineOptions, eng2.EngineOption) % UI.EngineOptions.Length];

			Color cc2;

			switch (cc) {
				case Color.Cyan:
					cc2 = Color.BrightCyan;
					break;
				default:
					cc2 = cc;
					break;
			}

			cs = new ColorScheme()
			{
				Normal = Attribute.Make(cc, Color.Black),
				Focus  = Attribute.Make(cc2, Color.DarkGray),

			};
			cs = cs.NormalizeHot();

			Colors.Add(eng2, cs);
		}
		else {
			cs = Colors[eng2];
		}

		ret:
		return cs;
	}

	private async void ResultTable_KeyPress(View.KeyEventEventArgs eventArgs)
	{
		var kek = eventArgs.KeyEvent.Key;

		var k = kek & ~Key.CtrlMask;
		var (r, c) = (Tv_Results.SelectedRow, Tv_Results.SelectedColumn);

		// NOTE: Column 2 contains the URL

		Url v = (Tv_Results.Table.Rows[r][COL_URL]).ToString();

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

			case Key.X:
				//TODO: WIP
				break;
		}

		eventArgs.Handled = false;
	}

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