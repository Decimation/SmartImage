// Deci SmartImage.Rdx ResultModel.cs
// $File.CreatedYear-$File.CreatedMonth-26 @ 0:50

using Flurl;
using SmartImage.Lib.Results;
using SmartImage.Rdx.Shell;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace SmartImage.Rdx;

public class ResultModel : IDisposable
{

	public SearchResult Result { get; }

	// public STable Table { get; }

	public int Id { get; }

	public Grid Grid { get; private set; }

	public ResultModel(SearchResult result)
		: this(result, Interlocked.Increment(ref Count)) { }

	public ResultModel(SearchResult result, int id)
	{
		Result = result;
		Id     = id;
		Grid   = CreateGrid();

		// Table  = Create();
	}

	private protected static int Count = 0;

	public void Dispose()
	{
		Result.Dispose();
	}

	private static Grid CreateGrid()
	{
		var gr = new Grid();
		gr.AddColumns(2);
		gr.AddRow("Name", "Details");
		return gr;
	}

	/*public STable Create()
	{
		var table = CreateTable();

		int i = 0;

		foreach (SearchResultItem sri in Result.Results) {
			table.Rows.Add([
				new Text($"{i                                                        + 1}"),
				Markup.FromInterpolated($"[link={sri.Url}]{sri.Root.Engine.Name} #{i + 1}[/]"),
				Markup.FromInterpolated($"{sri.Similarity}"),
				Markup.FromInterpolated($"{sri.Artist}"),
				Markup.FromInterpolated($"{sri.Description}"),
				Markup.FromInterpolated($"{sri.Character}")
			]);

			i++;
		}

		return table;
	}

	private static STable CreateTable()
	{
		var table = new STable()
		{
			Border      = TableBorder.Heavy,
			Title       = new($"Results"),
			ShowFooters = false,
			ShowHeaders = false,
			Expand      = false,
		};

		table.AddColumns(new TableColumn("#"),
		                 new TableColumn("Name"),
		                 new TableColumn("Similarity"),
		                 new TableColumn("Artist"),
		                 new TableColumn("Description"),
		                 new TableColumn("Character")
		);

		return table;
	}*/

	internal Grid UpdateGrid(int ix = -1, bool clear = false)
	{
		if (clear) {
			Grid = CreateGrid();
		}

		var allRes = Result.GetAllResults();
		int i      = 0;

		var gr2 = new Grid();

		foreach (var item in allRes) {
			Style? style = null;
			if (ix == i++) {
				style = new Style(Color.Yellow);
				gr2.AddColumns(new[] { new GridColumn(), new GridColumn() });
				gr2.AddRow(nameof(SearchResultItem.Url), $"{item.Url}");

			}

			Grid.AddRow(new Text($"{Result.Engine.Name} #{i}", style), new Text($"{item.Score}"));
		}

		return gr2;
	}

	internal IRenderable[][] GetRowsForFormat2(OutputFields format)
	{
		var allRes = Result.GetAllResults();
		var ls     = new List<IRenderable[]>();
		int j      = 0;

		foreach (SearchResultItem item in allRes) {
			var rg = GetRowsForFormat(item, j++, format);
			ls.Add(rg);
		}

		return ls.ToArray();
	}

	internal static IRenderable[] GetRowsForFormat(SearchResultItem s, int i, OutputFields format)
	{
		var ls = new List<IRenderable>();

		Url?   url  = s.Url;
		string host = url?.Host ?? CliFormat.STR_DEFAULT;

		Color c = CliFormat.GetEngineColor(s.Root.Engine.EngineOption);

		if (format.HasFlag(OutputFields.Name)) {
			ls.Add(new Text($"{s.Root.Engine.Name} #{i + 1}", CliFormat.Sty_Name.Foreground(c)));
		}

		if (format.HasFlag(OutputFields.Similarity)) {
			ls.Add(new Text($"{s.Similarity / 100f:P}", CliFormat.Sty_Sim));
		}

		if (format.HasFlag(OutputFields.Url)) {
			ls.Add(new Text(host, CliFormat.Sty_Url.Link(url)));
		}

		return ls.ToArray();
	}

	internal IRenderable[] GetRowsForFormat(OutputFields format)
	{
		var ls = new List<IRenderable>();

		Url?   url  = Result.RawUrl;
		string host = url?.Host ?? CliFormat.STR_DEFAULT;

		Color c = CliFormat.GetEngineColor(Result.Engine.EngineOption);

		if (format.HasFlag(OutputFields.Name)) {
			ls.Add(new Text($"{Result.Engine.Name}", CliFormat.Sty_Name.Foreground(c)));
		}

		if (format.HasFlag(OutputFields.Similarity)) {
			ls.Add(new Text(CliFormat.STR_DEFAULT, CliFormat.Sty_Sim));
		}

		if (format.HasFlag(OutputFields.Url)) {
			ls.Add(new Text(host, CliFormat.Sty_Url.Link(url)));
		}

		return ls.ToArray();
	}

}