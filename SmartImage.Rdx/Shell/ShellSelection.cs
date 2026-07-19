// Author: Deci | Project: SmartImage.Rdx | Name: ShellSelection.cs
// Date: 2026/01/10 @ 00:01:54

#nullable disable
using SmartImage;
using SmartImage.Lib.Engines.Results;
using SmartImage.Lib.Utilities;
using SmartImage.Rdx.Shell;
using Spectre.Console;

namespace SmartImage.Rdx.Shell;

// todo: deprecate
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

	public int Index()
	{
		var scnIdx = 0;
		int root   = 0;
		int t      = 0;

		if (Item is IChildResultItem { IsChild: true } sub) {
			// scnIdx = Item.Parent.ScannedItems.IndexOf(Item);
			root = sub.Parent.Root.Results.IndexOf(sub.Parent);

			scnIdx++;

		}
		else {
			root = Item.Root.Results.IndexOf(Item);

		}

		for (int k = 0; k < root; k++) {

			var kItem = Item is IChildResultItem {} subItem ? subItem.Parent : Item;

			var result  = kItem.Root.Results[k] as IScannableItem;

			var scnItm  = result.ScannedItems;
			var scnIdx2 = scnItm.FindIndex(p=>p.Parent == kItem);

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


	public int Index3()
	{
		var t = 0;
		
		var itemIdx = Item.Root.Results.IndexOf(Item);

		for (int i = 0; i < Item.Root.Results.Count; i++) {
			var itemResult = Item.Root.Results[i];

			t += i;

			if (itemResult is IChildResultItem { } sub && Item is IScannableItem {} scannableItem) {
				var subIdx = scannableItem.ScannedItems.FindIndex(p=>p.Parent==sub);
				t += subIdx;
			}
		}

		return t;
	}

	[CBN]
	private static ShellSelection Parse(string str, SearchResult sr)
	{
		var spl = str.Split('.');

		IResultItem sri = null;
		ScannedResultItem sri2 = null;

		var  resIdx    = 0;
		var  scnIdx    = -1;
		bool isScanned = false;

		if (sr.Results.TryParseIndex(spl[0], out resIdx, out sri)) {
			if (spl.Length == 2) {
				if (sri is SearchResultItem { } sriOrig && sriOrig.ScannedItems.TryParseIndex(spl[1], out scnIdx, out sri2)) {
					sri       = sri2.Parent;
					isScanned = true;
				}
			}
		}
		else {
			return null;
		}

		return new ShellSelection(sri, resIdx, scnIdx, isScanned);
	}

	public static ShellSelection GetSelectionChoice(SearchResult res)
	{
		ShellSelection ret;

		Renderables.Prm_Selection.Validator = str =>
		{
			ret = Parse(str, res);

			if (ret == null) {
				return ValidationResult.Error();
			}

			return ValidationResult.Success();
		};


		var val = AnsiConsole.Prompt(Renderables.Prm_Selection);
		var sri = Parse(val, res);
		return sri;
	}

}