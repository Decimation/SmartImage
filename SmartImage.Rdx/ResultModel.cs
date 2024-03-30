// Deci SmartImage.Rdx ResultModel.cs
// $File.CreatedYear-$File.CreatedMonth-26 @ 0:50

using SmartImage.Lib.Results;
using Spectre.Console;

namespace SmartImage.Rdx;

public class ResultModel : IDisposable
{
	public SearchResult Result { get; }

	// public STable Table { get; }

	public int Id { get; }

	public ResultModel(SearchResult result)
		: this(result, Interlocked.Increment(ref Count)) { }

	public ResultModel(SearchResult result, int id)
	{
		Result = result;
		Id     = id;

		// Table  = Create();
	}

	private protected static int Count = 0;

	public void Dispose()
	{
		Result.Dispose();
	}

}