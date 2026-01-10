// Author: Deci | Project: SmartImage.Rdx | Name: Renderables.cs
// Date: 2025/12/27 @ 22:12:49

#nullable disable
using SmartImage;
using SmartImage.Lib.Engines.Results;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace SmartImage.Rdx.Shell;

internal static class Renderables
{

	extension(SearchResult result)
	{

		public IEnumerable<IRenderable> CreateMainRows()
		{
			Style style = result.Engine.EngineOption.GetColor();

			return [new Text($"{result.Engine.Name}", style), new Text($"{result.Results.Count}")];
		}

		public IEnumerable<IRenderable[]> CreateFullResultRows()
		{
			Style style = result.Engine.EngineOption.GetColor();

			for (int i = 0; i < result.Results.Count; i++) {
				var res = result.Results[i];

				yield return res.GetResultRow(i, style);
			}

		}

	}

	extension(SearchResultItem sri)
	{

		public IRenderable GetResolution() => (sri.HasDimensions) ? new Text($"{sri.Width}x{sri.Height}") : Elements.Txt_NA;

		public IRenderable GetSimilarity() => sri.Similarity.HasValue ? new Text($"{sri.Similarity}") : Elements.Txt_NA;

		public IRenderable[] GetResultRow(int i, Style style)
		{
			IRenderable url;
			var         link = sri.Url;
			Style       linkStyle;

			if (link != null) {
				linkStyle = new Style(link: link);
				url       = new Markup(Markup.Escape(link.ToString()), linkStyle);
			}
			else {
				url       = Elements.Txt_NA;
				linkStyle = style;
			}


			var name   = new Text($"#{i}" + (sri.IsRaw ? " (Raw)" : null), style);
			var sim    = sri.GetSimilarity();
			var artist = Elements.AsRenderable(sri.Artist);
			var wh     = sri.GetResolution();

			return [name, url, sim, artist, wh];
		}

		public IRenderable[] GetItemRow(int idx, int subIdx)
		{
			// var url = ui is UniImageUrl uiu ? uiu.Url.ToString() : String.Empty;

			var result = sri.Root;

			var style = new Style(link: sri.Url, foreground: result.Engine.EngineOption.GetColor());

			return
			[
				new Text($"#{idx}.{subIdx}", style),
				new Text(Markup.Escape(sri.Url)),
				sri.GetSimilarity(),
				Elements.Txt_NA,
				sri.GetResolution()
			];
		}

	}

}

/// <summary>
/// <see cref="Renderables.GetResultRow"/>
/// <see cref="Renderables.GetItemRow"/>
/// </summary>
internal enum ResultRowIndex
{

	ROW_NUM        = 0,
	ROW_URL        = 1,
	ROW_SIMILARITY = 2,
	ROW_ARTIST     = 3,
	ROW_WH         = 4

}