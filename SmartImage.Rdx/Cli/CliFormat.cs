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

public abstract class Formatter
{

	public abstract IRenderable Format(SearchResult s);

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

	internal static Color GetEngineColor(SearchEngineOptions s)
	{
		if (!EngineColors.TryGetValue(s, out var c)) {
			c = Color.NavajoWhite1;
		}

		return c;
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