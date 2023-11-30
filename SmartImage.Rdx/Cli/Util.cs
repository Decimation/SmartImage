// $User.Name $File.ProjectName $File.FileName
// $File.CreatedYear-$File.CreatedMonth-$File.CreatedDay @ $File.CreatedHour:$File.CreatedMinute

using System.Data;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace SmartImage.Rdx.Cli;

public static class Util
{

	public static Table FromDataTable(DataTable dt)
	{
		var t = new Table();

		foreach (DataColumn row in dt.Columns) {
			t.AddColumn(new TableColumn(row.ColumnName));
		}

		foreach (DataRow row in dt.Rows) {
			var obj = row.ItemArray.Select(x =>
			{
				if (x is IRenderable r) {
					return r;
				}
				else {
					return (IRenderable) new Text(x.ToString());
				}
			}).Cast<IRenderable>().ToArray();

			t.AddRow(obj);
		}

		return t;
	}

}