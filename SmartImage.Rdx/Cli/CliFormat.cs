global using STable = Spectre.Console.Table;
global using DTable = System.Data.DataTable;
using System.Data;
using Flurl;
using Kantan.Utilities;
using SmartImage.Lib.Engines;
using SmartImage.Lib.Results;
using Spectre.Console;
using Spectre.Console.Rendering;

// $User.Name $File.ProjectName $File.FileName
// $File.CreatedYear-$File.CreatedMonth-$File.CreatedDay @ $File.CreatedHour:$File.CreatedMinute

namespace SmartImage.Rdx.Cli;

[Flags]
internal enum ResultTableFormat
{

	None = 0,

	Name       = 1 << 0,
	Similarity = 1 << 1,
	Url        = 1 << 2,

	Default = Name | Similarity | Url

}

internal enum ResultFileFormat
{

	None = 0,
	Csv,

}

internal static partial class CliFormat
{
	[MURV]
	public static FigletFont LoadFigletFontFromResource(string name, out MemoryStream fs)
	{
		var o = R2.ResourceManager.GetObject(name);

		if (o == null) {
			throw new InvalidOperationException(nameof(name));
		}

		fs = new MemoryStream((byte[]) o);
		var ff = FigletFont.Load(fs);

		return ff;
	}

	public static Table GetTableForFormat(ResultTableFormat format)
	{

		var fmt   = format.GetSetFlags(true, true);
		var table = new Table();
		var col   = new TableColumn[fmt.Count];

		for (int i = 0; i < col.Length; i++) {
			col[i] = new TableColumn(new Text($"{fmt[i]}",
			                                  new Style(decoration: Decoration.Bold | Decoration.Underline)));
		}

		table.AddColumns(col);

		return table;
	}

	public static IRenderable[] GetRowsForFormat(SearchResultItem s, int i, ResultTableFormat format)
	{
		var ls = new List<IRenderable>();

		Url?   url  = s.Url;
		string host = url?.Host ?? STR_DEFAULT;

		Color c = GetEngineColor(s.Root.Engine.EngineOption);

		if (format.HasFlag(ResultTableFormat.Name)) {
			ls.Add(new Text($"{s.Root.Engine.Name} #{i + 1}", s_styleName.Foreground(c)));
		}

		if (format.HasFlag(ResultTableFormat.Similarity)) {
			ls.Add(new Text($"{s.Similarity / 100f:P}", s_styleSim));
		}

		if (format.HasFlag(ResultTableFormat.Url)) {
			ls.Add(new Text(host, s_styleUrl.Link(url)));
		}

		return ls.ToArray();
	}

	public static IRenderable[] GetRowsForFormat(SearchResult s, ResultTableFormat format)
	{
		var ls = new List<IRenderable>();

		Url?   url  = s.RawUrl;
		string host = url?.Host ?? STR_DEFAULT;

		Color c = GetEngineColor(s.Engine.EngineOption);

		if (format.HasFlag(ResultTableFormat.Name)) {
			ls.Add(new Text($"{s.Engine.Name}", s_styleName.Foreground(c)));
		}

		if (format.HasFlag(ResultTableFormat.Similarity)) {
			ls.Add(new Text(STR_DEFAULT, s_styleSim));
		}

		if (format.HasFlag(ResultTableFormat.Url)) {
			ls.Add(new Text(host, s_styleUrl.Link(url)));
		}

		return ls.ToArray();
	}

	private static Color GetEngineColor(SearchEngineOptions s)
	{
		if (!CliFormat.EngineColors.TryGetValue(s, out var c)) {
			c = Color.NavajoWhite1;
		}

		return c;
	}

	public static IEnumerable<TableRow> GetRows(Table t, Func<TableRow> f)
	{
		var trw = new TableRow[t.Columns.Count];

		for (int j = 0; j < trw.Length; j++) {
			var r = f();
			trw[j] = r;
		}

		return trw;
	}

	public static STable DTableToSTable(DTable dt)
	{
		var t = new STable();

		foreach (DataColumn row in dt.Columns) {
			t.AddColumn(new TableColumn(row.ColumnName));
		}

		foreach (DataRow row in dt.Rows) {
			var obj = row.ItemArray
				.Select(x =>
				{
					if (x is IRenderable r) {
						return r;
					}

					if (x == null) {
						return EmptyText;
					}

					return new Text(x.ToString());
				});

			t.AddRow(obj);
		}

		return t;
	}

}