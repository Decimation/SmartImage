using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Novus.Utilities;
using SmartImage.Lib.Engines;
using SmartImage.Lib.Searching;

namespace SmartImage.Lib
{
	public class SearchClient
	{
		public SearchClient(SearchConfig config)
		{
			Config = config;

			Engines = GetAllEngines()
				.Where(e => Config.SearchEngines.HasFlag(e.Engine))
				.ToArray();

			if (!Engines.Any()) {
				throw new ArgumentException("No engines specified");
			}

			Tasks = new(Engines.Select(CreateTask));

		}

		public SearchConfig Config { get; init; }

		public bool IsComplete => !Tasks.Any();

		private List<Task<SearchResult>> Tasks { get; }

		public SearchEngine[] Engines { get; }

		private Task<SearchResult> CreateTask(SearchEngine engine)
		{
			return Task.Run(() => engine.GetResult(Config.Query));
		}

		public async Task<SearchResult> Next()
		{
			if (IsComplete) {
				throw new Exception();
			}


			var finished = await Task.WhenAny(Tasks);

			var v = await finished;

			Tasks.Remove(finished);

			return v;
		}

		public static SearchEngine[] GetAllEngines()
		{
			return ReflectionHelper.GetAllImplementationsOfType(typeof(SearchEngine))
				.Select(Activator.CreateInstance)
				.Cast<SearchEngine>()
				.ToArray();
		}
	}
}