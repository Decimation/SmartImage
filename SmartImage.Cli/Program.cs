#nullable disable

using System.Collections;
using System.Data;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Dynamic;
using System.Text;
using System.Text.RegularExpressions;
using AngleSharp.Css.Values;
using ConsoleTableExt;
using Kantan.Utilities;
using SmartImage.Lib;
using SmartImage.Lib.Searching;
using Novus;
using Kantan;
using Kantan.Cli;
using Kantan.Cli.Controls;
using Kantan.Net;
using Kantan.Text;
using Novus.OS.Win32;
using Color = System.Drawing.Color;

namespace SmartImage.Cli;

public static class Program
{
	private const int WIDTH  = 120;
	private const int HEIGHT = 250;

	#region

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

	#endregion


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
				@"C:\Users\Deci\Pictures\Test Images\Test6.jpg",
				// "https://data17.kemono.party/data/3e/b4/3eb449b10212f019acf07c595b653700a15feec3a7341d5432ebf008f69d6d5f.png?f=17EA29A6-8966-4801-A508-AC89FABE714D.png"
			};
#endif

		Debug.WriteLine($"{args.QuickJoin()}");

		Init.Setup();

		Console.Title          = Resources.Name;
		Console.OutputEncoding = Encoding.Unicode;
		ConsoleManager.Init();

		// Console.Clear();


		ArgumentHandler.Run(args);


		Console.WriteLine($"{"Query:".AddColor(Color.LawnGreen)} {Query}");

		var cfglist = Config.ToMap().Select(x => new List<object> { x.Key, x.Value.ToString() })
		                    .ToList();

		var ctb = ConsoleTableBuilder.From(cfglist);

		ctb.WithCharMapDefinition(CharMapDefinition.FramePipDefinition)
		   .WithTitle($"Config", ConsoleColor.White, ConsoleColor.Magenta, TextAligntment.Center)
		   .ExportAndWriteLine();

		var cts = new CancellationTokenSource();

		_isComplete = false;

		_tasks = Client.GetSearchTasks(cts.Token);

		_continueTasks   = new List<Task>();
		_results         = new List<SearchResult>();
		_detailedResults = new List<ImageResult>();
		_filteredResults = new List<SearchResult>();

		SearchCompleted += (sender, eventArgs) => { };

		ContinueCompleted += (sender, eventArgs) => { };

		ResultCompleted += (sender, eventArgs) =>
		{
			if (!eventArgs.Flags.HasFlag(SearchResultFlags.Filtered)) {
				HandleResult(eventArgs);

			}
		};

		while (!_isComplete && !cts.IsCancellationRequested) {
			var finished = await Task.WhenAny(_tasks);

			Task task = finished.ContinueWith(Client.GetResultContinueCallback, cts, cts.Token,
			                                  0, TaskScheduler.Default);

			_continueTasks.Add(task);

			var value = finished.Result;

			_tasks.Remove(finished);

			bool? isFiltered;
			bool  isPriority = Config.PriorityEngines.HasFlag(value.Engine.EngineOption);
			value.Flags |= SearchResultFlags.Priority;

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
					_results.Add(value);
					_detailedResults.Add(value.PrimaryResult);
				}
				else {
					_filteredResults.Add(value);
					value.Flags |= SearchResultFlags.Filtered;
				}
			}
			else {
				_results.Add(value);
			}

			// Call event
			ResultCompleted?.Invoke(null, value);

			_isComplete = !_tasks.Any();

		}

		Console.WriteLine();

	}

	private static readonly Encoding CodePage437 =
		CodePagesEncodingProvider.Instance.GetEncoding((int) Native.CodePages.CP_IBM437);

	private static readonly string CheckMark =
		Strings.EncodingConvert(Encoding.Unicode, CodePage437, Strings.Constants.CHECK_MARK.ToString());

	private static readonly string MulSign = Strings.Constants.MUL_SIGN.ToString();

	#region

	private static List<SearchResult>       _results;
	private static List<ImageResult>        _detailedResults;
	private static List<SearchResult>       _filteredResults;
	private static List<Task>               _continueTasks;
	private static List<Task<SearchResult>> _tasks;
	private static bool                     _isComplete;

	#endregion

	public static bool HideRaw { get; set; } = true;

	public static Dictionary<string, object> getmap(SearchResult s)
	{
		var map = new Dictionary<string, object>();

		// map.Add(nameof(PrimaryResult), PrimaryResult);

		foreach ((string key, object value) in s.PrimaryResult.Data) {
			map.Add(key, value);
		}

		// map.Add("Raw", s.RawUri);

		if (s.OtherResults.Count != 0) {
			map.Add("Other image results", s.OtherResults.Count);
		}

		if (s.ErrorMessage != null) {
			map.Add("Error", s.ErrorMessage);
		}

		if (!s.IsStatusSuccessful) {
			map.Add(nameof(SearchResult.Status), s.Status);
		}

		map.Add(nameof(SearchResult.Flags), s.Flags);

		return map;
	}


	private static void HandleResult(SearchResult searchResult)
	{
		// var data = getmap(searchResult);
		var data = searchResult.Data;


		var dict = data.Where(x => x.Value is { })
		               .Select(kv => new KeyValuePair<string, string>(kv.Key, kv.Value.ToString()));

		var dictList = dict.Where(k => (k.Value != null))
		                   .Select(x => new List<object>
			                           { $"{x.Key}", x.Value.ToString().Truncate(Console.BufferWidth - 30) })
		                   .ToList();

		var ctb = ConsoleTableBuilder.From(dictList);

		var titleColor = (searchResult.IsStatusSuccessful ? ConsoleColor.Blue : ConsoleColor.DarkRed);

		if (searchResult is { Flags: SearchResultFlags.Priority }) {
			titleColor = ConsoleColor.Green;
		}


		ctb.WithCharMapDefinition(CharMapDefinition.FramePipDefinition)
		   /*.WithMetadataRow(MetaRowPositions.Top, builder =>
		   {
			   var sb = new StringBuilder();
	
			   if (searchResult is { Flags: SearchResultFlags.Filtered }) sb.Append($"- ");
			   if (searchResult is { Flags: SearchResultFlags.Priority }) sb.Append($"! ");
	
			   // sb.AppendLine();
	
			   return sb.ToString();
		   })*/
		   .WithMetadataRow(MetaRowPositions.Bottom, builder =>
		   {
			   var sb = new StringBuilder();

			   // if (searchResult is { RawUri: { } }) 

			   if (!HideRaw) {
				   sb.Append($"Raw: {searchResult.RawUri}");
			   }

			   sb.AppendLine();

			   return sb.ToString();

			   // return searchResult?.RawUri?.ToString();
		   })
		   .WithTitle(searchResult.Engine.Name, ConsoleColor.White, titleColor, TextAligntment.Center)
		   .ExportAndWriteLine();
	}

	public static event EventHandler ContinueCompleted;

	public static event EventHandler<SearchResult> ResultCompleted;

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