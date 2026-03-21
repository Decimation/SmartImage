// Author: Deci | Project: SmartImage.Rdx | Name: Renderables.cs
// Date: 2025/12/27 @ 22:12:49

global using SizeIS = SixLabors.ImageSharp.Size;
using System.Data;
using Novus.Runtime;
using SmartImage.Lib.Images.Uni;
using SmartImage.Lib.Model;
#nullable disable
using System.Reflection;
using Kantan.Text;
using Novus.OS;
using Novus.Utilities;
using SmartImage;
using SmartImage.Lib;
using SmartImage.Lib.Engines.Results;
using SmartImage.Lib.Utilities.Integration;
using Spectre.Console;
using Spectre.Console.Rendering;
using Kantan.Console;
using static Kantan.Console.RenderableUtility;

namespace SmartImage.Rdx.Shell;

internal static class Renderables
{

	extension(SearchResult result)
	{

		public IEnumerable<IRenderable> GetMainRows()
		{
			Style style = result.Engine.Option.GetColor();

			return [new Text($"{result.Engine.Name}", style), new Text($"{result.Results.Count}")];
		}

		public IEnumerable<IRenderable[]> GetFullResultRows()
		{
			Style style = result.Engine.Option.GetColor();

			for (int i = 0; i < result.Results.Count; i++) {
				var res = result.Results[i];

				yield return res.GetResultRow(i, style);
			}

		}

	}

	extension(IResultItem sri)
	{

		public IRenderable GetResolution()
			=> sri.HasDimensions ? GetResolution(new SizeIS(sri.Width.Value, sri.Height.Value)) : ElementUtility.Txt_NA;

		public IRenderable GetSimilarity() => AsRenderable(sri.Similarity);

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
				url       = ElementUtility.Txt_NA;
				linkStyle = style;
			}


			var name   = new Text($"#{i}" + (sri.IsRaw ? " (Raw)" : null), style);
			var sim    = sri.GetSimilarity();
			var artist = AsRenderable(sri.Artist);
			var wh     = sri.GetResolution();

			return [name, url, sim, artist, wh];
		}

		public Grid GetItemInfoGrid()
		{
			// TODO

			IRenderable url;
			var         link = sri.Url;
			Style       linkStyle;

			if (link != null) {
				linkStyle = new Style(link: link);
				url       = new Markup(Markup.Escape(link.ToString()), linkStyle);
			}
			else {
				url = ElementUtility.Txt_NA;
			}

			var gr = new Grid();
			gr.AddColumns(2);

			var name   = new Text($"{sri.Root.Engine.Name}");
			var sim    = sri.GetSimilarity();
			var artist = AsRenderable(sri.Artist);
			var wh     = sri.GetResolution();

			var elems = new List<IRenderable>() { name, url, sim, artist, wh };

			// var elems2 = [sri.Character, sri.Source, sri.Description, sri.Site];
			var elemNames = new string[]
			{
				nameof(sri.Character),
				nameof(sri.Source),
				nameof(sri.Description),
				nameof(sri.Site),
				nameof(sri.Title)
			};

			/*var fields = sri.GetType().GetProperties(BindingFlags.GetProperty | BindingFlags.Instance | BindingFlags.Public)
			.Where(x => x.GetValue(sri) != null);*/

			foreach (string elemName in elemNames) {
				var prop = sri.GetType().GetProperty(elemName, BindingFlags.GetProperty | BindingFlags.Instance | BindingFlags.Public);

				if (prop is { }) {
					var val = prop.GetValue(sri);
					var s   = val?.ToString();

					if (!String.IsNullOrWhiteSpace(s)) {
						elems.Add(new Text(s));

					}
				}
			}

			if (elems.Count % 2 != 0) {
				elems.Add(ElementUtility.Txt_NA);
			}

			for (int i = 0; i < elems.Count - 1; i += 2) {
				gr.AddRow(elems[i], elems[i + 1]);
			}

			return gr;
		}

		public IRenderable[] GetItemRow(int idx, int subIdx)
		{
			var result = sri.Root;
			var style  = new Style(link: sri.Url, foreground: result.Engine.Option.GetColor());

			return
			[
				new Text($"#{idx}.{subIdx}", style),
				new Text(Markup.Escape(sri.Url), new Style(link: sri.Url)),
				sri.GetSimilarity(),
				ElementUtility.Txt_NA,
				sri.GetResolution()
			];
		}

		public Grid CreateExtendedGrid()
		{
			// TODO: WIP
			var grid = new Grid();
			grid.AddColumns(2);

			var inteer    = new[] { typeof(IResultItem), typeof(ISimilarity), typeof(IHashable), typeof(IResultMetadata), typeof(IUniImage) };
			var interProp = inteer.Select(x => x.GetProperties()).Distinct();

			var properties = sri.GetType()
			                    .GetProperties(BindingFlags.Public | BindingFlags.Instance)
			                    .Where(p => !p.IsCalculated())
			                    .Where(x => x.Name is not (nameof(SearchResultItem.ScannedItems) or nameof(SearchResultItem.IsRaw)
				                                or nameof(ScannedResultItem.Width) or nameof(ScannedResultItem.Height)
				                                or nameof(SearchResultItem.Metadata) or nameof(SearchResultItem.Parent)));

			foreach (var property in properties) {
				var propVal = property.GetValue(sri);

				if (propVal is string s && String.IsNullOrWhiteSpace(s) || (RuntimeProperties.IsNullable(propVal) && propVal == null)) {
					continue;
				}

				var render = AsRenderable(propVal);
				grid.AddRow(new Text($"{property.Name}", Elements.Sty_ResultHeader), render);
			}

			grid.AddRow(new Text("Resolution", Elements.Sty_ResultHeader), sri.GetResolution());

			return grid;
		}

	}

#region

	public static IRenderable GetResolution(SizeIS sz) => new Text($"{sz.Width}{Strings.Constants.MUL_SIGN}{sz.Height}");

#endregion

#region

	public static SpcTable CreateOverviewTable()
	{
		var col = new TableColumn[]
		{
			new(new Text("Engine", Elements.Sty_ResultHeader)),
			new(new Text("Results", Elements.Sty_ResultHeader)),
		};

		SpcTable tb = CreateEmptyResultTable();

		tb.AddColumns(col);
		tb = tb.Centered();

		return tb;
	}

	private static SpcTable CreateEmptyResultTable()
	{
		var tb = new SpcTable()
		{
			// Caption     = new TableTitle("Results", Elements.Sty_ResultHeader),
			Border      = TableBorder.Simple,
			ShowHeaders = true,
		};
		return tb;
	}

	public static SpcTable CreateResultTable()
	{
		var col = new TableColumn[]
		{
			new(new Text("Result", Elements.Sty_ResultHeader)),
			new(new Text("URL", Elements.Sty_ResultHeader)),
			new(new Text("Similarity", Elements.Sty_ResultHeader)),
			new(new Text("Artist", Elements.Sty_ResultHeader)),
			new(new Text("Resolution", Elements.Sty_ResultHeader)),

		};

		var tb = CreateEmptyResultTable();

		tb.AddColumns(col);

		return tb;
	}


	internal static Grid CreateConfigGrid(SearchConfig cfg, SearchQuery query)
	{
		var dt = new Grid();
		dt.AddColumns(2);

		dt.AddRow(new Text("Query", Elements.Sty_Grid1), new Text(query.Source.Value, new Style(link: query.Source.Value)));
		dt.AddRow(new Text("Query Format", Elements.Sty_Grid1), new Text($"({query.Source.Type}) {query.Source.ImageFormat.Name}"));
		dt.AddRow(new Text("Upload", Elements.Sty_Grid1), new Text($"{query.Upload}", new Style(link: query.Upload.Url)));

		var kv = new Dictionary<object, object>
		{
			[R1.S_SearchEngines]   = cfg.SearchEngines,
			[R1.S_PriorityEngines] = cfg.PriorityEngines,
			[R1.S_UploadEngine]    = cfg.UploadEngine,
			[R1.S_AutoSearch]      = cfg.AutoSearch,
			[R1.S_ReadCookies]     = cfg.ReadCookies,
			["FlareSolverr"]       = cfg.FlareSolverr,
		};

		foreach (var (s, o) in kv) {
			IRenderable kR;

			if (s is Text txt) {
				kR = txt;
			}
			else {
				kR = new Text(s?.ToString(), Elements.Sty_Grid1);
			}

			dt.AddRow(kR, AsRenderable(o));
		}


		// Render the layout
		// AnsiConsole.Write(layout);


		return dt;
	}

	internal static Grid CreateEnvironmentGrid()
	{
		var gr = new Grid();
		gr.AddColumns(2);

		var rows = new IRenderable[]
		{
			new Text("User", Elements.Sty_Grid1), new Text($"{Environment.UserName} / {FileSystem.IsRoot}"),
			new Text("Version", Elements.Sty_Grid1), new Text($"{Program.Version}"),
			new Text("Runtime", Elements.Sty_Grid1), new Text($"{Environment.OSVersion} / {Environment.Version}"),
			new Text("Location", Elements.Sty_Grid1), new TextPath(BaseOSIntegration.Executable)
		};

		gr.AddRowsByChunk(2, rows);

		return gr;
	}

#endregion

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