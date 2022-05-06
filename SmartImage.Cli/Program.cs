#nullable disable

using System.Data;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using AngleSharp.Css.Values;
using ConsoleTableExt;
using Kantan.Utilities;
using SmartImage.Lib;
using SmartImage.Lib.Searching;
using Novus;
using Kantan;
using Kantan.Cli.Controls;
using Kantan.Net;
using Kantan.Text;
using Color = System.Drawing.Color;

namespace SmartImage.Cli;

public static class Program
{
	private static readonly SearchConfig Config = new()
	{
		Notification      = true,
		NotificationImage = true,
		SearchEngines     = SearchEngineOptions.All,
		PriorityEngines   = SearchEngineOptions.Auto,
		Filtering         = true
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
			new CliParameter
			{
				ArgumentCount = 1,
				ParameterId   = "-se",
				Function = strings =>
				{
					Config.SearchEngines = Enum.Parse<SearchEngineOptions>(strings[0]);
					return null;
				}
			},
			new CliParameter
			{
				ArgumentCount = 1,
				ParameterId   = "-pe",
				Function = strings =>
				{
					Config.PriorityEngines = Enum.Parse<SearchEngineOptions>(strings[0]);
					return null;
				}
			},
			new CliParameter
			{
				ArgumentCount = 0,
				ParameterId   = "-f",
				Function = delegate
				{
					Config.Filtering = true;
					return null;
				}
			},
			new CliParameter
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
		Default = new CliParameter
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

	public static async Task Main(string[] args)
	{

#if DEBUG
		if (args is not { } || !args.Any())
			args = new[]
			{
				// @"C:\Users\Deci\Pictures\Test Images\Test6.jpg",
				"https://data17.kemono.party/data/3e/b4/3eb449b10212f019acf07c595b653700a15feec3a7341d5432ebf008f69d6d5f.png?f=17EA29A6-8966-4801-A508-AC89FABE714D.png"
			};
#endif

		Debug.WriteLine($"{args.QuickJoin()}");

		Init.Setup();
		Console.Title          = Resources.Name;
		Console.OutputEncoding = Encoding.Unicode;


		ArgumentHandler.Run(args);


		Console.WriteLine($"{"Query:".AddColor(Color.LawnGreen)} {Query}");
		Console.WriteLine($"{"Config:".AddColor(Color.Cyan)} {Config}");


		var cts = new CancellationTokenSource();

		bool isComplete = false;

		List<Task<SearchResult>> tasks = Client.GetSearchTasks(cts.Token);

		var continueTasks   = new List<Task>();
		var results         = new List<SearchResult>();
		var detailedResults = new List<ImageResult>();
		var filteredResults = new List<SearchResult>();

		SearchCompleted += (sender, eventArgs) =>
		{
			Console.WriteLine($"Search complete: {eventArgs}");
		};

		ContinueCompleted += (sender, eventArgs) =>
		{
			Console.WriteLine($"Continue complete: {eventArgs}");
		};

		ResultCompleted += (sender, eventArgs) =>
		{
			Console.WriteLine($"Result complete: {eventArgs}");
		};

		while (!isComplete && !cts.IsCancellationRequested) {
			Task<SearchResult> finished = await Task.WhenAny(tasks);

			Task task = finished.ContinueWith(Client.GetResultContinueCallback, cts, cts.Token,
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

		Console.WriteLine();

		foreach (SearchResult searchResult in results) {

			IEnumerable<KeyValuePair<string, string>> dict = searchResult.Data.Where(x =>
			{
				object o = x.Value;
				return o is { };
			}).Select(kv =>
			{
				(string s, object o) = kv;
				string value = o.ToString();
				string key   = s.AddColor(Color.LawnGreen);
				return new KeyValuePair<string, string>(key, value);
			});

			var ct = dict.Select(x => new[] { x.Key, x.Value }).ToList();
			var dv = ConsoleTableBuilder.From(ct);
			dv.ExportAndWriteLine();
			
			Console.WriteLine();
		}
	}
	
	public static event EventHandler ContinueCompleted;
	
	public static event EventHandler<ResultCompletedEventArgs> ResultCompleted;
	
	public static event EventHandler<SearchCompletedEventArgs> SearchCompleted;
}