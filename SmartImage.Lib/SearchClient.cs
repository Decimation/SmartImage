using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Novus.Utilities;
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


			Results = new();
		}

		public SearchConfig Config { get; init; }

		public bool IsComplete { get; private set; }


		public SearchEngine[] Engines { get; }

		public List<SearchResult> Results { get; }


		public void Reset()
		{
			Results.Clear();
			IsComplete = false;
		}

		public async Task RunSearchAsync()
		{
			if (IsComplete) {
				throw new SmartImageException();
			}

			var tasks = new List<Task<SearchResult>>(Engines.Select(e => e.GetResultAsync(Config.Query)));

			while (!IsComplete) {
				var finished = await Task.WhenAny(tasks);

				var value = await finished;

				tasks.Remove(finished);

				Results.Add(value);

				IsComplete = !tasks.Any();
			}

			Trace.WriteLine($"{nameof(SearchClient)}: Search complete");

		}

		public static SearchEngine[] GetAllEngines()
		{
			return ReflectionHelper.GetAllImplementations<SearchEngine>()
				.Select(Activator.CreateInstance)
				.Cast<SearchEngine>()
				.ToArray();
		}
	}
}