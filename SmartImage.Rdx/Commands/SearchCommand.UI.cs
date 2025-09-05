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

namespace SmartImage.Rdx.Commands;

public sealed partial class SearchCommand
{

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

}