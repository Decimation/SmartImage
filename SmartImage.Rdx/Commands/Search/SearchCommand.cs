// Author: Deci | Project: SmartImage.Rdx | Name: SearchCommand.cs
// Date: 2025/12/27 @ 22:12:21

// ReSharper disable RedundantUsingDirective.Global
// ReSharper disable ArrangeObjectCreationWhenTypeNotEvident
// ReSharper disable InconsistentNaming

#region Global usings

global using ISImage = SixLabors.ImageSharp.Image;
global using CBN = JetBrains.Annotations.CanBeNullAttribute;
global using INN = JetBrains.Annotations.ItemNotNullAttribute;
global using AC = Spectre.Console.AnsiConsole;
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
using Microsoft.Extensions.Logging;
using SmartImage.Lib;
using SmartImage.Lib.Engines.Results;
using SmartImage.Lib.Utilities;
using SmartImage.Lib.Utilities.Diagnostics;
using SmartImage.Rdx.Commands.Common;
using SmartImage.Rdx.Shell;
using SmartImage.Shared;
using Spectre.Console;
using Spectre.Console.Cli;
using Spectre.Console.Rendering;

// ReSharper disable UseSymbolAlias

// TODO: Create separate SearchCommands for interactive/non-interactive?
// TODO: Create types representing shell UI state
// TODO: Create types ...

[assembly: InternalsVisibleTo(Common.PROJ_SMARTIMAGE_LIB_UNITTEST)]

namespace SmartImage.Rdx.Commands.Search;

#nullable disable

public sealed partial class SearchCommand : CommonAsyncCommand<SearchCommandSettings>
{

	private static readonly ILogger s_logger = AppSupport.Factory.CreateLogger(nameof(SearchCommand));

	private readonly CancellationTokenSource m_cts;
	private readonly CancellationTokenSource m_ctsRun;
	private readonly CancellationTokenSource m_ctsRunSearch;

	private readonly ConcurrentDictionary<SearchResult, ResultViewState> m_dialogs;

	private Layout m_layout;

	private SpcTable m_mainTable;

	public SearchClient Client { get; private set; }

	public SearchQuery Query { get; private set; }

	public SearchCommand()
	{
		m_cts          = new CancellationTokenSource();
		m_ctsRun       = new CancellationTokenSource();
		m_ctsRunSearch = new CancellationTokenSource();

		m_previewCanvasCache = new MemoryCache("PreviewCache");
		m_dialogs            = new ConcurrentDictionary<SearchResult, ResultViewState>();
		Query                = SearchQuery.Null;
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

		p.Description = "Uploading query";
		var url = await Query.TryUploadAsync(Client.UploadEngine);

		if (!url) {
			throw new SmartImageException($"Could not upload {Query}");
		}

		p.Increment(Elements.COMPLETE / 2);

	}

	protected override void InitConfig(SearchCommandSettings scs)
	{
		Config = new SearchConfig();
		base.InitConfig(scs);

		Client = new SearchClient(Config);

		m_mainTable        = CommandSettings.Interactive ? Renderables.CreateOverviewTable() : Renderables.CreateResultTable();
		m_mainTable.Expand = true;
	}

	public override async Task<int> ExecuteAsync(CommandContext context, SearchCommandSettings settings, CancellationToken cancellationToken)
	{
		InitConfig(settings);

		Console.CancelKeyPress += OnCancelKeyPress;

		var initTask = AnsiConsole.Progress().AutoRefresh(true).StartAsync(InitQueryAsync);

		await initTask;

		m_layout = CreateLayout();

		try {

			IRenderable elem = CommandSettings.Interactive ? m_layout : m_mainTable;

			Task main = AnsiConsole.Live(elem).StartAsync(c => RunSearchLiveAsync(c, m_ctsRunSearch.Token));

			await main;
		}
		catch (OperationCanceledException e) {
			s_logger.LogError(e, "Canceled");
		}

		if (CommandSettings.HasCommand) {
			await RunCompletionCommandAsync(m_cts.Token);
		}

		if (CommandSettings is { HasOutputFile: true, OutputFileFormat: OutputFileFormat.Delimited }) {
			WriteOutputFile();
		}

		if (CommandSettings.Interactive) {
			await RunInteractiveAsync(m_cts.Token);
		}

		if (CommandSettings.KeepOpen) {
			await AnsiConsole.ConfirmAsync("Exit", cancellationToken: m_cts.Token);
		}

		return Shared.Common.EC_OK;
	}

#region

	private async Task ContinueInteractive(Task<SearchResult> task)
	{
		var result = await task;

		m_dialogs.TryAdd(result, ResultViewState.Create(result));

		m_mainTable.AddRow(result.GetMainRows());
		Elements.Prm_SearchResult.AddChoice(result);
	}

	private async Task ContinueNonInteractive(Task<SearchResult> task)
	{
		var result = await task;

		var fullRows = result.GetFullRows();

		foreach (IRenderable[] row in fullRows) {
			m_mainTable.AddRow(row);
		}
	}

	private async Task RunSearchLiveAsync(LiveDisplayContext c, CancellationToken ct = default)
	{
		var search = Client.RunSearchAsync(Query, token: ct);

		while (!ct.IsCancellationRequested && await Client.ResultChannel.Reader.WaitToReadAsync(ct)) {
			var task = Client.ResultChannel.Reader.ReadAsync(ct).AsTask();

			var task2 = CommandSettings.Interactive ? task.ContinueWith(ContinueInteractive, ct) : task.ContinueWith(ContinueNonInteractive, ct);

			await task2;

			c.Refresh();

		}

		await search;
	}

#endregion

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

			var dialog = m_dialogs[sr];
			srTable  = dialog.Table;
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

				/*var sri     = AC.Prompt(m_prompts[sr]);
				var itemIdx = sr.Results.IndexOf(sri);
				var selIdx2 = ShellSelection.GetIndex2(sri);*/

				var sel     = ShellSelection.GetSelectionChoice(sr); 
				var item    = sel.Item;
				var sri     = item as SearchResultItem;
				var selIdx  = sel.Index();
				var selIdx2 = sel.Index2();

				s_logger.LogDebug("Selected {Item} {Scn} | {Idx1}, {Idx2}", sel.Item, sel.IsScannedItem, selIdx, selIdx2);

				if (cmd == R2.Chc_Open) {
					SearchClient.OpenResult(sri.Url);
					clrWrite = false;
					continue;
				}

				if (cmd == R2.Chc_Scan && !sri.IsChild && sri is not { HasScannedItems: true }) {
					await AnsiConsole.Live(srTable).StartAsync(async f =>
					{
						s_logger.LogTrace("Scanning {Item}", sri);
						bool scannedOk = false;
						scannedOk = await sri.ScanAsync(m_ctsRun.Token);

						if (!scannedOk) {
							return;
						}

						for (int i = 0; i < sri.ScannedItems.Count; i++) {
							IResultItem scnItm = sri.ScannedItems[i];
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

				if (cmd == R2.Chc_Calc && item.HasHash) {

					AnsiConsole.Live(srTable).Start(f =>
					{
						item.CalculateSimilarity(Query.Source);

						srTable.Rows.Update(selIdx2, (int) ResultRowIndex.ROW_SIMILARITY, (sri ?? item).GetSimilarity());
						f.Refresh();
					});


					clrWrite = true;
					continue;
				}


				if (cmd == R2.Chc_Preview && item is ScannedResultItem { HasImage: true } sriScn) {

					var ci = GetPreviewCanvasImage(sriScn);

					AnsiConsole.AlternateScreen(() =>
					{
						//
						ShowPreview(ci, sriScn);
					});
					clrWrite = true;
				}

				if (cmd == R2.Chc_Download && item is ScannedResultItem { } sriScnDl) {

					if (!sriScnDl.HasLocalFilePath) {
						HandleDownload(sriScnDl);
					}
					else {
						AnsiConsole.WriteLine($"Already downloaded {sriScnDl}");
						var proc = Process.Start(sriScnDl.LocalFilePath);
						await proc.WaitForExitAsync(ct);
						proc.Dispose();
					}
				}


				if (cmd == R2.Chc_Expand) {

					AnsiConsole.AlternateScreen(() =>
					{
						var gr = GetExpandedLayout(item);
						AC.Write(gr);
						AC.Console.Input.ReadKey(true);
					});
				}

			} while (cmd != R2.Chc_Back && !ct.IsCancellationRequested);

		} while (cmd != R2.Chc_Exit && !ct.IsCancellationRequested);
	}

#endregion

	private Layout CreateLayout()
	{
		var queryCi = new CanvasImage(Query.Source.GetSource());

		var ciPanel = new Panel(queryCi)
		{
			Header = new PanelHeader($"{Query.Source.Value}"),
			Expand = true,
		};

		var cfgGrid = Renderables.CreateConfigGrid(Config, Query);

		var cfgPanel = new Panel(cfgGrid) { Header = new PanelHeader("Search Options") };

		return new Layout("Root")
			.SplitColumns(
				new Layout("L").SplitRows(
					new("LC", cfgPanel),
					new("LT", m_mainTable)),
				new Layout("R", ciPanel));
	}

	private Layout GetExpandedLayout(IResultItem sri)
	{
		CanvasImage prev;

		if (sri is ScannedResultItem scnItem) {
			prev = GetPreviewCanvasImage(scnItem);
		}
		else {
			prev = new CanvasImage(Query.Source.GetSource());
		}

		var ciPanel = new Panel(prev)
		{
			Header = new PanelHeader($"{sri}"),
			Expand = true,
		};

		var extGrid = Renderables.CreateExtendedGrid(sri);

		var extPanel = new Panel(extGrid) { Header = new PanelHeader("Result Data") };

		var exLayout = new Layout("Root")
			.SplitColumns(
				new Layout("L", extPanel),
				new Layout("R", ciPanel));

		return exLayout;
	}

	private static void HandleDownload(ScannedResultItem sri)
	{
		var ok = sri.TryWriteOrGetFile();

		if (ok) {
			AC.AlternateScreen(() =>
			{
				var gr = new Grid();
				gr.AddColumns(new(), new());
				gr.AddRow(new Text("File", Elements.Sty_Name), new Text(sri.LocalFilePath));
				AnsiConsole.Write(gr);

				var prompt = new ConfirmationPrompt("Open?");
				var choice = AnsiConsole.Prompt(prompt);

				if (choice) {
					using var proc = Process.Start(new ProcessStartInfo
					{
						FileName         = sri.LocalFilePath,
						WorkingDirectory = String.Empty,
						UseShellExecute  = true
					});

					proc?.WaitForExit();
				}
			});
		}
	}

	private void OnCancelKeyPress(object sender, ConsoleCancelEventArgs args)
	{
		s_logger.LogTrace("Cancellation requested {Sender} {Args}", sender, args);

		// AnsiConsole.Clear();

		m_ctsRunSearch.Cancel();

		// m_cts.Cancel();
		// m_cts.TryReset();
		// m_ctsRun.TryReset();

		args.Cancel = true;
	}

	public override ValidationResult Validate(CommandContext context, SearchCommandSettings settings)
	{
		var r = base.Validate(context, settings);
		return r;
	}

	public override void Dispose()
	{
		s_logger.LogDebug("Disposing search command");

		Elements.Prm_Selection.Validator = null;

		m_cts.Dispose();
		m_ctsRun.Dispose();
		m_ctsRunSearch.Dispose();
		m_previewCanvasCache.Dispose();
		Client.Dispose();
		Query.Dispose();
	}

}