using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Flurl;
using Kantan.Utilities;
using SmartImage.Lib.Results;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace SmartImage.Rdx.Cli;

[Flags]
internal enum ResultGridFormat
{

	None,
	Name       = 1 << 0,
	Similarity = 1 << 1,
	Url        = 1 << 2,

}

internal static partial class CliFormat
{

	internal static class Console
	{

		public static Grid GetGrid(ResultGridFormat format)
		{

			var fmt = format.GetSetFlags(true);

			var grid = new Grid();
			var col  = new GridColumn[fmt.Count];

			for (int i = 0; i < col.Length; i++) {
				col[i] = new GridColumn();
			}

			grid.AddColumns(col);

			var row1 = fmt.Select(x =>
			{
				return new Text($"{x}", new Style(decoration: Decoration.Bold | Decoration.Underline));
			});

			grid.AddRow(row1.Cast<IRenderable>().ToArray());

			return grid;
		}

		public static IRenderable[] GetRows(SearchResultItem s, int i, ResultGridFormat format)
		{
			var ls = new List<IRenderable>();

			Url?   url  = s.Url;
			string host = url?.Host ?? "-";

			if (!CliFormat.EngineColors.TryGetValue(s.Root.Engine.EngineOption, out var c)) {
				c = Color.NavajoWhite1;
			}

			if (format.HasFlag(ResultGridFormat.Name)) {
				ls.Add(new Text($"{s.Root.Engine.Name} #{i + 1}",
				                new Style(c, decoration: Decoration.Italic)));
			}

			if (format.HasFlag(ResultGridFormat.Similarity)) {
				ls.Add(new Text($"{s.Similarity / 100f:P}",
				                new Style(Color.Wheat1,
				                          decoration: Decoration.None)));
			}

			if (format.HasFlag(ResultGridFormat.Url)) {
				ls.Add(new Text(host, new Style(Color.Cyan1,
				                                decoration: Decoration.None, link: url))
				);
			}

			return ls.ToArray();
		}

	}

}