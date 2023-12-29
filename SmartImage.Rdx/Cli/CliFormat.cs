global using STable = Spectre.Console.Table;
global using DTable = System.Data.DataTable;
using System.Data;
using Spectre.Console;
using Spectre.Console.Rendering;

// $User.Name $File.ProjectName $File.FileName
// $File.CreatedYear-$File.CreatedMonth-$File.CreatedDay @ $File.CreatedHour:$File.CreatedMinute

namespace SmartImage.Rdx.Cli;

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

	public static STable DTableToSTable(DTable dt)
	{
		var t = new STable();

		foreach (DataColumn row in dt.Columns) {
			t.AddColumn(new TableColumn(row.ColumnName));
		}

		foreach (DataRow row in dt.Rows) {
			var obj = row.ItemArray
				.Select<object, IRenderable>(x =>
				{
					if (x is IRenderable r) {
						return r;
					}

					if (x == null) {
						return EmptyText;
					}

					return new Text(x.ToString());
				}).ToArray();

			t.AddRow(obj);
		}

		return t;
	}

}