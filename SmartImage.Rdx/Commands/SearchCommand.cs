// Read S SmartImage.Rdx SearchCommand.cs
// 2023-07-05 @ 2:07 AM

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

// ReSharper disable InconsistentNaming

[assembly: InternalsVisibleTo(SearchQuery.PROJ_SMARTIMAGE_LIB_UNITTEST)]

namespace SmartImage.Rdx.Commands;

#nullable disable

public sealed partial class SearchCommand : AsyncCommand<SearchCommandSettings>, IDisposable
{

	public SearchClient Client { get; }

	public SearchQuery Query { get; private set; }

	public SearchConfig Config { get; }

	private readonly CancellationTokenSource m_cts;

	/// <summary>
	/// Key: <see cref="SearchResult"/>
	/// Value: <see cref="m_table"/> index
	/// </summary>
	private readonly ConcurrentDictionary<SearchResult, int> m_results;

	private readonly MemoryCache m_cache;

	private SearchCommandSettings m_scs;

	private readonly STable m_table;

	private static readonly ILogger s_logger = AppSupport.Factory.CreateLogger(nameof(SearchCommand));


	static SearchCommand() { }

	public SearchCommand()
	{
		Config = new SearchConfig();

		// Config = (SearchConfig) cfg;
		Client = new SearchClient(Config);

		// Client.OnSearchComplete += OnSearchComplete;

		// Client.OnResultComplete   += OnResultComplete;
		m_cts     = new CancellationTokenSource();
		m_scs     = null;
		m_table   = CreateResultTable();
		m_results = new();
		m_cache   = new MemoryCache("Buf");


		Query = SearchQuery.Null;
	}

#region

	private async Task InitConfigAsync([CBN] object c)
	{
		//todo

		Config.SearchEngines   = m_scs.SearchEngines;
		Config.PriorityEngines = m_scs.PriorityEngines;

		Config.ReadCookies = m_scs.ReadCookies;

		Config.FlareSolverr       = m_scs.FlareSolverr;
		Config.FlareSolverrApiUrl = m_scs.FlareSolverrApiUrl;


	}

	private async Task<bool> InitQueryAsync(ProgressContext ctx)
	{
		var p = ctx.AddTask("Creating query");
		p.IsIndeterminate = true;
		bool ok = true;

		Query = await SearchQuery.TryCreateAsync(m_scs.Query);

		if (Query == SearchQuery.Null) {
			// throw new SmartImageException($"Could not create query"); //todo

			ok = false;
			goto ret;
		}


		p.Increment(ConsoleFormat.COMPLETE / 2);

		// ctx.Refresh();

		p.Description = "Uploading query";
		var url = await Query.UploadAsync();

		if (url == null) {
			// throw new SmartImageException("Could not upload query"); //todo
			ok = false;
			goto ret;
		}

		p.Increment(ConsoleFormat.COMPLETE / 2);

	ret:
		return ok;
	}

	public override async Task<int> ExecuteAsync(CommandContext context, SearchCommandSettings settings)
	{
		m_scs = settings;

		var task = AnsiConsole.Progress()
			.AutoRefresh(true)
			.StartAsync(InitQueryAsync);

		try {
			var ok = await task;

			if (!ok) {
				throw new SmartImageException("Could not upload query");
			}

			await InitConfigAsync(ok);

			var ci = ConsoleFormat.GetQueryCanvasImage(Query.Source);

			// var ci = new CanvasImage(Query.Source.GetStream());

			var panel = new Panel(ci)
			{
				Header = new PanelHeader($"{Query.Source.Value}"),
				Expand = true,

			};


			var gr = ConsoleFormat.CreateConfigGrid(Config, Query);

			var grp = new Panel(gr) { Header = new PanelHeader("Config") };

			var layout = new Layout("Root")
				.SplitRows(
					new Layout("T", panel) { },
					new Layout("B", grp));

			AnsiConsole.Write(layout);
		}
		catch (Exception e) {
			AnsiConsole.WriteException(e);
			return BaseOSIntegration.EC_ERROR;
		}


		Console.CancelKeyPress += OnCancelKeyPress;

		/*
		 *
		 * todo
		 */

#if !UNITTEST
		Task run = AnsiConsole.Live(m_table)
			.StartAsync(c => RunSearchLiveAsync(c));

		/*
		Task run = AnsiConsole.Live(m_root)
			.StartAsync(c => RunSearchLiveAsync2(c));
			*/

#else
		run = RunSearchLiveAsync(null);

#endif

		if (!String.IsNullOrWhiteSpace(m_scs.Command)) {
			run = run.ContinueWith(RunCompletionCommandAsync, m_cts.Token,
			                       TaskContinuationOptions.OnlyOnRanToCompletion,
			                       TaskScheduler.Default);
		}

		if (!String.IsNullOrWhiteSpace(m_scs.OutputFile)) {
			switch (m_scs.OutputFileFormat) {

				case OutputFileFormat.None:
					break;

				case OutputFileFormat.Delimited:
					run = run.ContinueWith(WriteOutputFile, m_cts.Token,
					                       TaskContinuationOptions.OnlyOnRanToCompletion,
					                       TaskScheduler.Default);
					break;

				default:
					throw new ArgumentOutOfRangeException();
			}

		}

		await run;

		Task run2;

		if (m_scs.Interactive) {
			run2 = RunInteractiveAsync(m_cts.Token);

			// run2 = RunInteractiveAsync2(m_cts.Token);

			await run2;
		}

		if (m_scs.KeepOpen) {
			await AnsiConsole.ConfirmAsync("Exit", cancellationToken: m_cts.Token);
		}

		return BaseOSIntegration.EC_OK;
	}


	private async Task RunInteractiveAsync(CancellationToken ct = default)
	{
		string cmd = null;

		do {
			cmd = GetCommandPrompt();

			var res = GetEnginePrompt();

			var sri = GetResultItemPrompt(res);


			if (sri is not null) {
				s_logger.LogTrace("Interactive: {ResItem}", sri);

				if (cmd == R2.Chc_Open) {
					SearchClient.OpenResult(sri.Url);
					continue;
				}
				else if (cmd == R2.Chc_Scan) {
					var imgScanOk = await ShowImageScanResultsAsync(sri, ct);

					if (imgScanOk) {
						continue;
					}
				}

				if (cmd == R2.Chc_Calc) {
					if (sri is null || sri.Similarity.HasValue) {
						continue;
					}

					if (sri.HasHash) {
						await CalcResultAsync(sri);
					}


					continue;
				}

				if (cmd == R2.Chc_Preview) {

					//todo
					Stream str = null;

					if (!sri.HasBytes) {
						continue;
					}


					//todo
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

							s_logger.LogDebug("{CacheItem} {RemRes}", arguments.CacheItem, arguments.RemovedReason);
							return;
						}
					};

					// string key = sri.Url.ToString();

					var key = sri.Url;
					var val = m_cache.Get(key);

					if (val is not Stream) {
						str = sri.GetStream();
						m_cache.Set(key, str, cip);
					}

					var (w, h) = (AnsiConsole.Profile.Width, AnsiConsole.Profile.Height);

					var ci = new CanvasImage(str);

					AnsiConsole.AlternateScreen(() => ShowPreview(ci, sri));
				}

				if (cmd == R2.Chc_Download) { }
			}

		cont:
			continue;
		} while (cmd != R2.Chc_Exit);
	}

	// TODO: Rewrite RunSearch counterparts

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

			// m_results.Add(result);


			/*var txt  = new Text(result.Engine.Name, GetEngineColor(result.Engine.EngineOption));
			var txt2 = new Text($"{result.Results.Count}");

			m_mainTable.AddRow(txt, txt2);*/


			var rows = CreateResultRows(result);

			m_results[result] = m_table.Rows.Count;

			foreach (IRenderable[] row in rows) {
				m_table.AddRow(row);
			}


			c.Refresh();

		}

		await search;

	}


	private async Task RunCompletionCommandAsync([CBN] object o)
	{
		var command = Cli.Wrap(m_scs.Command);

		var cmdArgs      = m_scs.CommandArguments;
		var stdOutBuffer = new StringBuilder();
		var stdErrBuffer = new StringBuilder();

		if (!String.IsNullOrWhiteSpace(cmdArgs)) {
			command = command.WithArguments(cmdArgs);
		}

		command = command.WithStandardErrorPipe(PipeTarget.ToStringBuilder(stdErrBuffer))
			.WithStandardOutputPipe(PipeTarget.ToStringBuilder(stdOutBuffer));

		var commandTask = command.ExecuteAsync(m_cts.Token);

		AnsiConsole.WriteLine($"Process id: {commandTask.ProcessId}");

		var result = await commandTask;

		AnsiConsole.WriteLine($"Process successful: {result.IsSuccess}");
	}

	private void WriteOutputFile([CBN] object o)
	{
		var fw = File.OpenWrite(m_scs.OutputFile);

		var sw = new StreamWriter(fw)
		{
			AutoFlush = true
		};

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

		sw.Dispose();
		fw.Dispose();

		AnsiConsole.WriteLine($"Wrote to {m_scs.OutputFile}");
	}

#endregion

#region

	private Task CalcResultAsync(SearchResultItem ui)
	{
		return AnsiConsole.Live(m_table).StartAsync(async f =>
		{
			var row = GetRowForItem(ui);
			ui.CalculateSimilarity(Query.Source);

			m_table.Rows.Update(row, 2, new Text(ui.Similarity.ToString()));
			f.Refresh();
		});


	}

	private async ValueTask<bool> ShowImageScanResultsAsync(SearchResultItem item, CancellationToken token = default)
	{
		bool ok = true;

		await AnsiConsole.Live(m_table).StartAsync(async (f) =>
		{
			IList<SearchResultItem> resOk = [];

			if (!item.HasImage) {

				// var ok = await r.ScanAsync();
				s_logger.LogTrace("Scanning {Item}", item);
				resOk = await item.ScanAsync(token);

				/*if (!resOk) {
					// Debugger.Break();
					ok = false;
					return;

				}*/
			}
			else {
				return;
			}


			int i       = 0;
			var row     = GetRowForItem(item);
			var rowOrig = row;
			var delta   = resOk.Count;
			var idx     = item.Root.Results.IndexOf(item);
			item.Root.Results.InsertRange(idx, resOk);

			foreach (var ui in resOk) {
				m_table.InsertRow(++row, CreateUniImageRow(ui, idx, i++));
			}

			foreach (var kv in m_results) {
				if (kv.Value >= rowOrig) {
					m_results[kv.Key] = kv.Value + delta;

				}
			}

		});

		return ok;

	}

	private void ShowPreview(CanvasImage ci, SearchResultItem ui)
	{
		AnsiConsole.Clear();
		(AnsiConsole.Profile.Width, AC.Profile.Height) = (ui.Image.Width, ui.Image.Height);

		// Console.SetWindowSize(ui.Image.Width, ui.Image.Height);
		// ci.MaxWidth ??= ci.Width;
		ci.MaxWidth ??= ui.Image.Width;

		var panel = new Panel(ci) { Expand = true, };
		var (w, h) = (AnsiConsole.Profile.Width, AC.Profile.Height);

		AnsiConsole.Live(panel).Start((ldc) =>
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

							// AnsiConsole.Profile.Width  = ci.Width;
							// AnsiConsole.Profile.Height = ci.Height;

							try {
								// Console.SetBufferSize(ci.Width, ci.Height);
								// Console.SetWindowSize(ui.Image.Width, ui.Image.Height);
							}
							catch (Exception e) { }

							ci.MaxWidth = ui.Image.Width;
							break;
					}

					if (mw != 0 || pw != 0) {
						ci.PixelWidth = Math.Clamp(ci.PixelWidth      + pw, 0, ci.Width);
						ci.MaxWidth   = Math.Clamp((ci.MaxWidth ?? 0) + mw, 0, AnsiConsole.Profile.Width);

					}
				}

				// lay["btm"].Update(new Text($"{ci.MaxWidth} / {ci.PixelWidth}"));

				// Console.Title = $"{ci.MaxWidth} / {ci.PixelWidth}";
				panel.Header = new PanelHeader($"{ci.MaxWidth} / {ci.PixelWidth}");
			}
		});

		// (AnsiConsole.Profile.Width, AnsiConsole.Profile.Height) = (w, h);


		return;
	}

#endregion

#region

	/*
	private SearchResultItem GetItemForUni(UniImage ui, out int uniIndex)
	{
		foreach (SearchResult sr in m_results.Keys) {
			foreach (var sri in sr.Results) {
				if (sri.HasImage) {
					for (int k = 0; k < sri.Uni.Count; k++) {
						UniImage ui2 = sri.Uni[k];

						if (ui == ui2) {
							uniIndex = k;
							return sri;
						}
					}
				}
			}
		}

		uniIndex = BaseOSIntegration.EC_ERROR;

		return null;
	}
	*/

	private int GetRowForItem(SearchResultItem sri)
	{
		int a = 0, b = 0;

		a = m_results[sri.Root];
		b = sri.Root.Results.IndexOf(sri);

		return a + b;

	}

	/*private int GetRowForUni(UniImage ui)
	{

		SearchResultItem sri = GetItemForUni(ui, out int c);
		c++; // TODO NOTE: +1 for #.0 when #

		int a = m_results[sri.Root];
		int b = sri.Root.Results.IndexOf(sri);

		return a + b + c;
	}*/

#endregion


	[ContractAnnotation("=> halt")]
	private void OnCancelKeyPress(object sender, ConsoleCancelEventArgs args)
	{
		AnsiConsole.MarkupLine($"[red]Cancellation requested[/]");
		AnsiConsole.Clear();
		m_cts.Cancel();
		args.Cancel = false;

		Environment.Exit(BaseOSIntegration.EC_ERROR);
	}

	public override ValidationResult Validate(CommandContext context, SearchCommandSettings settings)
	{
		var r = base.Validate(context, settings);
		return r;

	}

	public void Dispose()
	{
		Debug.WriteLine($"Disposing {nameof(SearchCommand)}");

		foreach (var sr in m_results.Keys) {
			sr.Dispose();
		}

		ConsoleFormat.Prm_Num.Validator = null;
		ConsoleFormat.Prm_Engine.Choices.Clear();
		m_results.Clear();
		m_cts.Dispose();
		m_scs = null;
		Client.Dispose();
		Query.Dispose();
	}

}