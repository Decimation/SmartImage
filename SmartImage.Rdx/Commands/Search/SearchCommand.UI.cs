#nullable disable
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

#endregion

}