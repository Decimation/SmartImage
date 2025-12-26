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

using CliWrap;
using Flurl;
using Flurl.Http;
using JetBrains.Annotations;
using Kantan.Diagnostics;
using Kantan.Model.MemberIndex;
using Kantan.Monad;
using Kantan.Net.Utilities;
using Kantan.Text;
using Kantan.Utilities;
using Microsoft;
using Novus.Streams;
using Novus.Utilities;
using SixLabors.ImageSharp.Processing;
using SmartImage.Lib;
using SmartImage.Lib.Engines;
using SmartImage.Lib.Images;
using SmartImage.Lib.Images.Uni;
using SmartImage.Lib.Utilities;
using SmartImage.Lib.Utilities.Diagnostics;
using SmartImage.Lib.Utilities.Integration;
using SmartImage.Rdx.Shell;
using Spectre.Console;
using Spectre.Console.Cli;
using Spectre.Console.Rendering;
using System;
using System.Collections.Concurrent;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.Caching;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SmartImage.Lib.Engines.Results;
using SmartImage.Lib.Model;
using SmartImage.Lib.Engines.Search;
using Size = SixLabors.ImageSharp.Size;
using SmartImage.Lib.Engines.Upload;


[assembly: InternalsVisibleTo(SearchQuery.PROJ_SMARTIMAGE_LIB_UNITTEST)]

namespace SmartImage.Rdx.Commands;

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

	private readonly MemoryCache m_prevCache;

	private readonly SpcTable m_mainTable;

	private readonly SelectionPrompt<SearchResult> m_srPrompt = new()
	{
		Mode          = SelectionMode.Leaf,
		SearchEnabled = true,
		Converter     = static sr => { return sr.Engine.Name; }
	};

	private Layout m_layout;

	private static readonly ILogger s_logger;

	static SearchCommand()
	{
		s_logger = AppSupport.Factory.CreateLogger(nameof(SearchCommand));

	}

	public SearchCommand()
	{
		m_cts       = new CancellationTokenSource();
		m_ctsRun    = new CancellationTokenSource();
		m_scs       = null;
		m_mainTable = CreateMainTable();

		// m_results      = new();
		m_resultTables = new ConcurrentDictionary<SearchResult, SpcTable>();
		m_prevCache    = new MemoryCache("Buf");

		Query = SearchQuery.Null;
	}

#region

	private async Task InitQueryAsync(ProgressContext ctx)
	{
		var p = ctx.AddTask("Creating query");
		p.IsIndeterminate = true;

		Query = await SearchQuery.TryCreateAsync(m_scs.Query);

		if (Query == SearchQuery.Null) {
			throw new SmartImageException($"Could not create query {Query}");
		}

		p.Increment(ConsoleFormat.COMPLETE / 2);

		// ctx.Refresh();

		p.Description = "Uploading query";
		var url = await Query.TryUploadAsync();

		if (!url) {
			throw new SmartImageException($"Could not upload {Query}");
		}

		p.Increment(ConsoleFormat.COMPLETE / 2);

	}

	/// <inheritdoc />
	protected override void InitConfig(SearchCommandSettings scs)
	{
		Config = new SearchConfig();

		base.InitConfig(scs);

		Client = new SearchClient(Config);
	}

	public override async Task<int> ExecuteAsync(CommandContext context, SearchCommandSettings settings, CancellationToken cancellationToken)
	{
		Console.CancelKeyPress += OnCancelKeyPress;

		InitConfig(settings);

		var initTask = AnsiConsole.Progress()
			.AutoRefresh(true)
			.StartAsync(InitQueryAsync);

		await initTask;

		var ci = ConsoleFormat.GetQueryCanvasImage(Query.Source);

		var ciPanel = new Panel(ci)
		{
			Header = new PanelHeader($"{Query.Source.Value}"),
			Expand = true,
		};

		var cfgGrid = ConsoleFormat.CreateConfigGrid(Config, Query);

		var cfgPanel = new Panel(cfgGrid) { Header = new PanelHeader("Config") };

		m_layout = new Layout("Root").SplitColumns(
			new Layout("L").SplitRows(
				new("LC", cfgPanel),
				new("LT", m_mainTable)
			),
			new Layout("R", ciPanel) { }
		);

		// AnsiConsole.Write(m_layout);


		try {
			Task main = AnsiConsole.Live(m_layout)
				.StartAsync(c => RunSearchLiveAsync(c, m_ctsRun.Token));

			await main;
		}
		catch (OperationCanceledException e) {
			s_logger.LogError(e, "Canceled");
		}

		if (m_scs.HasCommand) {
			await RunCompletionCommandAsync(m_cts.Token);
		}

		if (m_scs.HasOutputFile) {
			switch (m_scs.OutputFileFormat) {

				case OutputFileFormat.None:
					break;

				case OutputFileFormat.Delimited:
					WriteOutputFile();
					break;

				default:
					throw new ArgumentOutOfRangeException();
			}

		}

		if (m_scs.Interactive) {
			await RunInteractiveAsync(m_cts.Token);
		}

		if (m_scs.KeepOpen) {
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

			var table = CreateResultTable();
			var rows  = CreateResultRows(result);

			foreach (IRenderable[] row in rows) {
				table.AddRow(row);
			}

			m_resultTables.TryAdd(result, table);

			m_mainTable.AddRow(CreateMainRows(result));
			m_srPrompt.AddChoice(result);

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

		bool srTableClr = false;

		do {

			AnsiConsole.Clear();
			AnsiConsole.Write(m_layout);

			sr = AnsiConsole.Prompt(m_srPrompt);

			srTable = m_resultTables[sr];

			clrWrite = true;

			do {
				if (clrWrite) {
					AnsiConsole.Clear();
					AnsiConsole.Write(srTable);

				}

				cmd = AnsiConsole.Prompt(ConsoleFormat.Prm_Command);

				if (cmd == R2.Chc_Back) {
					break;
				}

				var sel = GetResultItemIndexes(sr);
				var sri = sel.Item;

				s_logger.LogInformation("Selected {Item} {Scn} | {Idx1}, {Idx2}", sel.Item, sel.IsScannedItem, sel.ItemIdx, sel.ScanIdx);

				// s_logger.LogTrace("Interactive: {ResItem}", sri);

				if (cmd == R2.Chc_Open) {
					SearchClient.OpenResult(sri.Url);
					clrWrite = false;
					continue;
				}

				if (cmd == R2.Chc_Scan) {
					await AnsiConsole.Live(srTable).StartAsync(async (f) =>
					{
						s_logger.LogTrace("Scanning {Item}", sri);
						bool scannedOk = false;
						scannedOk = await sri.ScanAsync2(m_ctsRun.Token);

						if (!scannedOk) {
							return;
						}

						int row;

						row = sr.Results.IndexOf(sri);

						var idx = sel.Index();

						// row = GetRowForItem(sri);
						// row = sr.Index(sel.Item, sel.Scanned);

						// row = sr.Index(sri);

						// var row2 = sriPak;
						// row = row2.RootIdx + (row2.ScnIdx == -1 ? 0 : (row2.ScnIdx + 1));

						if (sri.HasImage) {
							srTable.Rows.Update(row, ROW_WH, CreateResultItemResolutionRow(sri));

						}

						if (sri.HasHash && !sri.Similarity.HasValue) {
							sri.CalculateSimilarity(Query.Source);
							srTable.Rows.Update(row, ROW_SIMILARITY, CreateResultItemSimilarityCell(sri));
						}

						var scanned = sri.ScannedItems;

						int i = 0;

						foreach (var ui in scanned) {

							srTable.InsertRow(++row, CreateItemRow(ui, idx, i++));
						}

						/*if (sr.ScannedResults.TryGetValue(sri, out SearchResultItem[] scanned)) {
							int i = 0;

							var idx = sri.Root.Results.IndexOf(sri);

							foreach (var ui in scanned) {

								srTable.InsertRow(++row, CreateItemRow(ui, idx, i++));
							}

						}*/

						f.Refresh();

						/*foreach (var kv in m_results) {
						if (kv.Value >= rowOrig) {
							m_results[kv.Key] = kv.Value + delta;
						}
					}*/

					});
					clrWrite = true;
					continue;
				}

				if (cmd == R2.Chc_Calc) {

					if (sri.HasHash) {

						AnsiConsole.Live(srTable).Start(f =>
						{
							// var row  = GetRowForItem(sri);
							// var row = SearchResultItem.FindSumIndex(sri);
							// var row = sr.Index(sri);

							// var row = sr.Index(sel.Item, sel.Scanned);

							var row = sel.Index();

							// var row = (sel.ScanIdx == -1 ? 0 : sel.ScanIdx) + sel.ItemIdx;

							// var row2 = sriPak;
							// var row  = row2.RootIdx + (row2.ScnIdx == -1 ? 0 : (row2.ScnIdx + 1));

							sri.CalculateSimilarity(Query.Source);

							srTable.Rows.Update(row, ROW_SIMILARITY, CreateResultItemSimilarityCell(sri));
							f.Refresh();
						});
					}

					clrWrite = false;
					continue;
				}

				if (cmd == R2.Chc_Preview) {
					if (!sri.HasBytes || !sri.HasImage) {
						continue;
					}

					var ci = GetPreview(sri);

					ShowPreview(ci, sri);
					clrWrite = true;
				}

				if (cmd == R2.Chc_Download) {
					//todo
				}

			cont:
				continue;

			} while (cmd != R2.Chc_Back);

		} while (cmd != R2.Chc_Exit);
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
		var val = m_prevCache.Get(key);

		CanvasImage ci = val as CanvasImage;

		if (ci is null) {
			str = sri.GetStream();
			ci  = new CanvasImage(str);
			m_prevCache.Set(key, ci, cip);
		}
		else { }

		Trace.Assert(ci != null);

		// var ci = new CanvasImage(str);

		return ci;
	}

	private void ShowPreview(CanvasImage ci, SearchResultItem ui)
	{
		// AnsiConsole.Clear();

		// (AnsiConsole.Profile.Width, AC.Profile.Height) = (ui.Image.Width, ui.Image.Height);
		// ci.MaxWidth = AnsiConsole.Profile.Width;

		// Console.SetWindowSize(ui.Image.Width, ui.Image.Height);
		// ci.MaxWidth ??= ci.Width;
		// ci.MaxWidth ??= ui.Image.Width;

		// var panel = new Panel(ci) { Expand = true, };

		var (w, h)     = (AnsiConsole.Profile.Width, AC.Profile.Height);
		var (mwO, pwO) = (ci.MaxWidth, ci.PixelWidth);
		var (mw, pw)   = (mwO ?? ci.Width, pwO);

		AnsiConsole.Live(ci).Start((ldc) =>
		{
			while (true) {

				ldc.Refresh();
				var cki = AnsiConsole.Console.Input.ReadKey(true);


				if (cki.HasValue) {

					switch (cki.Value.Key) {
						case ConsoleKey.DownArrow:
							pw--;
							break;

						case ConsoleKey.LeftArrow:
							mw--;
							break;

						case ConsoleKey.UpArrow:
							pw++;
							break;

						case ConsoleKey.RightArrow:
							mw++;
							break;

						case ConsoleKey.Escape:
							return;

						/*
						case ConsoleKey.R:
							mw = ui.Image.Width;

							// ci.PixelWidth =   0;
							break;
							*/

						case ConsoleKey.A:
							ci.MaxWidth = mwO;
							pw          = 0;
							break;
					}

					ci.MaxWidth   = mw < 0 ? null : mw;
					ci.PixelWidth = Math.Clamp(pw, 0, 10);
				}

				// Console.Title = $"{ci.MaxWidth} / {ci.PixelWidth}";
				// panel.Header = new PanelHeader($"{ci.MaxWidth} / {ci.PixelWidth}");
			}
		});


		return;
	}

#endregion


	// [ContractAnnotation("=> halt")]
	private void OnCancelKeyPress(object sender, ConsoleCancelEventArgs args)
	{
		// AnsiConsole.MarkupLine($"[red]Cancellation requested[/]");
		// AnsiConsole.MarkupLine($"[red]Sender: {sender}[/]");

		s_logger.LogTrace("Cancellation requested {Sender} {Args}", sender, args);

		// AnsiConsole.Clear();

		// m_cts.Cancel();
		m_ctsRun.Cancel();

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
		Debug.WriteLine($"Disposing {nameof(SearchCommand)}");

		foreach (var sr in m_resultTables.Keys) {
			sr.Dispose();
		}

		ConsoleFormat.Prm_Num.Validator  = null;
		ConsoleFormat.Prm_Num2.Validator = null;

		m_resultTables.Clear();
		m_cts.Dispose();
		m_prevCache.Dispose();
		m_scs = null;
		Client.Dispose();
		Query.Dispose();
	}

}