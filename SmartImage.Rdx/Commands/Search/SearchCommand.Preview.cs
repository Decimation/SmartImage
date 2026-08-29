using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Caching;
using System.Text;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp.Processing;
using SmartImage.Lib.Engines.Results;
using SmartImage.Lib.Images;
using SmartImage.Rdx.Shell;
using Spectre.Console;

namespace SmartImage.Rdx.Commands.Search;

public partial class SearchCommand
{

	private readonly MemoryCache m_previewCanvasCache;

	private CanvasImage m_queryCanvasImg;

	private static readonly PreviewConsoleAction[] s_previewActions =
	[
		new()
		{
			Description = "Resize (internal)",
			Key         = ConsoleKey.S,
			Func = static (ci, sri) =>
			{
				ci.Mutate(act =>
				{
					//
					act.Resize(sri.AllocImage.Image.Width, sri.AllocImage.Image.Height);
				});
				return false;
			},
		},
		new()
		{
			Description = "Resize (scale)",
			Key         = ConsoleKey.R,
			Func = static (ci, sri) =>
			{
				ci.Mutate(static act =>
				{
					var cs = act.GetCurrentSize();
					var ns = cs.ResizeByFactor(new SizeIS(AC.Profile.Width, AC.Profile.Height));
					act.Resize(ns);
				});

				ci.MaxWidth = null;
				return false;
			},
		},
		new()
		{
			Description = "Set max preview width to buffer width",
			Key         = ConsoleKey.M,
			Func = static (ci, sri) =>
			{
				ci.MaxWidth = AC.Profile.Width;
				return false;
			},
		},
		new()
		{
			Description = "Exit preview",
			Key         = ConsoleKey.Escape,
			Func        = static (_, _) => { return true; },
		}

	];

	private static readonly string s_previewDescription = s_previewActions.Aggregate(String.Empty, (s, kv) =>
	{
		//
		return s + Markup.Escape(($"[{kv.Key}] : {kv.Description}")) + " | ";
	});

	private static readonly CacheItemPolicy s_cacheItemPolicy = new CacheItemPolicy
	{
		AbsoluteExpiration = DateTimeOffset.Now + TimeSpan.FromMinutes(1),
		RemovedCallback = static arguments =>
		{
			s_logger.LogDebug("Cache item {CacheItem} removed: {RemRes}", arguments.CacheItem.Key, arguments.RemovedReason);
		}
	};

	private CanvasImage GetPreviewCanvasImage(ScannedResultItem sri)
	{
		Stream str = null;

		var key = sri.AllocImage.Value;
		var val = m_previewCanvasCache.Get(key);

		if (val is not CanvasImage ci) {
			str = sri.AllocImage.GetSource();

			if (str == Stream.Null || !sri.AllocImage.HasImage) {
				Trace.WriteLine($"No source for {key}!");
				return m_queryCanvasImg;
			}

			ci = new CanvasImage(str);

			m_previewCanvasCache.Set(key, ci, s_cacheItemPolicy);
		}

		Trace.Assert(ci != null);

		return ci;
	}

	private void ShowPreview(CanvasImage ci, ScannedResultItem sri)
	{
		var pnl = new Panel(ci)
		{
			Expand = true,
			Border = BoxBorder.None,
			Header = new PanelHeader($"{sri.AllocImage.Value}"),
		};

		var sriLayout = new Layout("Preview");

		sriLayout.SplitRows(
			new Layout("Image") { Ratio  = 2 },
			new Layout("Details") { Size = 2 }
		);

		// var grid = sri.GetItemInfoGrid();
		var infoGrid = new Grid { Expand = false, };
		infoGrid.AddColumns(2);
		infoGrid.AddRow("Keys", s_previewDescription);
		infoGrid.AddRow("ExtraData", sri.ToString());

		sriLayout["Image"].Update(pnl);
		sriLayout["Details"].Update(infoGrid);

		AnsiConsole.Live(sriLayout).Start(ldc =>
		{
			while (true) {
				// ci.MaxWidth = mw < 0 ? null : mw;
				ldc.Refresh();
				var cki = AnsiConsole.Console.Input.ReadKey(true);

				if (!cki.HasValue) {
					continue;
				}

				foreach (PreviewConsoleAction action in s_previewActions) {
					if (action.Key == cki.Value.Key) {
						var retVal = action.Func(ci, sri);

						if (retVal) {
							return;
						}
					}
				}
			}
		});


	}

}