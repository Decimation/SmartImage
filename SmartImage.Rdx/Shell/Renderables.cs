// Author: Deci | Project: SmartImage.Rdx | Name: Renderables.cs
// Date: 2025/12/27 @ 22:12:49

global using SizeIS = SixLabors.ImageSharp.Size;
using System.Data;
using System.Runtime.InteropServices;
using Flurl;
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
using RU = Kantan.Console.RenderableUtility;

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

		public IEnumerable<IList<IRenderable>> GetFullRows()
			=> result.Results.Select(static res => res.GetMainRows(true));

	}


	extension(IResultItem sri)
	{

		public IRenderable GetResolution()
			=> sri.HasDimensions ? GetResolution(new SizeIS(sri.Width.Value, sri.Height.Value)) : ElementUtility.Txt_NA;

		public IRenderable GetSimilarity() => RU.AsRenderable(sri.Similarity);

		public IRenderable GetUrl()
		{
			if (Url.IsValid(sri.Url)) {
				return new Text(Markup.Escape(sri.Url), new Style(link: sri.Url));
			}

			return ElementUtility.Txt_NA;
		}

		public Grid GetItemInfoGrid()
		{
			// TODO

			var gr = new Grid();
			gr.AddColumns(2);

			var elems = sri.GetMainRows(false).ToList();

			var elemNames = new[]
			{
				nameof(sri.Character),
				nameof(sri.Source),
				nameof(sri.Description),
				nameof(sri.Site),
				nameof(sri.Title)
			};

			/*var fields = sri.GetType().GetProperties(BindingFlags.GetProperty | BindingFlags.Instance | BindingFlags.Public)
			.Where(x => x.GetValue(sri) != null);*/

			elems.AddRange(sri.GetElementProperties(elemNames));

			if (elems.Count % 2 != 0) {
				elems.Add(ElementUtility.Txt_NA);
			}

			for (int i = 0; i < elems.Count - 1; i += 2) {
				gr.AddRow(elems[i], elems[i + 1]);
			}

			return gr;
		}

		public IList<IRenderable> GetMainRows(bool full)
		{
			IRenderable name;

			if (full) {
				var i     = sri.Root.Results.IndexOf(sri);
				var style = sri.Root.Engine.Option.GetColor();

				name = new Text($"#{i}" + (sri.IsRaw ? " (Raw)" : null), style);
			}
			else {
				name = new Text($"{sri.Root.Engine.Name}");
			}


			var sim    = sri.GetSimilarity();
			var artist = RU.AsRenderable(sri.Artist);
			var wh     = sri.GetResolution();
			var url    = sri.GetUrl();

			return [name, url, sim, artist, wh];
		}


		public IList<IRenderable> GetItemRow(int idx, int subIdx)
		{
			var result = sri.Root;
			var style  = new Style(link: sri.Url, foreground: result.Engine.Option.GetColor());
			var name   = new Text($"#{idx}.{subIdx}", style);

			var rows = sri.GetMainRows(false);
			rows[0] = name;
			return rows;
		}

		public Grid CreateExtendedGrid()
		{
			// TODO: WIP
			var grid = new Grid();
			grid.AddColumns(2);

			var inteer    = new[] { typeof(IResultItem), typeof(ISimilarity), typeof(IHashable), typeof(IResultMetadata), typeof(IUniImage) };
			var interProp = inteer.Select(x => x.GetProperties()).Distinct();

			var properties = sri.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
			                    .Where(static p => !p.IsCalculated())
			                    .Where(static x =>
			                    {
				                    return x.Name is not (nameof(SearchResultItem.ScannedItems) or nameof(SearchResultItem.IsRaw)
					                           or nameof(ScannedResultItem.Width) or nameof(ScannedResultItem.Height)
					                           or nameof(SearchResultItem.Metadata) or nameof(SearchResultItem.Parent));
			                    });

			foreach (var property in properties) {
				var propVal = property.GetValue(sri);

				if (propVal is string s && String.IsNullOrWhiteSpace(s) || (ObjectUtility.IsNullable(propVal) && propVal == null)) {
					continue;
				}

				var render = RU.AsRenderable(propVal);
				grid.AddRow(new Text($"{property.Name}", Elements.Sty_ResultHeader), render);
			}

			grid.AddRow(new Text("Resolution", Elements.Sty_ResultHeader), sri.GetResolution());

			return grid;
		}

		public IEnumerable<IRenderable> GetElementProperties(IEnumerable<string> propNames)
		{
			const BindingFlags OPTIONS = BindingFlags.GetProperty | BindingFlags.Instance | BindingFlags.Public;

			foreach (string name in propNames) {
				var prop = sri.GetType().GetProperty(name, OPTIONS);

				if (prop is { }) {
					var val = prop.GetValue(sri);

					yield return RU.AsRenderable(val);
				}
			}
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

		dt.AddRow(new Text("Query", Elements.Sty_Grid1), new Text(Markup.Escape(query.Source.Value),new Style(link: query.Source.Value)));
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

			dt.AddRow(kR, RU.AsRenderable(o));
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