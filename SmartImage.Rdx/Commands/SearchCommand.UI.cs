using SmartImage.Lib.Engines.Results;
using SmartImage.Lib.Images.Uni;
using SmartImage.Lib.Utilities;
using SmartImage.Rdx.Shell;
using Spectre.Console;
using Spectre.Console.Rendering;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#nullable disable
namespace SmartImage.Rdx.Commands;

public sealed partial class SearchCommand
{

	internal const int ROW_NUM        = 0;
	internal const int ROW_URL        = 1;
	internal const int ROW_SIMILARITY = 2;
	internal const int ROW_ARTIST     = 3;
	internal const int ROW_WH         = 4;

#region

	/*private int GetRowForItem(SearchResultItem sri)
	{
		int rootIdx = 0, scanIdxOfs = 0, c = 0;

		var iterSrc = sri.IsChild ? sri.Parent : sri;
		rootIdx = sri.Root.Results.IndexOf(iterSrc);


		for (int i = 0; i < rootIdx; i++) {
			var item = iterSrc.Root.Results[i];

			// var item = iterSrc;
			var sc = item.ScannedItems.IndexOf(sri);
			scanIdxOfs += sc == -1 ? item.ScannedItems.Count : sc;

		}

		// scanIdxOfs = iterSrc.ScannedItems.IndexOf(sri);

		/*for (int i = 0; i < iterSrc.Root.Results.Count; i++) {
			var iterRR  = iterSrc.Root.Results[i];
			var iterRRC = iterRR.ScannedItems.IndexOf(sri);

		}#1#


		/*if (sri.IsChild && !sri.HasScannedItems) {
			c++;
		}#1#


		return rootIdx + scanIdxOfs + c;

	}*/

	private static readonly ConcurrentDictionary<SearchResultItem, int> tbl;

	private static Selection GetResultItemIndexes(SearchResult res)
	{
		Selection ret;

		ConsoleFormat.Prm_Num2.Validator = str =>
		{
			ret = Parse(str, res);

			if (ret == null) {
				return ValidationResult.Error();
			}

			return ValidationResult.Success();
		};


		var val = AnsiConsole.Prompt(ConsoleFormat.Prm_Num2);
		var sri = Parse(val, res);
		return sri;


	}

	internal record Selection
	{

		public SearchResultItem Item { get; }

		// public SearchResultItem Scanned { get; }

		public int ItemIdx { get; }

		public int ScanIdx { get; }

		public bool IsScannedItem {get;}

		public Selection(SearchResultItem item, int itemIdx, int scanIdx, bool isScanned)
		{
			Item    = item;
			ItemIdx = itemIdx;
			ScanIdx = scanIdx;
			IsScannedItem = isScanned;
		}

		public int Index()
		{
			var root = Item.Root.Results.IndexOf(IsScannedItem ? Item.Parent : Item);
			// Debug.Assert(root == ItemIdx);

			var scnIdx = 0;

			if (IsScannedItem) {
				scnIdx = Item.Parent.ScannedItems.IndexOf(Item);
				Debug.Assert(scnIdx == ScanIdx);
				scnIdx++;
			}

			return root + scnIdx;
		}

	}

	[CBN]
	static Selection Parse(string str, SearchResult sr)
	{
		var spl = str.Split('.');

		SearchResultItem sri = null, sri2 = null;

		var resIdx = 0;
		var scnIdx = -1;
		bool isScanned = false;


		if (sr.Results.TryParseIndex(spl[0], out resIdx, out sri)) {
			if (spl.Length == 2) {
				if (sri.ScannedItems.TryParseIndex(spl[1], out scnIdx, out sri2)) {
					sri = sri2;
					isScanned = true;
				}
				// if (sr.ScannedResults[sri].TryParseIndex(spl[1], out scnIdx, out sri2)) { }

			}
			else { }

		}
		else { }

		return new Selection(sri, resIdx, scnIdx, isScanned);
	}

#endregion

#region

	private static IRenderable[] CreateItemRow(SearchResultItem sri, int idx, int subIdx)
	{
		// var url = ui is UniImageUri uiu ? uiu.Url.ToString() : String.Empty;

		var result = sri.Root;

		var style = new Style(link: sri.Url, foreground: ConsoleFormat.GetEngineColor(result.Engine.EngineOption));

		return
		[
			new Text($"#{idx}.{subIdx}", style),
			new Text(Markup.Escape(sri.Url)),
			CreateResultItemSimilarityCell(sri),
			ConsoleFormat.Txt_Empty,
			CreateResultItemResolutionRow(sri)
		];
	}

	private static SpcTable CreateResultTable()
	{
		var col = new TableColumn[]
		{
			new(new Text("Result", ConsoleFormat.Sty_ResultHeader)),
			new(new Text("URL", ConsoleFormat.Sty_ResultHeader)),
			new(new Text("Similarity", ConsoleFormat.Sty_ResultHeader)),
			new(new Text("Artist", ConsoleFormat.Sty_ResultHeader)),
			new(new Text("Resolution", ConsoleFormat.Sty_ResultHeader)),

		};

		var tb = new SpcTable()
		{
			Caption     = new TableTitle("Results", ConsoleFormat.Sty_ResultHeader),
			Border      = TableBorder.Simple,
			ShowHeaders = true,
		};

		tb.AddColumns(col);

		return tb;
	}

	private static SpcTable CreateMainTable()
	{
		var col = new TableColumn[]
		{
			new(new Text("Engine", ConsoleFormat.Sty_ResultHeader)),
			new(new Text("Results", ConsoleFormat.Sty_ResultHeader)),

		};

		var tb = new SpcTable()
		{
			Caption     = new TableTitle("Results", ConsoleFormat.Sty_ResultHeader),
			Border      = TableBorder.Simple,
			ShowHeaders = true,
		};

		tb.AddColumns(col);

		return tb;
	}

	private static IEnumerable<IRenderable> CreateMainRows(SearchResult result)
	{
		Style style = ConsoleFormat.GetEngineColor(result.Engine.EngineOption);

		return [new Text($"{result.Engine.Name}", style), new Text($"{result.Results.Count}")];
	}

	private static IEnumerable<IRenderable[]> CreateResultRows(SearchResult result)
	{
		Style style = ConsoleFormat.GetEngineColor(result.Engine.EngineOption);


		for (int i = 0; i < result.Results.Count; i++) {
			var res = result.Results[i];

			yield return CreateResultItemRow(res, i, style);
		}

	}


	private static IRenderable CreateResultItemResolutionRow(SearchResultItem sri)
		=> (sri.HasDimensions) ? new Text($"{sri.Width}x{sri.Height}") : ConsoleFormat.Txt_NA;

	private static IRenderable CreateResultItemSimilarityCell(SearchResultItem sri)
		=> sri.Similarity.HasValue ? new Text($"{sri.Similarity}") : ConsoleFormat.Txt_NA;

	private static IRenderable[] CreateResultItemRow(SearchResultItem sri, int i, Style style)
	{
		IRenderable url;
		var         link = sri.Url;
		Style       linkStyle;

		if (link != null) {
			linkStyle = new Style(link: link);
			url       = new Markup(Markup.Escape(link.ToString()), linkStyle);
		}
		else {
			url       = ConsoleFormat.Txt_NA;
			linkStyle = style;
		}

		var name   = new Text($"#{i}", style);
		var sim    = CreateResultItemSimilarityCell(sri);
		var artist = new Text($"{sri.Artist}");
		var wh     = CreateResultItemResolutionRow(sri);

		return [name, url, sim, artist, wh];
	}

#endregion

}