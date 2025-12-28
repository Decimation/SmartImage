#nullable disable
using System.Collections.Concurrent;
using SmartImage.Lib.Engines.Results;
using SmartImage.Lib.Utilities;
using SmartImage.Rdx.Shell;
using Spectre.Console;

namespace SmartImage.Rdx.Commands.Search;

public sealed partial class SearchCommand
{

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

	internal static readonly ConcurrentDictionary<SearchResultItem, int> tbl = new();

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

	private static Selection GetSelectionChoice(SearchResult res)
	{
		Selection ret;

		Elements.Prm_Num2.Validator = str =>
		{
			ret = Parse(str, res);

			if (ret == null) {
				return ValidationResult.Error();
			}

			return ValidationResult.Success();
		};


		var val = AnsiConsole.Prompt(Elements.Prm_Num2);
		var sri = Parse(val, res);
		return sri;


	}

#endregion

}