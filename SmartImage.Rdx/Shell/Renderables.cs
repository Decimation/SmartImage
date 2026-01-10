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

		public IEnumerable<IRenderable> GetMainRows()
		{
			Style style = result.Engine.EngineOption.GetColor();

			return [new Text($"{result.Engine.Name}", style), new Text($"{result.Results.Count}")];
		}

		public IEnumerable<IRenderable[]> GetFullResultRows()
		{
			Style style = result.Engine.EngineOption.GetColor();

			for (int i = 0; i < result.Results.Count; i++) {
				var res = result.Results[i];

				yield return res.GetFullResultRow(i, style);
			}

		}

	}

	extension(SearchResultItem sri)
	{

		public IRenderable GetResolution() => (sri.HasDimensions) ? new Text($"{sri.Width}x{sri.Height}") : Elements.Txt_NA;

		public IRenderable GetSimilarity() => AsRenderable(sri.Similarity);

		public IRenderable[] GetFullResultRow(int i, Style style)
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
			var artist = AsRenderable(sri.Artist);
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

	public static IRenderable AsRenderable<T>(T? val) where T : struct
	{
		return val.HasValue ? AsRenderable(val.Value) : Elements.Txt_NA;
	}

	public static IRenderable AsRenderable<T>(T val)
	{
		IRenderable renderable = val switch
		{
			IRenderable r => r,

			string sz when String.IsNullOrWhiteSpace(sz) => Elements.Txt_NA,

			string sz => new Text(Markup.Escape(sz)),

			bool b => b.ToPrettyText(),

			null => Elements.Txt_NA,

			_ => new Text(val?.ToString())
		};
		return renderable;
	}

	public static Text ToPrettyText(this bool b) => b ? Elements.Txt_Rad : Elements.Txt_Mul;

}

/// <summary>
/// <see cref="Renderables.GetFullResultRow"/>
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