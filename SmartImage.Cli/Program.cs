#nullable disable

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Kantan.Utilities;
using SmartImage.Lib;
using SmartImage.Lib.Searching;
using Novus;
using Kantan;
using Kantan.Net;

namespace SmartImage.Cli;

public static class Program
{
	public static async Task Main(string[] args)
	{
		Console.WriteLine();
		Trace.WriteLine($"");

#if TEST
		Console.WriteLine();
#endif

#if DEBUG

		args = new[]
		{
			@"C:\Users\Deci\Pictures\Test Images\Test6.jpg",
			"https://data17.kemono.party/data/3e/b4/3eb449b10212f019acf07c595b653700a15feec3a7341d5432ebf008f69d6d5f.png?f=17EA29A6-8966-4801-A508-AC89FABE714D.png"
		};

#endif


		ArgumentHandler.Run(args);

		Console.WriteLine(Query);

		var asm = typeof(SearchClient).Assembly.GetName();
		Debug.WriteLine($"{asm.Version}");

		var cts = new CancellationTokenSource();

		bool isComplete = false;

		var tasks           = Client.GetSearchTasks(cts.Token);
		var continueTasks   = new List<Task>();
		var results         = new List<SearchResult>();
		var detailedResults = new List<ImageResult>();
		var filteredResults = new List<SearchResult>();

		SearchCompleted += (sender, eventArgs) =>
		{
			Console.WriteLine($"{eventArgs}");
		};

		ContinueCompleted += (sender, eventArgs) =>
		{
			Console.WriteLine($"{eventArgs}");
		};

		ResultCompleted += (sender, eventArgs) =>
		{
			Console.WriteLine($"{eventArgs}");
		};

		while (!isComplete && !cts.IsCancellationRequested) {
			var finished = await Task.WhenAny(tasks);

			var task = finished.ContinueWith(Client.GetResultContinueCallback, state: cts.Token, cts.Token,
			                                 0, TaskScheduler.Default);

			continueTasks.Add(task);

			SearchResult value = finished.Result;

			tasks.Remove(finished);

			bool? isFiltered;
			bool  isPriority = Config.PriorityEngines.HasFlag(value.Engine.EngineOption);

			//
			//                          Filtering
			//                         /         \
			//                     true           false
			//                     /                 \
			//               IsNonPrimitive         [Results]
			//                /          \
			//              true         false
			//             /               \
			//        [Results]        [FilteredResults]
			//

			if (Config.Filtering) {

				if (value.IsNonPrimitive) {
					results.Add(value);
					detailedResults.Add(value.PrimaryResult);
					isFiltered = false;
				}
				else {
					filteredResults.Add(value);
					isFiltered = true;
				}
			}
			else {
				results.Add(value);
				isFiltered = null;
			}

			//

			// Call event
			ResultCompleted?.Invoke(null, new ResultCompletedEventArgs(value)
			{
				IsFiltered = isFiltered,
				IsPriority = isPriority
			});

			isComplete = !tasks.Any();

		}
	}

	/// <summary>
	///     Fires when <see cref="GetResultContinueCallback" /> returns
	/// </summary>
	public static event EventHandler ContinueCompleted;

	/// <summary>
	///     Fires when a result is returned (<see cref="RunSearchAsync" />).
	/// </summary>
	public static event EventHandler<ResultCompletedEventArgs> ResultCompleted;

	/// <summary>
	///     Fires when a search is complete (<see cref="RunSearchAsync" />).
	/// </summary>
	public static event EventHandler<SearchCompletedEventArgs> SearchCompleted;

	private static readonly SearchConfig Config = new()
	{
		Notification      = true,
		NotificationImage = true,
		SearchEngines     = SearchEngineOptions.All,
		PriorityEngines   = SearchEngineOptions.Auto,
		Filtering         = true,
	};

	private static readonly SearchClient Client = new(Config);

	private static ImageQuery Query => Config.Query;


	/// <summary>
	/// Command line argument handler
	/// </summary>
	private static readonly CliHandler ArgumentHandler = new()
	{
		Parameters =
		{
			new()
			{
				ArgumentCount = 1,
				ParameterId   = "-se",
				Function = strings =>
				{
					Config.SearchEngines = Enum.Parse<SearchEngineOptions>(strings[0]);
					return null;
				}
			},
			new()
			{
				ArgumentCount = 1,
				ParameterId   = "-pe",
				Function = strings =>
				{
					Config.PriorityEngines = Enum.Parse<SearchEngineOptions>(strings[0]);
					return null;
				}
			},
			new()
			{
				ArgumentCount = 0,
				ParameterId   = "-f",
				Function = delegate
				{
					Config.Filtering = true;
					return null;
				}
			},
			new()
			{
				ArgumentCount = 0,
				ParameterId   = "-output_only",
				Function = delegate
				{
					Config.OutputOnly = true;
					return null;
				}
			}
		},
		Default = new()
		{
			ArgumentCount = 1,
			ParameterId   = null,
			Function = strings =>
			{
				Config.Query = strings[0];
				return null;
			}
		}
	};
}