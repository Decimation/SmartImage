// Author: Deci | Project: SmartImage.Rdx | Name: ShellSelection.cs
// Date: 2026/01/10 @ 00:01:54

#nullable disable
using SmartImage;
using SmartImage.Lib.Engines.Results;
using SmartImage.Lib.Utilities;
using SmartImage.Rdx.Shell;
using Spectre.Console;

namespace SmartImage.Rdx.Shell;

internal record ShellSelection
{

	public IResultItem Item { get; }

	public int ItemIdx { get; }

	public int ScanIdx { get; }

	public bool IsScannedItem { get; }

	internal ShellSelection(IResultItem item, int itemIdx, int scanIdx, bool isScanned)
	{
		Item          = item;
		ItemIdx       = itemIdx;
		ScanIdx       = scanIdx;
		IsScannedItem = isScanned;
	}

	public static int Index(IResultItem item)
	{
		var scnIdx = 0;
		int root   = 0;
		int t      = 0;

		if (item.IsChild) {
			// scnIdx = Item.Parent.ScannedItems.IndexOf(Item);
			root = item.Parent.Root.Results.IndexOf(item.Parent);

			scnIdx++;

		}
		else {
			root = item.Root.Results.IndexOf(item);

		}

		for (int k = 0; k < root; k++) {

			var result = item.Parent.Root.Results[k] as SearchResultItem;

			var scnItm  = result.ScannedItems;
			var scnIdx2 = scnItm.IndexOf(item);

			if (scnIdx2 == -1) {
				t += scnItm.Count;
			}
			else {
				t += scnIdx2;
			}

		}

		return root + scnIdx + t;
	}

	public static int Index2(IResultItem item)
	{

		var rg        = item.Index + item.Root.Results[..item.Index].OfType<SearchResultItem>().Sum(x => x.ScannedItems.Count);
		var isScanned = item is ScannedResultItem;
		var scnItem   = isScanned ? (ScannedResultItem) item : null;
		var scanIdx   = isScanned ? scnItem.Index : 0;

		var sumIdx  = rg + scanIdx + (isScanned ? ((scanIdx == 0) ? 1 : 0) : 0);
		var sumIdx2 = rg + scanIdx + (isScanned ? ((scanIdx == 0) ? 1 : 0) : 1);

		return sumIdx2;
	}

	public int Index()
	{
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

		for (int k = 0; k < root; k++) {

			var result = Item.Parent.Root.Results[k] as SearchResultItem;

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

	public int Index2()
	{

		var rg = ItemIdx + Item.Root.Results[..ItemIdx].OfType<SearchResultItem>().Sum(x => x.ScannedItems.Count);

		var sumIdx  = rg + ScanIdx + (IsScannedItem ? ((ScanIdx == 0) ? 1 : 0) : 0);
		var sumIdx2 = rg + ScanIdx + (IsScannedItem ? ((ScanIdx == 0) ? 1 : 0) : 1);

		return sumIdx2;
	}

	[CBN]
	private static ShellSelection Parse(string str, SearchResult sr)
	{
		var spl = str.Split('.');

		IResultItem sri = null, sri2 = null;

		var  resIdx    = 0;
		var  scnIdx    = -1;
		bool isScanned = false;

		if (sr.Results.TryParseIndex(spl[0], out resIdx, out sri)) {
			if (spl.Length == 2) {
				if (sri is SearchResultItem { } sriOrig && sriOrig.ScannedItems.TryParseIndex(spl[1], out scnIdx, out sri2)) {
					sri       = sri2;
					isScanned = true;
				}
			}
		}

		return new ShellSelection(sri, resIdx, scnIdx, isScanned);
	}

	public static ShellSelection GetSelectionChoice(SearchResult res)
	{
		ShellSelection ret;

		Elements.Prm_Selection.Validator = str =>
		{
			ret = Parse(str, res);

			if (ret == null) {
				return ValidationResult.Error();
			}

			return ValidationResult.Success();
		};


		var val = AnsiConsole.Prompt(Elements.Prm_Selection);
		var sri = Parse(val, res);
		return sri;
	}

}