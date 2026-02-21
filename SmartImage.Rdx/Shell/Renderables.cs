// Author: Deci | Project: SmartImage.Rdx | Name: Renderables.cs
// Date: 2025/12/27 @ 22:12:49

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

	public static IRenderable GetResolution(SizeIS sz) => new Text($"{sz.Width}{Strings.Constants.MUL_SIGN}{sz.Height}");

	extension(SearchResultItem sri)
	{

		public IRenderable GetResolution() => sri.HasDimensions ? GetResolution(new SizeIS(sri.Width.Value, sri.Height.Value)) : Elements.Txt_NA;

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

		public Grid GetInfoGrid()
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
			}

			var gr = new Grid();
			gr.AddColumns(2);

			var name   = new Text($"{sri.Root.Engine.Name}");
			var sim    = sri.GetSimilarity();
			var artist = AsRenderable(sri.Artist);
			var wh     = sri.GetResolution();

			var elems = new List<IRenderable>() { name, url, sim, artist, wh };

			// var elems2 = [sri.Character, sri.Source, sri.Description, sri.Site];
			var elemnames = new String[] { nameof(sri.Character), nameof(sri.Source), nameof(sri.Description), nameof(sri.Site),nameof(sri.Title) };

			/*var fields = sri.GetType().GetProperties(BindingFlags.GetProperty | BindingFlags.Instance | BindingFlags.Public)
			.Where(x => x.GetValue(sri) != null);*/

			foreach (string elemname in elemnames) {
				var prop = sri.GetType().GetProperty(elemname, BindingFlags.GetProperty | BindingFlags.Instance | BindingFlags.Public);

				if (prop is {}) {
					var    val  = prop.GetValue(sri);
					var s = val?.ToString();
					if (!String.IsNullOrWhiteSpace(s)) {
						elems.Add(new Text(s));

					}
				}
			}

			if (elems.Count % 2 != 0) {
				elems.Add(Elements.Txt_NA);
			}

			for (int i = 0; i < elems.Count-1; i+=2) {
				gr.AddRow(elems[i], elems[i+1]);
			}
			
			return gr;
		}

		public IRenderable[] GetItemRow(int idx, int subIdx)
		{
			// var url = ui is UniImageUrl uiu ? uiu.Url.ToString() : String.Empty;

			var result = sri.Root;

			var style = new Style(link: sri.Url, foreground: result.Engine.EngineOption.GetColor());

			return
			[
				new Text($"#{idx}.{subIdx}", style),
				new Text(Markup.Escape(sri.Url), new Style(link: sri.Url)),
				sri.GetSimilarity(),
				Elements.Txt_NA,
				sri.GetResolution()
			];
		}

	}

#region

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

#endregion

#region

	public static SpcTable CreateMainTable()
	{
		var col = new TableColumn[]
		{
			new(new Text("Engine", Elements.Sty_ResultHeader)),
			new(new Text("Results", Elements.Sty_ResultHeader)),

		};

		SpcTable tb = CreateResultTable();

		tb.AddColumns(col);

		tb = tb.Centered();

		return tb;
	}

	private static SpcTable CreateResultTable()
	{
		var tb = new SpcTable()
		{
			// Caption     = new TableTitle("Results", Elements.Sty_ResultHeader),
			Border      = TableBorder.Simple,
			ShowHeaders = true,
		};
		return tb;
	}

	public static SpcTable CreateFullResultTable()
	{
		var col = new TableColumn[]
		{
			new(new Text("Result", Elements.Sty_ResultHeader)),
			new(new Text("URL", Elements.Sty_ResultHeader)),
			new(new Text("Similarity", Elements.Sty_ResultHeader)),
			new(new Text("Artist", Elements.Sty_ResultHeader)),
			new(new Text("Resolution", Elements.Sty_ResultHeader)),

		};

		var tb = CreateResultTable();

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
			[R1.S_UploadEngine] = cfg.UploadEngine,
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

	internal static Grid GetInfoGrid()
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

	internal static Grid AddRowsByChunk(this Grid g, int cnt, params IEnumerable<IRenderable> items)
	{
		var chunks = items.Chunk(cnt);

		foreach (IRenderable[] chunk in chunks) {
			g.AddRow(chunk);
		}

		return g;
	}

	internal static Grid MapToGrid<TKey, TValue>(IDictionary<TKey, TValue> dictionary,
	                                             [CBN] Func<TKey, IRenderable> keyFunc = null,
	                                             [CBN] Func<TValue, IRenderable> valFunc = null)
	{
		var grd = new Grid();
		grd.AddColumns(2);

		keyFunc ??= static k =>
		{
			//
			var s = k.ToString();
			ArgumentNullException.ThrowIfNull(s);
			return new Text(s, Elements.Sty_Grid1);
		};

		valFunc ??= AsRenderable;

		foreach (var (k, v) in dictionary) {
			grd.AddRow(keyFunc(k), valFunc(v));
		}

		return grd;
	}

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