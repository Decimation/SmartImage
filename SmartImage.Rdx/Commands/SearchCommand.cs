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

public sealed class SearchCommand : AsyncCommand<SearchCommandSettings>, IDisposable
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

	private readonly Layout m_root;

	private readonly Tree m_rootTree;

	private readonly ConcurrentDictionary<SearchResult, List<TreeNode>> m_rootTreeNodes;

	private readonly ConcurrentDictionary<int, int> m_results;

	static SearchCommand() { }

	public SearchCommand()
	{
		Config = new SearchConfig();

		// Config = (SearchConfig) cfg;
		Client = new SearchClient(Config);

		// Client.OnSearchComplete += OnSearchComplete;

		// Client.OnResultComplete   += OnResultComplete;
		m_cts           = new CancellationTokenSource();
		m_scs           = null;
		m_table         = CreateResultTable();
		m_root          = CreateRootLayout();
		m_rootTree      = CreateRootTree();
		m_rootTreeNodes = new ConcurrentDictionary<SearchResult, List<TreeNode>>();
		m_results       = new();
		m_cache         = new MemoryCache("Buf");


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
		/*Task run = AnsiConsole.Live(m_table)
			.StartAsync(c => RunSearchLiveAsync(c))*/
		;

		Task run = AnsiConsole.Live(m_root)
			.StartAsync(c => RunSearchLiveAsync2(c));

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
			// run2 = RunInteractiveAsync(m_cts.Token);
			run2 = RunInteractiveAsync2(m_cts.Token);

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

			var (sri, ui) = GetResultItemPrompt(res);


			if (sri is not null || ui is not null) {
				s_logger.LogTrace("Interactive: {ResItem} | {UniItem}", sri, ui);

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
					if (ui is null || ui.Similarity.HasValue) {
						continue;
					}

					if (ui.HasHash) {
						await CalcResultAsync(ui);
					}


					continue;
				}

				if (cmd == R2.Chc_Preview) {

					//todo
					Stream str;

					if (ui is not null) {
						str = ui.GetStream();
					}
					else if (sri.Thumbnail != null) {
						var thmbOk = await sri.LoadThumbnail(ct);

						if (thmbOk) {
							ui  = sri.Uni.Find(f => f.Value == sri.Thumbnail);
							str = ui.GetStream();
						}
						else {
							continue;
						}

					}
					else {
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

					var key = ui.Value;
					var val = m_cache.Get(key);

					if (val is not Stream) {
						m_cache.Set(key, str, cip);
					}

					var (w, h) = (AnsiConsole.Profile.Width, AnsiConsole.Profile.Height);

					var ci = new CanvasImage(str);

					AnsiConsole.AlternateScreen(() => ShowPreview(ci, ui));
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

	private Task CalcResultAsync(UniImage ui)
	{
		return AnsiConsole.Live(m_table).StartAsync(async f =>
		{
			var row = GetRowForUni(ui);
			ui.CalculateSimilarity(Query.Source);

			m_table.Rows.Update(row, 2, new Text(ui.Similarity.ToString()));
			f.Refresh();
		});


	}

	private SearchResultItem GetItemForUni(UniImage ui, out int uniIndex)
	{
		foreach (SearchResult sr in m_results.Keys) {
			foreach (var sri in sr.Results) {
				if (sri.HasUni) {
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

	private int GetRowForItem(SearchResultItem sri)
	{
		int a = 0, b = 0;

		a = m_results[sri.Root];
		b = sri.Root.Results.IndexOf(sri);

		return a + b;

	}

	private int GetRowForUni(UniImage ui)
	{

		SearchResultItem sri = GetItemForUni(ui, out int c);
		c++; // TODO NOTE: +1 for #.0 when #

		int a = m_results[sri.Root];
		int b = sri.Root.Results.IndexOf(sri);

		return a + b + c;
	}

	private async ValueTask<bool> ShowImageScanResultsAsync(SearchResultItem item, CancellationToken token = default)
	{
		bool ok = true;

		await AnsiConsole.Live(m_table).StartAsync(async (f) =>
		{
			if (!item.HasUni) {

				// var ok = await r.ScanAsync();
				s_logger.LogTrace("Scanning {Item}", item);
				var resOk = await item.ScanAsync(token);

				if (!resOk) {
					// Debugger.Break();
					ok = false;
					return;

				}
			}
			else {
				return;
			}

			int i       = 0;
			var row     = GetRowForItem(item);
			var rowOrig = row;
			var delta   = item.Uni.Count;
			var idx     = item.Root.Results.IndexOf(item);

			foreach (var ui in item.Uni) {
				m_table.InsertRow(++row, CreateUniImageRow(ui, item, idx, i++));
			}

			foreach (var kv in m_results) {
				if (kv.Value >= rowOrig) {
					m_results[kv.Key] = kv.Value + delta;

				}
			}

		});

		return ok;

	}

	public static Image ResizeToConsole(ISImage image)
	{
		// Get console dimensions
		int maxWidth  = AnsiConsole.Profile.Width;
		int maxHeight = AnsiConsole.Profile.Height;

		// Original image dimensions
		int origWidth  = image.Width;
		int origHeight = image.Height;

		// Calculate scale factor to fit within console
		double widthRatio  = (double) maxWidth  / origWidth;
		double heightRatio = (double) maxHeight / origHeight;
		double scale       = Math.Min(widthRatio, heightRatio);

		// If image is smaller than console, no need to resize
		if (scale >= 1.0)
			return image.Clone();

		int newWidth  = (int) (origWidth  * scale);
		int newHeight = (int) (origHeight * scale);

		// Resize the image
		var resized = image.Clone(ctx => ctx.Resize(new ResizeOptions()
		{
			Size = new Size(newWidth, newHeight),

		}));
		return resized;
	}

	private void ShowPreview(CanvasImage ci, UniImage ui)
	{
		// (AnsiConsole.Profile.Width, AC.Profile.Height) = (ui.Image.Width, ui.Image.Height);
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
							// AnsiConsole.Console.Profile.Width  = ui.Image.Width;
							// AnsiConsole.Console.Profile.Height = ui.Image.Height;
							AnsiConsole.Profile.Width  = ci.Width;
							AnsiConsole.Profile.Height = ci.Height;
							Console.SetBufferSize(ci.Width, ci.Height);

							// Console.SetWindowSize(ci.Width, ci.Height);

							// ci.MaxWidth                        = ui.Image.Width;
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

		(AnsiConsole.Profile.Width, AnsiConsole.Profile.Height) = (w, h);


		return;
	}

#region Prompts

	private (SearchResultItem, UniImage) GetResultItemPrompt(SearchResult res)
	{
		(SearchResultItem, UniImage) ret;

		ConsoleFormat.Prm_Num2.Validator = str =>
		{
			ret = Parse(str);

			if (ret is (null, null)) {
				return ValidationResult.Error();
			}

			return ValidationResult.Success();
		};


		var val = AnsiConsole.Prompt(ConsoleFormat.Prm_Num2);
		return Parse(val);

		(SearchResultItem, UniImage) Parse(string str)
		{
			var spl = str.Split('.');
			int i;

			SearchResultItem sri = null;
			UniImage         ui  = UniImage.Null;

			if (res.Results.TryParseIndex(spl[0], out sri)) {

				if (spl.Length == 2) {

					if (sri.Uni.TryParseIndex(spl[1], out ui)) { }
				}
				else { }

			}
			else { }

			return (sri, ui);
		}
	}

	private int GetNumberPrompt(SearchResult result)
	{
		ConsoleFormat.Prm_Num.Validator = i =>
		{
			if (i < result.Results.Count && i >= 0) {
				return ValidationResult.Success();
			}

			return ValidationResult.Error("Out of range");
		};


		return AnsiConsole.Prompt(ConsoleFormat.Prm_Num);
	}

	private string GetCommandPrompt()
	{
		return AnsiConsole.Prompt(ConsoleFormat.Prm_Command);
	}

	private SearchResult GetEnginePrompt()
	{
		if (Client.IsComplete && !ConsoleFormat.Prm_Engine.Choices.Any()) {
			ConsoleFormat.Prm_Engine.Choices.AddRange(m_results.Keys);
		}

		return AnsiConsole.Prompt(ConsoleFormat.Prm_Engine);
	}

#endregion

#region

	private static IRenderable[] CreateUniImageRow(UniImage ui, SearchResultItem sri, int idx, int subIdx)
	{
		// var url = ui is UniImageUri uiu ? uiu.Url.ToString() : String.Empty;

		var result = sri.Root;

		var style = new Style(link: ui.Value,
		                      foreground: ConsoleFormat.GetEngineColor(result.Engine.EngineOption));

		return
		[
			new Text($"{result.Engine.Name} #{idx}.{subIdx}", style),
			new Text(Markup.Escape(ui.Value)),
			ConsoleFormat.Txt_Empty,
			ConsoleFormat.Txt_Empty,
			ConsoleFormat.Txt_Empty
		];
	}

	private static STable CreateResultTable()
	{
		var col = new TableColumn[]
		{
			new("Result"),
			new("URL"),
			new("Similarity"),
			new("Artist"),
			new("Site"),

		};

		var tb = new STable()
		{
			Caption     = new TableTitle("Results", new Style(decoration: Decoration.Bold)),
			Border      = TableBorder.Simple,
			ShowHeaders = true,
		};

		tb.AddColumns(col);

		return tb;
	}

	private static IEnumerable<IRenderable[]> CreateResultRows(SearchResult result)
	{
		Style style = ConsoleFormat.GetEngineColor(result.Engine.EngineOption);

		/*var lr   = style.Foreground.GetLuminance();
		var lrr  = style.Foreground.GetContrastRatio(SpcColor.White);
		var lrr2 = style.Foreground.GetContrastRatio(SpcColor.Black);*/

		// Debug.WriteLine($"{lr} {lrr} {lrr2}");

		for (int i = 0; i < result.Results.Count; i++) {
			var res = result.Results[i];

			yield return CreateResultItemRows(res, i, style);
		}

	}

	private static IRenderable[] CreateResultItemRows(SearchResultItem res, int i, Style style)
	{
		IRenderable url;
		var         link = res.Url;
		Style       linkStyle;

		if (link != null) {
			linkStyle = new Style(link: link);
			url       = new Markup(Markup.Escape(link.ToString()), linkStyle);
		}
		else {
			url       = ConsoleFormat.Txt_NA;
			linkStyle = style;
		}

		var name = new Text($"{res.Root.Engine.Name} #{i}", style);

		var sim    = new Text($"{res.Similarity}");
		var artist = new Text($"{res.Artist}");
		var site   = new Text($"{res.Site}");
		return [name, url, sim, artist, site];
	}

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


#region

	private async Task RunSearchLiveAsync2(LiveDisplayContext c, CancellationToken token = default)
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

			// var      tree  = CreateResultTree(result);

			SpcColor color = ConsoleFormat.GetEngineColor(result.Engine.EngineOption);
			Style    style = new Style(color);

			var nodes = CreateTreeNodes(result, style);
			m_rootTreeNodes.TryAdd(result, nodes);

			var text     = new Text($"{result.Engine.Name} ({result.Results.Count})", new Style(color));
			var treeNode = new TreeNode(text);

			m_rootTree.AddNode(treeNode);
			var panel = new Panel(m_rootTree) { Header = new PanelHeader($"Results ({m_results.Count})"), Expand = true };

			m_root["Left"].Update(panel);

			// m_root["Bottom"].Update();
			// c.UpdateTarget(panel);

			c.Refresh();
		}

		await search;
	}

	private Panel CreatePanel(SearchResult result)
	{
		var tr    = CreateResultTree(result);
		var nodes = m_rootTreeNodes[result];
		tr.AddNodes(nodes);
		var panel = new Panel(tr) { Header = new PanelHeader($"Results ({nodes.Count})"), Expand = true };
		return panel;
	}

	private static Tree CreateResultTree(SearchResult result)
	{
		SpcColor color = ConsoleFormat.GetEngineColor(result.Engine.EngineOption);
		Style    style = new Style(color, decoration: Decoration.Underline);

		/*var lr   = style.Foreground.GetLuminance();
		var lrr  = style.Foreground.GetContrastRatio(SpcColor.White);
		var lrr2 = style.Foreground.GetContrastRatio(SpcColor.Black);*/

		// Debug.WriteLine($"{lr} {lrr} {lrr2}");
		var text = new Text($"{result.Engine.Name} ({result.Results.Count})", style);

		var tr = new Tree(text) { Expanded = true };

		// var nodes = CreateTreeNodes(result, style);
		// tr.Nodes.AddRange(nodes);

		return tr;
	}

	private List<TreeNode> CreateTreeNodes(SearchResult result, Style style)
	{
		return m_rootTreeNodes.GetOrAdd(result, result.Results.SelectMany((res, i) => CreateResultTreeNodes(res, i, style)).ToList());
	}

	private static IEnumerable<TreeNode> CreateResultTreeNodes(SearchResultItem res, int i, Style style)
	{

		IRenderable url;
		var         link = res.Url;

		var other = new Style(link: link);
		var name  = new Text($"#{i} {res.Similarity}", style.Combine(other)) { };

		if (link != null) {
			url = new Markup(Markup.Escape(link.ToString()), other);
		}
		else {
			url = ConsoleFormat.Txt_NA;
		}

		var sim    = new Text($"{res.Similarity}");
		var artist = new Text($"{res.Artist}");
		var site   = new Text($"{res.Site}");
		return [new TreeNode(name)];
	}

	private static Tree CreateRootTree()
	{
		return new Tree(new Text("Root Results", ConsoleFormat.Sty_RootResults))
			{ Guide = TreeGuide.Line };
	}

	public Layout CreateRootLayout()
	{
		var layout = new Layout("Root") { }
			.SplitColumns(
				new Layout("Left"),
				new Layout("Right")
					.SplitRows(
						new Layout("Top"),
						new Layout("Bottom")));


		return layout;

	}

	private async Task<ConsoleKeyInfo?> ReadKey(CancellationToken ct, Func<ConsoleKeyInfo, bool> fn)
	{
		ConsoleKeyInfo? k;

		do {
			k = await AC.Console.Input.ReadKeyAsync(true, ct);

			if (k.HasValue && fn(k.Value)) {
				return k;
			}

		} while (AnsiConsole.Console.Input.IsKeyAvailable() && k is not { Key: ConsoleKey.Escape });

		return k;
	}

	private async Task RunInteractiveAsync2(CancellationToken ct = default)
	{
		AnsiConsole.Live(m_root).StartAsync(async ctx =>
		{
			string cmd = null;
			TreeNode n = null;
			int ni = 0;
			while (true) {
				ConsoleKeyInfo? cki = null;

				while (AC.Console.Input.IsKeyAvailable()) {
					cki = await AC.Console.Input.ReadKeyAsync(true, ct);
				}


				//
				
				if (cki.Value is { Key: ConsoleKey.UpArrow}) {
					ni = (ni + 1) % m_rootTree.Nodes.Count;
				}
				if (cki.Value is { Key: ConsoleKey.DownArrow}) {
					ni = (ni - 1) % m_rootTree.Nodes.Count;
				}

				


			}
		});


		SearchResult sell = await GetEngineSelection2(ct);

		var panel = CreatePanel(sell);
		m_root["Left"].Update(panel);

		AnsiConsole.Clear();
		AnsiConsole.Write(m_root);

		(SearchResultItem sri, UniImage uni) = GetResultItemPrompt(sell);

		if (sri != null) {
			var t = new Grid();
			t.AddColumns(2);

			if (!String.IsNullOrWhiteSpace(sri.Url)) {
				t.AddRow([nameof(SearchResultItem.Url), sri.Url]);
			}

			if (!String.IsNullOrWhiteSpace(sri.Artist)) {
				t.AddRow([nameof(SearchResultItem.Artist), sri.Artist]);
			}

			m_root["Bottom"].Update(new Panel(t));

		}

		string inp = null;

		do {
			AnsiConsole.Clear();
			AnsiConsole.Write(m_root);
			inp = await AnsiConsole.AskAsync<string>(">", ct);
			var entries = inp.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

		} while (!inp.Contains("exit") && !ct.IsCancellationRequested);


	}

#endregion

}