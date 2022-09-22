using System.Reflection;
using Novus.Utilities;
using Spectre.Console;
using Terminal.Gui;

namespace SmartImage;

public static class TerminalHelper
{
	public static View[] GetViewFields(Type type)
	{
		return ReflectionHelper.GetFieldsById(type, new Assembly[] { Assembly.GetAssembly(typeof(Application)), })
		                       .Select(f => f.GetValue(null))
		                       .Cast<View>()
		                       .ToArray();
	}

	public static Table RemoveColumn(this Table t, int i)
	{
		var t2 = new Table()
		{
			Title         = t.Title,
			Alignment     = t.Alignment,
			Border        = t.Border,
			BorderStyle   = t.BorderStyle,
			Caption       = t.Caption,
			Expand        = t.Expand,
			ShowFooters   = t.ShowFooters,
			ShowHeaders   = t.ShowHeaders,
			UseSafeBorder = t.UseSafeBorder,
			Width         = t.Width,
		};

		for (int j = 0; j < t.Columns.Count; j++) {
			if (j == i) {
				continue;
			}

			t2.AddColumn(t.Columns[j]);
		}

		using var re = t.Rows.GetEnumerator();

		while (re.MoveNext()) {
			var r  = re.Current;
			var rr = r.ToList();
			rr.RemoveAt(i);

			// var re2=r.GetEnumerator();
			t2.AddRow(rr);
		}

		return t2;
	}
}