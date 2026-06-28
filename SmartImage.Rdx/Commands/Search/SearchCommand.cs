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
using System.Threading.Channels;
using Flurl;
using Microsoft.Extensions.Logging;
using SmartImage.Lib;
using SmartImage.Lib.Engines.Results;
using SmartImage.Lib.Images;
using SmartImage.Lib.Images.Uni;
using SmartImage.Lib.Utilities;
using SmartImage.Lib.Utilities.Diagnostics;
using SmartImage.Lib.Utilities.Integration;
using SmartImage.Rdx.Commands.Common;
using SmartImage.Rdx.Shell;
using Spectre.Console;
using Spectre.Console.Cli;
using Spectre.Console.Rendering;

// ReSharper disable UseSymbolAlias

// TODO: Create derived SearchCommand components for interactive/non-interactive; types representing shell UI state
// todo: use DI

[assembly: InternalsVisibleTo(Common.PROJ_SMARTIMAGE_LIB_UNITTEST)]

namespace SmartImage.Rdx.Commands.Search;

#nullable disable

public sealed partial class SearchCommand : CommonAsyncCommand<SearchCommandSettings>
{

	private static readonly ILogger s_logger = AppSupport.Factory.CreateLogger(nameof(SearchCommand));

	private readonly CancellationTokenSource m_cts;
	private readonly CancellationTokenSource m_ctsRun;
	private readonly CancellationTokenSource m_ctsRunSearch;

	// private Dictionary<SearchResult, Dictionary<IResultItem, int>>
	public SearchClient Client { get; private set; }

	public SearchQuery Query { get; private set; }

	public SearchCommand()
	{
		m_cts          = new CancellationTokenSource();
		m_ctsRun       = new CancellationTokenSource();
		m_ctsRunSearch = new CancellationTokenSource();

		m_previewCanvasCache = new MemoryCache("PreviewCache");
		m_dialogs            = [];
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

		// todo
		if (BaseOSIntegration.Integration.IsGalleryDLInstalled) {
			Elements.Prm_Command.Choices.Insert(2,R2.Chc_GalleryDl);
		}
	}

	protected override async Task<int> ExecuteAsync(CommandContext context, SearchCommandSettings settings, CancellationToken cancellationToken)
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

		return Lib.Common.EC_OK;
	}

	private async Task RunSearchLiveAsync(LiveDisplayContext c, CancellationToken ct = default)
	{
		var searchTask = Client.RunSearchAsync(Query, token: ct);

		while (!ct.IsCancellationRequested && await Client.ResultChannel.Reader.WaitToReadAsync(ct)) {
			var result = await Client.ResultChannel.Reader.ReadAsync(ct);


			if (CommandSettings.Interactive) {

				m_dialogs.TryAdd(result, ResultViewState.Create(result));

				m_mainTable.AddRow(result.GetMainRows());
				Elements.Prm_SearchResult.AddChoice(result);

			}
			else {

				var fullRows = result.GetFullRows();

				foreach (var list in fullRows) {
					var row = (IRenderable[]) list;
					m_mainTable.AddRow(row);
				}
			}


			c.Refresh();

		}

		await searchTask;
	}

	private async Task RunInteractiveAsync(CancellationToken ct = default)
	{
		string       cmd      = null;
		bool         clrWrite = true;
		SpcTable     srTable  = null;
		SearchResult sr       = null;

		/*
		 * TODO: organize logic and UI state machine into discrete objects instead of this
		 * epically complex nested logic
		 */

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

				if (sel == null) {
					continue;
				}

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

				if (cmd == R2.Chc_Scan && item is not ISubResultItem { IsChild: true }) {
					await AnsiConsole.Live(srTable).StartAsync(async f =>
					{
						s_logger.LogTrace("Scanning {Item}", item);
						bool scannedOk = false;
						scannedOk = await item.Root.ScanAsync(item, ct);

						if (!scannedOk) {
							return;
						}

						for (int i = 0; i < sr.ScannedItems.Count; i++) {
							IResultItem scnItm = sr.ScannedItems[i];
							scnItm.TryCalculateSimilarity(Query.Source);

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
						item.TryCalculateSimilarity(Query.Source);

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

				// todo: wip
				if (cmd == R2.Chc_GalleryDl && item is not ISubResultItem { IsChild: true }) {
					var ch      = Channel.CreateUnbounded<Url>();
					var gdlTask = ImageScanner.RunGalleryDLAsync(item.Url, ch.Writer, ct);

					while (await ch.Reader.WaitToReadAsync(ct)) {
						var res = await ch.Reader.ReadAsync(ct);

						if (res is not null) {
							var scn = await ScannedResultItem.FromSourceAsync(res, sri, ct: ct);

							if (scn != null) {
								sr.ScannedItems.Add(scn);
							}
						}
					}

					await gdlTask;
				}

			} while (cmd != R2.Chc_Back && !ct.IsCancellationRequested);

		} while (cmd != R2.Chc_Exit && !ct.IsCancellationRequested);
	}

#endregion

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

	protected override ValidationResult Validate(CommandContext context, SearchCommandSettings settings)
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