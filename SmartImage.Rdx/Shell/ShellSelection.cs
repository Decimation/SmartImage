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

	public SearchResultItem Item { get; }

	public int ItemIdx { get; }

	public int ScanIdx { get; }

	public bool IsScannedItem { get; }

	private ShellSelection(SearchResultItem item, int itemIdx, int scanIdx, bool isScanned)
	{
		Item          = item;
		ItemIdx       = itemIdx;
		ScanIdx       = scanIdx;
		IsScannedItem = isScanned;
	}

	public int Index2()
	{
		int i1 = 0, j1 = 0;

		var rg = ItemIdx + Item.Root.Results[..ItemIdx].Sum(x => x.ScannedItems.Count);


		var sumIdx  = rg + ScanIdx + (IsScannedItem ? ((ScanIdx == 0) ? 1 : 0) : 0);
		var sumIdx2 = rg + ScanIdx + (IsScannedItem ? ((ScanIdx == 0) ? 1 : 0) : 1);

		return sumIdx2;

		// return rg + ScanIdx + (IsScannedItem?1:0);
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

	[CBN]
	private static ShellSelection Parse(string str, SearchResult sr)
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

		return new ShellSelection(sri, resIdx, scnIdx, isScanned);
	}

	public static ShellSelection GetSelectionChoice(SearchResult res)
	{
		ShellSelection ret;

		Elements.Prm_Selection.Validator = str =>
		{
			ret = ShellSelection.Parse(str, res);

			if (ret == null) {
				return ValidationResult.Error();
			}

			return ValidationResult.Success();
		};


		var val = AnsiConsole.Prompt(Elements.Prm_Selection);
		var sri = ShellSelection.Parse(val, res);
		return sri;


	}

	/*public static ShellSelection GetSelectionChoice2(SearchResult sr)
	{
		var prompt = new SelectionPrompt<SearchResultItem>()
		{
			Mode = SelectionMode.Independent,
			SearchEnabled = true,
			Converter = item =>
			{
				//
				return item.Url;
			}
		};

		foreach (var item in sr.Results) {

			if (item.HasScannedItems) {
				prompt.AddChoiceGroup(item, item.ScannedItems);

			}
			else {
				prompt.AddChoice(item);

			}
		}

		var resp    = AnsiConsole.Prompt(prompt);
		int itemIdx = 0, scanIdx = 0;

		if (resp.IsChild) {
			scanIdx = resp.Parent.ScannedItems.IndexOf(resp);
			itemIdx = sr.Results.IndexOf(resp.Parent);
		}
		else {
			itemIdx = sr.Results.IndexOf(resp);

		}


		return new ShellSelection(resp, itemIdx, scanIdx, resp.IsChild);
	}*/

}