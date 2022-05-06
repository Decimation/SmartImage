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


	public static async Task Main(string[] args)
	{

#if DEBUG
		if (args is not { } || !args.Any()) {
			args = new[]
			{
				"-pe", "SauceNao",
				@"C:\Users\Deci\Pictures\Test Images\Test6.jpg",
				// "https://data17.kemono.party/data/3e/b4/3eb449b10212f019acf07c595b653700a15feec3a7341d5432ebf008f69d6d5f.png?f=17EA29A6-8966-4801-A508-AC89FABE714D.png"
			};

		}
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

		IsComplete = false;

		Tasks = Client.GetSearchTasks(cts.Token);

		ContinueTasks   = new List<Task>();
		Results         = new List<SearchResult>();
		DetailedResults = new List<ImageResult>();
		FilteredResults = new List<SearchResult>();

		SearchCompleted += (sender, eventArgs) => { };

		ContinueCompleted += (sender, eventArgs) => { };

		ResultCompleted += (sender, eventArgs) =>
		{
			HandleResult(eventArgs);

			// if (!eventArgs.Flags.HasFlag(SearchResultFlags.Filtered) && Config.Filtering) { }
		};

		while (!IsComplete && !cts.IsCancellationRequested) {
			var finished = await Task.WhenAny(Tasks);

			Task task = finished.ContinueWith(Client.GetResultContinueCallback, cts, cts.Token,
			                                  TaskContinuationOptions.None, TaskScheduler.Default);

			ContinueTasks.Add(task);

			var value = finished.Result;

			Tasks.Remove(finished);

			if (Config.PriorityEngines.HasFlag(value.Engine.EngineOption)) {
				value.Flags |= SearchResultFlags.Priority;
			}

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
					Results.Add(value);
					DetailedResults.Add(value.PrimaryResult);
				}
				else {
					FilteredResults.Add(value);
					value.Flags |= SearchResultFlags.Filtered;
				}
			}
			else {
				Results.Add(value);
			}

			// Call event
			ResultCompleted?.Invoke(null, value);

			IsComplete = !Tasks.Any();

		}

	}

	#region

	public static List<SearchResult> Results { get; private set; }

	public static List<ImageResult> DetailedResults { get; private set; }

	public static List<SearchResult> FilteredResults { get; private set; }

	public static List<Task> ContinueTasks { get; private set; }

	public static List<Task<SearchResult>> Tasks { get; private set; }

	public static bool IsComplete { get; private set; }

	public static bool HideRaw { get; set; } = true;

	#endregion


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

		if (searchResult.Flags.HasFlag(SearchResultFlags.Priority)) {
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

	public static event EventHandler ContinueCompleted;

	public static event EventHandler<SearchResult> ResultCompleted;

	public static event EventHandler<SearchCompletedEventArgs> SearchCompleted;

	private static readonly Encoding CodePage437 =
		CodePagesEncodingProvider.Instance.GetEncoding((int) Native.CodePages.CP_IBM437);

	private static readonly string CheckMark =
		Strings.EncodingConvert(Encoding.Unicode, CodePage437, Strings.Constants.CHECK_MARK.ToString());

	private static readonly string MulSign = Strings.Constants.MUL_SIGN.ToString();

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
}