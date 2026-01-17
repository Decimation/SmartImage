// Read S SmartImage.Rdx SearchCommand.cs
// 2023-07-05 @ 2:07 AM

// ReSharper disable RedundantUsingDirective.Global
// ReSharper disable InconsistentNaming

#region Global usings

global using ISImage = SixLabors.ImageSharp.Image;
global using CBN = JetBrains.Annotations.CanBeNullAttribute;
global using INN = JetBrains.Annotations.ItemNotNullAttribute;
global using AC = Spectre.Console.AnsiConsole;
global using AnsiConsole = Spectre.Console.AnsiConsole;
global using MN = System.Diagnostics.CodeAnalysis.MaybeNullAttribute;
global using MNNW = System.Diagnostics.CodeAnalysis.MemberNotNullWhenAttribute;
global using MURV = JetBrains.Annotations.MustUseReturnValueAttribute;
global using NN = System.Diagnostics.CodeAnalysis.NotNullAttribute;
global using R1 = SmartImage.Lib.Resources;
global using R2 = SmartImage.Rdx.Resources;

#endregion

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.Caching;
using System.Runtime.CompilerServices;
using Kantan.Text;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp.Processing;
using SmartImage.Lib;
using SmartImage.Lib.Engines.Results;
using SmartImage.Lib.Images;
using SmartImage.Lib.Utilities;
using SmartImage.Lib.Utilities.Diagnostics;
using SmartImage.Lib.Utilities.Integration;
using SmartImage.Rdx.Commands.Common;
using SmartImage.Rdx.Shell;
using SmartImage.Shared;
using Spectre.Console;
using Spectre.Console.Cli;
using Spectre.Console.Rendering;
using Size = SixLabors.ImageSharp.Size;

// ReSharper disable ArrangeObjectCreationWhenTypeNotEvident

// TODO: Create separate SearchCommands for interactive/non-interactive

[assembly: InternalsVisibleTo(Common.PROJ_SMARTIMAGE_LIB_UNITTEST)]

namespace SmartImage.Rdx.Commands.Search;

#nullable disable

public sealed partial class SearchCommand : CommonAsyncCommand<SearchCommandSettings>
{

	public SearchClient Client { get; private set; }

	public SearchQuery Query { get; private set; }


	private readonly CancellationTokenSource m_cts;

	private readonly CancellationTokenSource m_ctsRun;

	/// <summary>
	/// Key: <see cref="SearchResult"/>
	/// Value: <see cref="m_mainTable"/> index
	/// </summary>
	private readonly ConcurrentDictionary<SearchResult, SpcTable> m_resultTables;

	private readonly MemoryCache m_previewCanvasCache;

	private SpcTable m_mainTable;

	// private readonly ConcurrentDictionary<SearchResult, SelectionPrompt<SearchResultItem>> m_prompts = new();

	private Layout m_layout;

	private static readonly ILogger s_logger = AppSupport.Factory.CreateLogger(nameof(SearchCommand));

	static SearchCommand() { }

	public SearchCommand()
	{
		m_cts    = new CancellationTokenSource();
		m_ctsRun = new CancellationTokenSource();

		// m_results      = new();
		m_resultTables       = new ConcurrentDictionary<SearchResult, SpcTable>();
		m_previewCanvasCache = new MemoryCache("Buf");

		Query = SearchQuery.Null;
	}

#region

	private async Task InitQueryAsync(ProgressContext ctx)
	{
		var p = ctx.AddTask("Creating query");
		p.IsIndeterminate = true;

		Query = await SearchQuery.TryCreateAsync(CommandSettings.Query);

		if (Query == SearchQuery.Null) {
			throw new SmartImageException($"Could not create query {Query}");
		}

		p.Increment(Elements.COMPLETE / 2);

		// ctx.Refresh();

		p.Description = "Uploading query";
		var url = await Query.TryUploadAsync();

		if (!url) {
			throw new SmartImageException($"Could not upload {Query}");
		}

		p.Increment(Elements.COMPLETE / 2);

	}

	/// <inheritdoc />
	protected override void InitConfig(SearchCommandSettings scs)
	{
		Config = new SearchConfig();

		base.InitConfig(scs);

		Client = new SearchClient(Config);

		m_mainTable = CommandSettings.Interactive ? Renderables.CreateMainTable() : Renderables.CreateFullResultTable();

	}

	public override async Task<int> ExecuteAsync(CommandContext context, SearchCommandSettings settings, CancellationToken cancellationToken)
	{
		InitConfig(settings);

		Console.CancelKeyPress += OnCancelKeyPress;

		var initTask = AnsiConsole.Progress()
			.AutoRefresh(true)
			.StartAsync(InitQueryAsync);

		await initTask;

		var queryCi = new CanvasImage(Query.Source.GetStream());

		var ciPanel = new Panel(queryCi)
		{
			Header = new PanelHeader($"{Query.Source.Value}"),
			Expand = true,
		};

		var cfgGrid = Renderables.CreateConfigGrid(Config, Query);

		var cfgPanel = new Panel(cfgGrid) { Header = new PanelHeader("Search Options") { } };

		m_layout = new Layout("Root").SplitColumns(
			new Layout("L").SplitRows(
				new("LC", cfgPanel),
				new("LT", m_mainTable)
			),
			new Layout("R", ciPanel) { }
		);

		// AnsiConsole.Write(m_layout);

		try {

			IRenderable elem = CommandSettings.Interactive ? m_layout : m_mainTable;

			Task main = AnsiConsole.Live(elem)
				.StartAsync(c => RunSearchLiveAsync(c, m_ctsRun.Token));

			await main;
		}
		catch (OperationCanceledException e) {
			s_logger.LogError(e, "Canceled");
		}

		if (CommandSettings.HasCommand) {
			await RunCompletionCommandAsync(m_cts.Token);
		}

		if (CommandSettings.HasOutputFile) {
			switch (CommandSettings.OutputFileFormat) {

				case OutputFileFormat.None:
					break;

				case OutputFileFormat.Delimited:
					WriteOutputFile();
					break;

				default:
					throw new ArgumentOutOfRangeException();
			}

		}

		if (CommandSettings.Interactive) {
			await RunInteractiveAsync(m_cts.Token);
		}

		if (CommandSettings.KeepOpen) {
			await AnsiConsole.ConfirmAsync("Exit", cancellationToken: m_cts.Token);
		}

		return BaseOSIntegration.EC_OK;
	}


	private async Task RunSearchLiveAsync(LiveDisplayContext c, CancellationToken ct = default)
	{

#if UNITTEST
		return;
#endif

		var search = Client.RunSearchAsync(Query, token: ct);

		while (await Client.ResultChannel.Reader.WaitToReadAsync(ct)) {
			var task = Client.ResultChannel.Reader.ReadAsync(ct);

			var result = await task;

			var fullRows = result.GetFullResultRows();

			if (CommandSettings.Interactive) {
				var table = Renderables.CreateFullResultTable();

				foreach (IRenderable[] row in fullRows) {
					table.AddRow(row);
				}

				m_resultTables.TryAdd(result, table);

				/*var prompt = new SelectionPrompt<SearchResultItem>()
				{
					Converter = static r =>
					{
						return r.Url;
					},
					Mode = SelectionMode.Independent,
					SearchEnabled = true,
				};
				prompt.AddChoices(result.Results);

				m_prompts.TryAdd(result, prompt);*/

				m_mainTable.AddRow(result.GetMainRows());
				Elements.Prm_SearchResult.AddChoice(result);
			}
			else {
				foreach (IRenderable[] row in fullRows) {
					m_mainTable.AddRow(row);
				}
			}

			c.Refresh();

		}

		await search;
	}

	private async Task RunInteractiveAsync(CancellationToken ct = default)
	{
		string       cmd      = null;
		bool         clrWrite = true;
		SpcTable     srTable  = null;
		SearchResult sr       = null;

		do {

			AnsiConsole.Clear();
			AnsiConsole.Write(m_layout);

			sr = AnsiConsole.Prompt(Elements.Prm_SearchResult);

			srTable = m_resultTables[sr];

			clrWrite = true;

			do {
				if (clrWrite) {
					AnsiConsole.Clear();
					AnsiConsole.Write(srTable);
				}

				cmd = AnsiConsole.Prompt(Elements.Prm_Command);

				if (cmd == R2.Chc_Exit) {
					return;
				}

				if (cmd == R2.Chc_Back) {
					break;
				}

				// var selx = AC.Prompt(m_prompts[sr]);

				var sel     = ShellSelection.GetSelectionChoice(sr);
				var sri     = sel.Item;
				var selIdx  = sel.Index();
				var selIdx2 = sel.Index2();

				s_logger.LogDebug("Selected {Item} {Scn} | {Idx1}, {Idx2}", sel.Item, sel.IsScannedItem, selIdx, selIdx2);

				if (cmd == R2.Chc_Open) {
					SearchClient.OpenResult(sri.Url);
					clrWrite = false;
					continue;
				}

				if (cmd == R2.Chc_Scan && !sel.IsScannedItem) {
					await AnsiConsole.Live(srTable).StartAsync(async (f) =>
					{
						s_logger.LogTrace("Scanning {Item}", sri);
						bool scannedOk = false;
						scannedOk = await sri.ScanAsync(m_ctsRun.Token);

						if (!scannedOk) {
							return;
						}

						for (int i = 0; i < sri.ScannedItems.Count; i++) {
							SearchResultItem scnItm = sri.ScannedItems[i];
							scnItm.CalculateSimilarity(Query.Source);

							var scnRow = scnItm.GetItemRow(sel.ItemIdx, i);
							srTable.InsertRow(selIdx + i + 1, scnRow);
						}

						// m_prompts[sr].AddChoiceGroup(sri, sri.ScannedItems);

						f.Refresh();


					});
					clrWrite = true;
					continue;
				}

				if (cmd == R2.Chc_Calc && sri.HasHash) {

					AnsiConsole.Live(srTable).Start(f =>
					{
						sri.CalculateSimilarity(Query.Source);

						srTable.Rows.Update(selIdx2, (int) ResultRowIndex.ROW_SIMILARITY, sri.GetSimilarity());
						f.Refresh();
					});


					clrWrite = true;
					continue;
				}

				if (cmd == R2.Chc_Preview) {
					if (!sri.HasBytes || !sri.HasImage) {
						continue;
					}

					var ci = GetPreview(sri);

					AnsiConsole.AlternateScreen(() => { ShowPreview(ci, sri); });
					clrWrite = true;
				}

				if (cmd == R2.Chc_Download) {

					HandleDownload(sri);
				}


			} while (cmd != R2.Chc_Back && !ct.IsCancellationRequested);

		} while (cmd != R2.Chc_Exit && !ct.IsCancellationRequested);
	}

#endregion

#region

	private CanvasImage GetPreview(SearchResultItem sri)
	{
		Stream str = null;

		var cip = new CacheItemPolicy()
		{
			AbsoluteExpiration = DateTimeOffset.Now + TimeSpan.FromMinutes(1),
			RemovedCallback = arguments =>
			{
				switch (arguments.RemovedReason) {

					case CacheEntryRemovedReason.Removed:
						break;

					case CacheEntryRemovedReason.Expired:
						break;

					case CacheEntryRemovedReason.Evicted:
						break;

					case CacheEntryRemovedReason.ChangeMonitorChanged:
						break;

					case CacheEntryRemovedReason.CacheSpecificEviction:
						break;

					default:
						throw new ArgumentOutOfRangeException();
				}

				s_logger.LogDebug("Cache item {CacheItem} removed: {RemRes}", arguments.CacheItem.Key, arguments.RemovedReason);
				return;
			}
		};

		// string key = sri.Url.ToString();

		var key = sri.Url;
		var val = m_previewCanvasCache.Get(key);

		var ci = val as CanvasImage;

		if (ci is null) {
			str = sri.GetStream();
			ci  = new CanvasImage(str) { };


			m_previewCanvasCache.Set(key, ci, cip);
		}
		else { }

		Trace.Assert(ci != null);

		return ci;
	}

	private void ShowPreview(CanvasImage ci, SearchResultItem sri)
	{
		var (w, h) = (AnsiConsole.Profile.Width, AC.Profile.Height);

		var pnl        = new Panel(ci) { Expand = true, Border = BoxBorder.None, Header = new PanelHeader($"{sri.Value}")};
		/*var sriLayout = new Layout("Info");

		sriLayout.SplitColumns(
			new Layout("Metadata"),
			new Layout("Image")
		);


		var grid = sri.GetInfoGrid();
		sriLayout["Metadata"].Update(grid);
		sriLayout["Image"].Update(ci);*/

		AnsiConsole.Live(pnl).Start(ldc =>
		{
			while (true) {
				// ci.MaxWidth = mw < 0 ? null : mw;
				ldc.Refresh();
				var cki = AnsiConsole.Console.Input.ReadKey(true);

				if (!cki.HasValue) {
					continue;
				}

				switch (cki.Value.Key) {


					case ConsoleKey.S:
						ci.Mutate(act => { act.Resize(sri.Image.Width, sri.Image.Height); });
						break;

					case ConsoleKey.M:
						ci.MaxWidth = w;
						break;

					case ConsoleKey.R:
						ci.Mutate(act =>
						{
							var cs = act.GetCurrentSize();

							var cs2 = cs.ResizeByFactor(new Size(w, h));
							act.Resize(cs2);
						});

						ci.MaxWidth = null;
						continue;

					case ConsoleKey.Escape:
						return;

				}
			}
		});


		return;
	}

#endregion


	// [ContractAnnotation("=> halt")]
	private static void HandleDownload(SearchResultItem sri)
	{
		var ok = sri.TryWriteOrGetFile();

		if (ok) {
			AC.AlternateScreen(() =>
			{
				var gr = new Grid();
				gr.AddColumns(new GridColumn[] { new(), new() });
				gr.AddRow(new IRenderable[] { new Text("File", Elements.Sty_Name), new Text(sri.LocalFilePath) });
				AnsiConsole.Write(gr);

				var prompt = new ConfirmationPrompt("Open?") { };
				var choice = AnsiConsole.Prompt(prompt);

				if (choice) {
					var proc = Process.Start(new ProcessStartInfo()
					{
						FileName         = sri.LocalFilePath,
						WorkingDirectory = String.Empty,
						UseShellExecute  = true
					});

					proc?.WaitForExit();
					proc?.Dispose();
				}
			});
		}
	}

	private void OnCancelKeyPress(object sender, ConsoleCancelEventArgs args)
	{
		// AnsiConsole.MarkupLine($"[red]Cancellation requested[/]");
		// AnsiConsole.MarkupLine($"[red]Sender: {sender}[/]");

		s_logger.LogTrace("Cancellation requested {Sender} {Args}", sender, args);

		// AnsiConsole.Clear();

		m_ctsRun.Cancel();
		m_cts.Cancel();
		m_cts.TryReset();
		m_ctsRun.TryReset();

		args.Cancel = true;

		// Environment.Exit(BaseOSIntegration.EC_ERROR);
	}

	public override ValidationResult Validate(CommandContext context, SearchCommandSettings settings)
	{
		var r = base.Validate(context, settings);
		return r;
	}

	public override void Dispose()
	{
		s_logger.LogDebug("Disposing search command");

		foreach (var sr in m_resultTables.Keys) {
			sr.Dispose();
		}

		Elements.Prm_Selection.Validator = null;

		m_resultTables.Clear();
		m_cts.Dispose();
		m_ctsRun.Dispose();
		m_previewCanvasCache.Dispose();
		Client.Dispose();
		Query.Dispose();
	}

}