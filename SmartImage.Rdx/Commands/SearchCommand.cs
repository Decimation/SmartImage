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
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SmartImage.Lib.Engines.Results;
using SmartImage.Lib.Model;
using SmartImage.Lib.Engines.Search;
using Size = SixLabors.ImageSharp.Size;


[assembly: InternalsVisibleTo(SearchQuery.PROJ_SMARTIMAGE_LIB_UNITTEST)]

namespace SmartImage.Rdx.Commands;

#nullable disable

public sealed partial class SearchCommand : CommonAsyncCommand<SearchCommandSettings>
{

	public SearchClient Client { get; }

	public SearchQuery Query { get; private set; }

	private readonly CancellationTokenSource m_cts;

	/// <summary>
	/// Key: <see cref="SearchResult"/>
	/// Value: <see cref="m_table"/> index
	/// </summary>
	private readonly ConcurrentDictionary<SearchResult, int> m_results;

	private readonly MemoryCache m_cache;

	private readonly STable m_table;

	private static readonly ILogger s_logger = AppSupport.Factory.CreateLogger(nameof(SearchCommand));

	static SearchCommand() { }

	public SearchCommand()
	{
		Config = new SearchConfig();

		// Config = (SearchConfig) cfg;
		Client = new SearchClient(Config);

		m_cts     = new CancellationTokenSource();
		m_scs     = null;
		m_table   = CreateResultTable();
		m_results = new();
		m_cache   = new MemoryCache("Buf");

		Query = SearchQuery.Null;

	}

#region

	private async Task InitQueryAsync(ProgressContext ctx)
	{

		var p = ctx.AddTask("Creating query");
		p.IsIndeterminate = true;
		bool ok = true;

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

	public override async Task<int> ExecuteAsync(CommandContext context, SearchCommandSettings settings)
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

		AC.Write(ciPanel);
		AC.Write(cfgPanel);

		/*var layout = new Layout("Root").SplitRows(
			new Layout("T", ciPanel) { },
			new Layout("B", cfgPanel));

		AnsiConsole.Write(layout);*/


		/*
		 * todo
		 */

#if !UNITTEST
		Task main = AnsiConsole.Live(m_table)
			.StartAsync(c => RunSearchLiveAsync(c));

#else
		run = RunSearchLiveAsync(null);

#endif
		await main;

		if (!main.IsCompletedSuccessfully) {
			Debugger.Break();
			return BaseOSIntegration.EC_ERROR;
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


	private async Task RunSearchLiveAsync(LiveDisplayContext c, CancellationToken token = default)
	{

#if UNITTEST
		return;
#endif

		var search = Client.RunSearchAsync(Query, token: m_cts.Token);

		while (await Client.ResultChannel.Reader.WaitToReadAsync(token)) {
			var task = Client.ResultChannel.Reader.ReadAsync(token);

			var result = await task;

			m_results.TryAdd(result, BaseOSIntegration.EC_ERROR);

			var rows = CreateResultRows(result);

			m_results[result] = m_table.Rows.Count;

			foreach (IRenderable[] row in rows) {
				m_table.AddRow(row);
			}

			c.Refresh();

		}

		await search;
	}

	private async Task RunInteractiveAsync(CancellationToken ct = default)
	{
		string cmd      = null;
		bool   clrWrite = true;

		do {

			AnsiConsole.Clear();
			AnsiConsole.Write(m_table);

			cmd = GetCommandPrompt();

			if (cmd == R2.Chc_Exit) {
				goto cont;
			}

			var sr = GetEnginePrompt();

			var sri = GetResultItemPrompt(sr);

			s_logger.LogTrace("Interactive: {ResItem}", sri);

			if (cmd == R2.Chc_Open) {
				SearchClient.OpenResult(sri.Url);
				continue;
			}

			if (cmd == R2.Chc_Scan) {
				await ScanItemAsync(sri, ct);
				continue;
			}

			if (cmd == R2.Chc_Calc) {
				if (sri.HasHash) {
					CalculateItem(sri);
				}

				continue;
			}

			if (cmd == R2.Chc_Preview) {
				if (!sri.HasBytes || !sri.HasImage) {
					continue;
				}

				var ci = GetPreview(sri);

				ShowPreview(ci, sri);
			}

			if (cmd == R2.Chc_Download) {
				//todo
			}

		cont:
			continue;
		} while (cmd != R2.Chc_Exit);
	}

#endregion

#region

	private void CalculateItem(SearchResultItem item)
	{
		AnsiConsole.Live(m_table).Start(f =>
		{
			var row = GetRowForItem(item);
			item.CalculateSimilarity(Query.Source);

			m_table.Rows.Update(row, 2, new Text(item.Similarity.ToString()));
			f.Refresh();
		});


	}

	private async ValueTask ScanItemAsync(SearchResultItem item, CancellationToken token = default)
	{
		await AnsiConsole.Live(m_table).StartAsync(async (f) =>
		{
			bool scannedOk = false;

			if (!(item.HasImage ^ item.HasScannedItems)) {

				// var ok = await r.ScanAsync();
				s_logger.LogTrace("Scanning {Item}", item);
				scannedOk = await item.ScanAsync(token);

			}
			else {
				return;
			}

			if (!scannedOk) {
				return;
			}

			int i       = 0;
			var row     = GetRowForItem(item);
			var rowOrig = row;
			var delta   = item.ScannedItems.Count;
			var idx     = item.Root.Results.IndexOf(item);

			// item.Root.Results.InsertRange(idx, scannedItems);

			foreach (var ui in item.ScannedItems) {
				m_table.InsertRow(++row, CreateItemRow(ui, idx, i++));
			}

			f.Refresh();

			foreach (var kv in m_results) {
				if (kv.Value >= rowOrig) {
					m_results[kv.Key] = kv.Value + delta;
				}
			}

		});

	}

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
		var val = m_cache.Get(key);

		CanvasImage ci = val as CanvasImage;

		if (ci is null) {
			str = sri.GetStream();
			ci = new CanvasImage(str);
			m_cache.Set(key, ci, cip);
		}
		else {
		}

		Trace.Assert(ci != null);

		// var ci = new CanvasImage(str);

		return ci;
	}

	private void ShowPreview(CanvasImage ci, SearchResultItem ui)
	{
		AnsiConsole.Clear();

		// (AnsiConsole.Profile.Width, AC.Profile.Height) = (ui.Image.Width, ui.Image.Height);
		ci.MaxWidth = AnsiConsole.Profile.Width;

		// Console.SetWindowSize(ui.Image.Width, ui.Image.Height);
		// ci.MaxWidth ??= ci.Width;
		// ci.MaxWidth ??= ui.Image.Width;

		// var panel = new Panel(ci) { Expand = true, };
		var (w, h) = (AnsiConsole.Profile.Width, AC.Profile.Height);

		AnsiConsole.Live(ci).Start((ldc) =>
		{
			while (true) {

				ldc.Refresh();
				var cki = AnsiConsole.Console.Input.ReadKey(true);


				if (cki.HasValue) {
					int mw = 0, pw = 0;

					switch (cki.Value.Key) {
						case ConsoleKey.DownArrow:
							pw = -1;
							break;

						case ConsoleKey.LeftArrow:
							mw = -1;
							break;

						case ConsoleKey.UpArrow:
							pw = 1;
							break;

						case ConsoleKey.RightArrow:
							mw = 1;
							break;

						case ConsoleKey.Escape:
							return;

						case ConsoleKey.R:
							ci.MaxWidth = ui.Image.Width;

							// ci.PixelWidth =   0;
							break;

						case ConsoleKey.A:
							ci.MaxWidth = AnsiConsole.Profile.Width;
							break;

						case ConsoleKey.W:
							AnsiConsole.Console.Profile.Width  = ui.Image.Width;
							AnsiConsole.Console.Profile.Height = ui.Image.Height;


							// ci.MaxWidth = ui.Image.Width;
							break;
					}

					if (mw != 0 || pw != 0) {
						ci.PixelWidth = Math.Clamp(ci.PixelWidth      + pw, 0, ci.Width);
						ci.MaxWidth   = Math.Clamp((ci.MaxWidth ?? 0) + mw, 0, AnsiConsole.Profile.Width);

					}
				}

				// Console.Title = $"{ci.MaxWidth} / {ci.PixelWidth}";
				// panel.Header = new PanelHeader($"{ci.MaxWidth} / {ci.PixelWidth}");
			}
		});


		return;
	}

#endregion


#region

	private async Task RunCompletionCommandAsync(CancellationToken ct = default)
	{
		// ReSharper disable once AssignNullToNotNullAttribute
		var command = Cli.Wrap(m_scs.Command);

		var cmdArgs      = m_scs.CommandArguments;
		var stdOutBuffer = new StringBuilder();
		var stdErrBuffer = new StringBuilder();

		if (cmdArgs is not null) {
			command = command.WithArguments(cmdArgs);
		}

		command = command.WithStandardErrorPipe(PipeTarget.ToStringBuilder(stdErrBuffer))
			.WithStandardOutputPipe(PipeTarget.ToStringBuilder(stdOutBuffer));

		var commandTask = command.ExecuteAsync(ct);

		AnsiConsole.WriteLine($"Process id: {commandTask.ProcessId}");

		var result = await commandTask;

		AnsiConsole.WriteLine($"Process successful: {result.IsSuccess}");
	}

	private void WriteOutputFile()
	{
		// ReSharper disable once AssignNullToNotNullAttribute
		var fw = File.OpenWrite(m_scs.OutputFile);

		using var sw = new StreamWriter(fw);
		sw.AutoFlush = true;

		var fields = m_scs.OutputFields;

		bool fName   = fields.HasFlag(OutputFields.Name);
		var  fUrl    = fields.HasFlag(OutputFields.Url);
		var  fSim    = fields.HasFlag(OutputFields.Similarity);
		var  fArtist = fields.HasFlag(OutputFields.Artist);
		var  fSite   = fields.HasFlag(OutputFields.Site);

		var names = Enum.GetValues<OutputFields>()
			.Where(f => fields.HasFlag(f) && !f.Equals(default(OutputFields)))
			.Select(Enum.GetName);

		sw.WriteLine(String.Join(m_scs.OutputFileDelimiter, names));

		foreach (SearchResult sr in m_results.Keys) {
			for (int j = 0; j < sr.Results.Count; j++) {
				var sri = sr.Results[j];

				var rg = new List<string>();

				if (fName)
					rg.Add($"{sr.Engine.Name} #{j + 1}");

				if (fUrl)
					rg.Add(sri.Url);

				if (fSim)
					rg.Add($"{sri.Similarity}");

				if (fArtist)
					rg.Add($"{sri.Artist}");

				if (fSite)
					rg.Add($"{sri.Site}");

				// string[] items  = [$"{sr.Engine.Name} #{j + 1}", sri.Url?.ToString()];
				sw.WriteLine(String.Join(m_scs.OutputFileDelimiter, rg));
			}
		}

		AnsiConsole.WriteLine($"Wrote to {m_scs.OutputFile}");
	}

#endregion

	[ContractAnnotation("=> halt")]
	private void OnCancelKeyPress(object sender, ConsoleCancelEventArgs args)
	{
		AnsiConsole.MarkupLine($"[red]Cancellation requested[/]");
		AnsiConsole.MarkupLine($"[red]Sender: {sender}[/]");

		// AnsiConsole.Clear();

		m_cts.Cancel();
		args.Cancel = false;

		Environment.Exit(BaseOSIntegration.EC_ERROR);
	}

	public override ValidationResult Validate(CommandContext context, SearchCommandSettings settings)
	{
		var r = base.Validate(context, settings);
		return r;
	}

	public override void Dispose()
	{
		Debug.WriteLine($"Disposing {nameof(SearchCommand)}");

		foreach (var sr in m_results.Keys) {
			sr.Dispose();
		}

		ConsoleFormat.Prm_Num.Validator  = null;
		ConsoleFormat.Prm_Num2.Validator = null;
		ConsoleFormat.Prm_Engine.Choices.Clear();

		m_results.Clear();
		m_cts.Dispose();
		m_cache.Dispose();
		m_scs = null;
		Client.Dispose();
		Query.Dispose();
	}

}