#nullable disable

using System.Data;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.RegularExpressions;
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

		SearchCompleted += (sender, eventArgs) => { };

		ContinueCompleted += (sender, eventArgs) => { };

		ResultCompleted += (sender, eventArgs) =>
		{
			HandleResult(eventArgs.Result);
		};

		while (!isComplete && !cts.IsCancellationRequested) {
			var finished = await Task.WhenAny(tasks);

			Task task = finished.ContinueWith(Client.GetResultContinueCallback, cts, cts.Token,
			                                  0, TaskScheduler.Default);

			continueTasks.Add(task);

			var value = finished.Result;

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

	}

	private static void HandleResult(SearchResult searchResult)
	{
		var dict = searchResult.Data.Where(x =>
		{
			object o = x.Value;
			return o is { };
		}).Select(kv =>
		{
			(string s, object o) = kv;
			string value = o.ToString();
			// string key   = s.AddColor(Color.LawnGreen);
			var key = s;
			return new KeyValuePair<string, string>(key, value);
		});


		/*var dt = new DataTable()
		{
			Columns = { "Property", "Value" }
		};*/

		/*var tableData = new List<List<object>>
		{
			new List<object>{ "Sakura Yamamoto", "Support Engineer", "London", 46},
			new List<object>{ "Serge Baldwin", "Data Coordinator", "San Francisco", 28, "something else" },
			new List<object>{ "Shad Decker", "Regional Director", "Edinburgh"},
		};*/

		/*foreach ((string key, string value) in dict) {

			// dt.Rows.Add(key, value);
			var v = value;

			if (Strings.StringWraps(value)) {
				// v = v.Truncate();//todo
				// v = WordWrap(value, Console.BufferWidth).QuickJoin(Environment.NewLine);
				Debug.WriteLine($"wraps: {value}");
				continue;
			}
			else {
				v = value;
			}

			dt.Rows.Add(key, v);
		}*/

		var vvv = dict.Where(k => k.Value is not { } s || !Strings.StringWraps(s))
		              .Select(x => new List<object>() { x.Key, x.Value.ToString() })
		              .ToList();
		var ctb = ConsoleTableBuilder.From(vvv);

		// var ctb = ConsoleTableBuilder.From(dt);

		ctb.WithCharMapDefinition(CharMapDefinition.FramePipDefinition)
		   .WithMetadataRow(MetaRowPositions.Bottom, builder =>
		   {
			   var sb = new StringBuilder();

			   if (searchResult is { RawUri: { } }) {
				   sb.Append($"Raw: {searchResult.RawUri}");
			   }

			   sb.AppendLine();

			   return sb.ToString();
		   })
		   /*.WithTextAlignment(new Dictionary<int, TextAligntment>
			   {
				   { 0, TextAligntment.Center },
				   { 1, TextAligntment.Right },
				   { 3, TextAligntment.Right },
				   { 100, TextAligntment.Right }
			   })
			   .WithMinLength(new Dictionary<int, int>
			   {
				   { 1, 30 }
			   })*/
		   .WithTitle(searchResult.Engine.Name,
		              ConsoleColor.White,
		              searchResult.IsStatusSuccessful ? ConsoleColor.Blue : ConsoleColor.Red,
		              TextAligntment.Center)
		   /*.WithFormatter(1, (text) =>
			   {
				   return text.ToUpper().Replace(" ", "-") + " «";
			   })*/
		   .ExportAndWriteLine();
	}

	public static event EventHandler ContinueCompleted;

	public static event EventHandler<ResultCompletedEventArgs> ResultCompleted;

	public static event EventHandler<SearchCompletedEventArgs> SearchCompleted;

	/// <summary>
	///     Writes the specified data, followed by the current line terminator, to the standard output stream, while wrapping lines that would otherwise break words.
	/// </summary>
	/// <param name="paragraph">The value to write.</param>
	/// <param name="tabSize">The value that indicates the column width of tab characters.</param>
	public static IEnumerable<string> WriteLineWordWrap(string paragraph, int tabSize = 8)
	{
		string[] lines = paragraph
		                 .Replace("\t", new String(' ', tabSize))
		                 .Split(new string[] { Environment.NewLine }, StringSplitOptions.None);

		for (int i = 0; i < lines.Length; i++) {
			string       process = lines[i];
			List<String> wrapped = new List<string>();

			while (process.Length > Console.WindowWidth) {
				int wrapAt = process.LastIndexOf(' ', Math.Min(Console.WindowWidth - 1, process.Length));
				if (wrapAt <= 0) break;

				wrapped.Add(process.Substring(0, wrapAt));
				process = process.Remove(0, wrapAt + 1);
			}

			foreach (string wrap in wrapped) {
				yield return wrap;
			}


		}
	}

	static IEnumerable<string> WordWrap(string text, int width)
	{

		var forcedZones = Regex.Matches(text, @"\n").Cast<Match>().ToList();
		var normalZones = Regex.Matches(text, @"\s+|(?<=[-,.;])|$").Cast<Match>().ToList();

		int start = 0;

		while (start < text.Length) {
			var zone =
				forcedZones.Find(z => z.Index >= start && z.Index <= start + width) ??
				normalZones.FindLast(z => z.Index >= start && z.Index <= start + width);

			if (zone == null) {
				yield return text.Substring(start, width);
				start += width;
			}
			else {
				yield return text.Substring(start, zone.Index - start);
				start = zone.Index + zone.Length;
			}
		}
	}
}