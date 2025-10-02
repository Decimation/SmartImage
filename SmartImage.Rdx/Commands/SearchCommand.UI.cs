using SmartImage.Lib.Engines.Results;
using SmartImage.Lib.Images.Uni;
using SmartImage.Lib.Utilities;
using SmartImage.Rdx.Shell;
using Spectre.Console;
using Spectre.Console.Rendering;
using System;
using System.Collections.Generic;
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

	private int GetRowForItem(SearchResultItem sri)
	{
		int a = 0, b = 0, c = 0;

		// a = m_results[sri.Root];

		// b = sri.Root.Results.IndexOf(sri);
		b = sri.HasParent ? (sri.Parent.Root.HasResults ? sri.Parent.Root.Results.IndexOf(sri.Parent) : 0) : sri.Root.Results.IndexOf(sri);
		c = sri.HasParent ? (sri.Parent.HasScannedItems ? sri.Parent.ScannedItems.IndexOf(sri) : 0) : 0;

		if (sri.HasParent && !sri.HasScannedItems) {
			c++; // TODO NOTE: +1 for #.0 when #

		}

		return a + b + c;

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
		=> (sri.Width.HasValue && sri.Height.HasValue) ? new Text($"{sri.Width}x{sri.Height}") : ConsoleFormat.Txt_NA;

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


#region Prompts

	private static SearchResultItem GetResultItemPrompt(SearchResult res)
	{
		SearchResultItem ret;

		ConsoleFormat.Prm_Num2.Validator = str =>
		{
			ret = Parse(str);

			if (ret == null) {
				return ValidationResult.Error();
			}

			return ValidationResult.Success();
		};


		var val = AnsiConsole.Prompt(ConsoleFormat.Prm_Num2);
		return Parse(val);

		[CBN]
		SearchResultItem Parse(string str)
		{
			var spl = str.Split('.');

			SearchResultItem sri = null;

			if (res.Results.TryParseIndex(spl[0], out sri)) {

				if (spl.Length == 2) {

					if (sri.ScannedItems.TryParseIndex(spl[1], out sri)) { }
				}
				else { }

			}
			else { }

			return sri;
		}
	}

#endregion

}