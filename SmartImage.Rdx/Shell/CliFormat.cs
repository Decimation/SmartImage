global using STable = Spectre.Console.Table;
global using DTable = System.Data.DataTable;
using System.Data;
using Kantan.Utilities;
using Spectre.Console;
using Spectre.Console.Cli;
using Spectre.Console.Rendering;

// $User.Name $File.ProjectName $File.FileName
// $File.CreatedYear-$File.CreatedMonth-$File.CreatedDay @ $File.CreatedHour:$File.CreatedMinute

namespace SmartImage.Rdx.Shell;

[Flags]
public enum OutputFields
{

	None = 0,

	Name       = 1 << 0,
	Url        = 1 << 1,
	Similarity = 1 << 2,
	Artist     = 1 << 3,
	Site       = 1 << 4,

	// Default = Name | Url | Similarity

}

public enum OutputFileFormat
{

	None = 0,
	Delimited,

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

	public static void Dump(CommandSettings settings)
	{
		var table = new STable().RoundedBorder();
		table.AddColumn("[grey]Name[/]");
		table.AddColumn("[grey]Value[/]");

		var properties = settings.GetType().GetProperties();

		foreach (var property in properties) {
			var value = property.GetValue(settings)
				?.ToString()
				?.Replace("[", "[[");

			table.AddRow(
				property.Name,
				value ?? "[grey]null[/]");
		}

		AnsiConsole.Write(table);
	}

	public static STable GetTableForFormat(OutputFields format)
	{

		var fmt   = format.GetSetFlags(true, true);
		var table = new STable();
		var col   = new TableColumn[fmt.Count];

		for (int i = 0; i < col.Length; i++) {
			col[i] = new TableColumn(new Text($"{fmt[i]}",
			                                  new Style(decoration: Decoration.Bold | Decoration.Underline)));
		}

		table.AddColumns(col);

		return table;
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
						return Txt_Empty;
					}

					return new Text(x.ToString());
				});

			t.AddRow(obj);
		}

		return t;
	}

	internal static string? GetOS()
	{
		string? os = null;

		if (IsLinux) {
			os = "Linux";

		}
		else if (IsWindows) {
			os = "Windows";
		}
		else if (IsMacOs) {
			os = "Mac";
		}

		return os;
	}

}