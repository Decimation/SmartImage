// Deci SmartImage.Rdx ResultModel.cs
// $File.CreatedYear-$File.CreatedMonth-26 @ 0:50

using SmartImage.Lib.Results;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace SmartImage.Rdx;

public class ResultModel : IDisposable
{

	public SearchResult Result { get; }

	public STable Table { get; }

	public int Id { get; }

	public ResultModel(SearchResult result) : this(result, Interlocked.Increment(ref cnt)) { }

	public ResultModel(SearchResult result, int id)
	{
		Result = result;
		Id     = id;
		Table  = Create();
	}

	public STable Create()
	{
		var table = CreateTable();

		int i = 0;

		foreach (SearchResultItem sri in Result.Results) {
			table.Rows.Add(new IRenderable[]
			{
				new Text($"{i + 1}"),
				Markup.FromInterpolated($"[link={sri.Url}]{sri.Root.Engine.Name} #{i + 1}[/]"),
				Markup.FromInterpolated($"{sri.Similarity}"),
				Markup.FromInterpolated($"{sri.Artist}"),
				Markup.FromInterpolated($"{sri.Description}"),
				Markup.FromInterpolated($"{sri.Character}"),
			});

			i++;
		}

		return table;
	}

	public static int cnt = 0;

	public void Dispose()
	{
		Result.Dispose();
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
	}

}