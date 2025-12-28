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

	private static readonly ConcurrentDictionary<SearchResultItem, int> tbl = new();

	private static Selection GetSelectionChoice(SearchResult res)
	{
		Selection ret;

		ConsoleElements.Prm_Num2.Validator = str =>
		{
			ret = Parse(str, res);

			if (ret == null) {
				return ValidationResult.Error();
			}

			return ValidationResult.Success();
		};


		var val = AnsiConsole.Prompt(ConsoleElements.Prm_Num2);
		var sri = Parse(val, res);
		return sri;


	}

	internal record Selection
	{

		public SearchResultItem Item { get; }

		// public SearchResultItem Scanned { get; }

		public int ItemIdx { get; }

		public int ScanIdx { get; }

		public bool IsScannedItem { get; }

		public Selection(SearchResultItem item, int itemIdx, int scanIdx, bool isScanned)
		{
			Item          = item;
			ItemIdx       = itemIdx;
			ScanIdx       = scanIdx;
			IsScannedItem = isScanned;
		}

		public int Index()
		{
			int i      = 0, j = 0;
			var scnIdx = 0;
			int root   = 0;
			int t      = 0;

			if (Item.IsChild) {
				// scnIdx = Item.Parent.ScannedItems.IndexOf(Item);
				root = Item.Parent.Root.Results.IndexOf(Item.Parent);

				scnIdx++;

			}
			else {
				root = Item.Root.Results.IndexOf(Item);

			}

			// t = Item.Parent.Root.Results[..(root + 1)].Sum(rr => rr.ScannedItems.Count);

			for (int k = 0; k < root; k++) {

				var result  = Item.Parent.Root.Results[k];
				var scnItm  = result.ScannedItems;
				var scnIdx2 = scnItm.IndexOf(Item);

				if (scnIdx2 == -1) {
					t += scnItm.Count;
				}
				else {
					t += scnIdx2;
				}

			}

			return root + scnIdx + t;
		}

	}

	[CBN]
	static Selection Parse(string str, SearchResult sr)
	{
		var spl = str.Split('.');

		SearchResultItem sri = null, sri2 = null;

		var  resIdx    = 0;
		var  scnIdx    = -1;
		bool isScanned = false;


		if (sr.Results.TryParseIndex(spl[0], out resIdx, out sri)) {
			if (spl.Length == 2) {
				if (sri.ScannedItems.TryParseIndex(spl[1], out scnIdx, out sri2)) {
					sri       = sri2;
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

		var style = new Style(link: sri.Url, foreground: ConsoleElements.GetEngineColor(result.Engine.EngineOption));

		return
		[
			new Text($"#{idx}.{subIdx}", style),
			new Text(Markup.Escape(sri.Url)),
			CreateResultItemSimilarityCell(sri),
			ConsoleElements.Txt_Empty,
			CreateResultItemResolutionRow(sri)
		];
	}

	private static SpcTable CreateResultTable()
	{
		var col = new TableColumn[]
		{
			new(new Text("Result", ConsoleElements.Sty_ResultHeader)),
			new(new Text("URL", ConsoleElements.Sty_ResultHeader)),
			new(new Text("Similarity", ConsoleElements.Sty_ResultHeader)),
			new(new Text("Artist", ConsoleElements.Sty_ResultHeader)),
			new(new Text("Resolution", ConsoleElements.Sty_ResultHeader)),

		};

		var tb = new SpcTable()
		{
			Caption     = new TableTitle("Results", ConsoleElements.Sty_ResultHeader),
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
			new(new Text("Engine", ConsoleElements.Sty_ResultHeader)),
			new(new Text("Results", ConsoleElements.Sty_ResultHeader)),

		};

		var tb = new SpcTable()
		{
			Caption     = new TableTitle("Results", ConsoleElements.Sty_ResultHeader),
			Border      = TableBorder.Simple,
			ShowHeaders = true,
		};

		tb.AddColumns(col);

		return tb;
	}

	private static IEnumerable<IRenderable> CreateMainRows(SearchResult result)
	{
		Style style = ConsoleElements.GetEngineColor(result.Engine.EngineOption);

		return [new Text($"{result.Engine.Name}", style), new Text($"{result.Results.Count}")];
	}

	private static IEnumerable<IRenderable[]> CreateResultRows(SearchResult result)
	{
		Style style = ConsoleElements.GetEngineColor(result.Engine.EngineOption);


		for (int i = 0; i < result.Results.Count; i++) {
			var res = result.Results[i];
			
			yield return CreateResultItemRow(res, i, style);

			/*if (res.IsChild) {
				for (int j = 0; j < res.Parent.ScannedItems.Count; j++) {
					yield return CreateItemRow(res.Parent.ScannedItems[j], i, j);

				}
			}
			else { }*/

			tbl.TryAdd(res, i);
		}

	}


	private static IRenderable CreateResultItemResolutionRow(SearchResultItem sri)
		=> (sri.HasDimensions) ? new Text($"{sri.Width}x{sri.Height}") : ConsoleElements.Txt_NA;

	private static IRenderable CreateResultItemSimilarityCell(SearchResultItem sri)
		=> sri.Similarity.HasValue ? new Text($"{sri.Similarity}") : ConsoleElements.Txt_NA;

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
			url       = ConsoleElements.Txt_NA;
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