using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Newtonsoft.Json.Linq;
using Novus.Utilities;
using SimpleCore.Net;
using SmartImage.Lib.Engines;
using SmartImage.Lib.Searching;
using SmartImage.Lib.Utilities;

namespace SmartImage.Lib
{
	public sealed class SearchClient
	{
		public SearchClient(SearchConfig config)
		{
			Config = config;

			Engines = GetAllEngines()
				.Where(e => Config.SearchEngines.HasFlag(e.Engine))
				.ToArray();

			if (!Engines.Any()) {
				throw new SmartImageException("No engines specified");
			}


			Results = new List<SearchResult>();
		}

		public SearchConfig Config { get; init; }

		public bool IsComplete { get; private set; }


		public BaseSearchEngine[] Engines { get; }

		public List<SearchResult> Results { get; }


		public void Reset()
		{
			Results.Clear();
			IsComplete = false;
		}

		public static string ResolveDirectLink(string s)
		{
			//todo
			string d = "";

			try {
				var     uri  = new Uri(s);
				string host = uri.Host;


				var doc  = new HtmlDocument();
				var html = Network.GetSimpleResponse(s);

				if (host.Contains("danbooru")) {
					Debug.WriteLine("danbooru");


					var jObject = JObject.Parse(html.Content);

					d = (string) jObject["file_url"]!;


					return d;
				}

				doc.LoadHtml(html.Content);

				string sel = "//img";

				var nodes = doc.DocumentNode.SelectNodes(sel);

				if (nodes == null) {
					return null;
				}

				Debug.WriteLine($"{nodes.Count}");
				Debug.WriteLine($"{nodes[0]}");


			}
			catch (Exception e) {
				Debug.WriteLine($"direct {e.Message}");
				return d;
			}


			return d;
		}

		public event EventHandler<SearchResultEventArgs> ResultCompleted;

		public class SearchResultEventArgs : EventArgs
		{
			public SearchResult Result { get; }

			public SearchResultEventArgs(SearchResult result)
			{
				Result = result;
			}
		}

		public async Task RunSearchAsync()
		{
			if (IsComplete) {
				Reset();
			}

			var tasks = new List<Task<SearchResult>>(Engines.Select(e => e.GetResultAsync(Config.Query)));

			while (!IsComplete) {
				var finished = await Task.WhenAny(tasks);

				var value = await finished;

				tasks.Remove(finished);

				Results.Add(value);

				// Call event
				ResultCompleted?.Invoke(this, new SearchResultEventArgs(value));

				IsComplete = !tasks.Any();
			}

			Trace.WriteLine($"[success] {nameof(SearchClient)}: Search complete");

		}

		public static BaseSearchEngine[] GetAllEngines()
		{
			return ReflectionHelper.GetAllImplementations<BaseSearchEngine>()
				.Select(Activator.CreateInstance)
				.Cast<BaseSearchEngine>()
				.ToArray();
		}
	}
}