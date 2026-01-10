global using SpcTable = Spectre.Console.Table;
global using DTable = System.Data.DataTable;
using System.Data;
using System.Resources;
using Kantan.Utilities;
using Spectre.Console;
using Spectre.Console.Cli;
using Spectre.Console.Rendering;

// $User.Name $File.ProjectName $File.FileName
// $File.CreatedYear-$File.CreatedMonth-$File.CreatedDay @ $File.CreatedHour:$File.CreatedMinute
#nullable disable
namespace SmartImage.Rdx.Shell;

internal static class ConsoleUtil
{

	private static readonly byte[] s_utf8BomSig =
	[
		0xEF, 0xBB, 0xBF
	];

	public static string ParseInputStream(int bufSize = 4096, int maxSize = 10_000_000)
	{
		string path = null;

		using Stream stdin = Console.OpenStandardInput();

		var buffer  = new byte[bufSize];
		var buffer2 = new byte[maxSize];
		int bytesRead;
		int iter  = 0;
		int b2pos = 0;

		while ((bytesRead = stdin.Read(buffer, 0, buffer.Length)) > 0) {
			if (iter == 0) {

				if (buffer[0]    == s_utf8BomSig[0]
				    && buffer[1] == s_utf8BomSig[1]
				    && buffer[2] == s_utf8BomSig[2]) {

					buffer    =  buffer[3..];
					bytesRead -= s_utf8BomSig.Length;
				}
			}

			Array.Copy(buffer, 0, buffer2, b2pos, bytesRead);
			b2pos += bytesRead;

			iter++;

			// prog?.Report(b2pos);
		}

		if (buffer2[(b2pos - 1)] == '\n' && buffer2[(b2pos - 2)] == '\r') {
			b2pos -= 2;
		}

		Array.Resize(ref buffer2, b2pos);

		var s = Console.InputEncoding.GetString(buffer2);

		if (File.Exists(s)) {
			path = s;
		}
		else {
			path = Path.GetTempFileName();
			File.WriteAllBytes(path, buffer2);
		}

		return path;
	}

	internal static void Dump(CommandSettings settings)
	{
		var table = new SpcTable().RoundedBorder();
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

	[MURV]
	public static FigletFont LoadFigletFontFromResource(string name, out MemoryStream fs)
		=> R2.ResourceManager.LoadFigletFontFromResource(name, out fs);

	[MURV]
	public static FigletFont LoadFigletFontFromResource(this ResourceManager rsrc, string name, out MemoryStream fs)
	{
		var o = rsrc.GetObject(name);

		if (o == null) {
			throw new InvalidOperationException(nameof(name));
		}

		fs = new MemoryStream((byte[]) o);
		var ff = FigletFont.Load(fs);

		return ff;
	}

	public static SpcTable DTableToSTable(this DTable dt)
	{
		var t = new SpcTable();

		foreach (DataColumn row in dt.Columns) {
			t.AddColumn(new TableColumn(row.ColumnName));
		}

		Func<object, IRenderable> selector = Elements.AsRenderable;

		foreach (DataRow row in dt.Rows) {
			var obj = row.ItemArray
				.Select(selector);

			t.AddRow(obj);
		}

		return t;
	}

}