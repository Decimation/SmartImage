using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using JetBrains.Annotations;
using SimpleCore.Utilities;
using SmartImage.Searching;

// ReSharper disable IdentifierTypo

namespace SmartImage
{
	/// <summary>
	/// Search config
	/// 
	/// </summary>
	/// <remarks>
	/// Config is read from config file (<see cref="RuntimeInfo.ConfigLocation"/>) or from specified arguments
	/// </remarks>
	public sealed class SearchConfig
	{
		private const string CFG_IMGUR_APIKEY = "imgur_client_id";
		private const string CFG_SAUCENAO_APIKEY = "saucenao_key";
		private const string CFG_SEARCH_ENGINES = "search_engines";
		private const string CFG_PRIORITY_ENGINES = "priority_engines";

		private SearchConfig()
		{
			// create cfg with default options if it doesn't exist
			if (!File.Exists(RuntimeInfo.ConfigLocation)) {
				var f = File.Create(RuntimeInfo.ConfigLocation);
				f.Close();
			}

			var cfgFromFileMap = ExplorerSystem.ReadMap(RuntimeInfo.ConfigLocation);

			// EnginesStr = Read<string>(CFG_SEARCH_ENGINES, cfgFromFileMap);
			// PriorityEnginesStr = Read<string>(CFG_PRIORITY_ENGINES, cfgFromFileMap);
			Engines = ReadMapKeyValue<SearchEngines>(CFG_SEARCH_ENGINES, cfgFromFileMap);
			PriorityEngines = ReadMapKeyValue<SearchEngines>(CFG_PRIORITY_ENGINES, cfgFromFileMap);
			ImgurAuth = ReadMapKeyValue<string>(CFG_IMGUR_APIKEY, cfgFromFileMap);
			SauceNaoAuth = ReadMapKeyValue<string>(CFG_SAUCENAO_APIKEY, cfgFromFileMap);
		}


		/// <summary>
		///     User config and arguments
		/// </summary>
		public static SearchConfig Config { get; private set; } = new SearchConfig();

		public bool NoArguments { get; set; }

		public SearchEngines Engines { get; set; }

		public SearchEngines PriorityEngines { get; set; }

		public string ImgurAuth { get; set; }


		public string SauceNaoAuth { get; set; }


		public bool UpdateConfig { get; set; }

		public string Image { get; set; }


		public IDictionary<string, string> ToMap()
		{
			var m = new Dictionary<string, string>
			{
				{CFG_SEARCH_ENGINES, Engines.ToString()},
				{CFG_PRIORITY_ENGINES, PriorityEngines.ToString()},
				{CFG_IMGUR_APIKEY, ImgurAuth},
				{CFG_SAUCENAO_APIKEY, SauceNaoAuth}
			};

			return m;
		}

		public void Reset()
		{
			Engines = SearchEngines.All;
			PriorityEngines = SearchEngines.SauceNao;
			ImgurAuth = null;
			SauceNaoAuth = null;
		}


		public static SearchConfig Update(SearchConfig cfgFromCli, SearchConfig cfgFromFile)
		{
			// todo: find a more sustainable way of doing this
			// todo: use reflection

			if (cfgFromCli.Engines == default) {
				cfgFromCli.Engines = cfgFromFile.Engines;
			}

			if (cfgFromCli.PriorityEngines == default) {
				cfgFromCli.PriorityEngines = cfgFromFile.PriorityEngines;
			}

			if (String.IsNullOrWhiteSpace(cfgFromCli.ImgurAuth)) {
				cfgFromCli.ImgurAuth = cfgFromFile.ImgurAuth;
			}

			if (String.IsNullOrWhiteSpace(cfgFromCli.SauceNaoAuth)) {
				cfgFromCli.SauceNaoAuth = cfgFromFile.SauceNaoAuth;
			}

			return cfgFromCli;
		}

		// todo: update cfg file

		internal void WriteToFile()
		{
			ExplorerSystem.WriteMap(ToMap(), RuntimeInfo.ConfigLocation);
			CliOutput.WriteInfo("wrote to {0}", RuntimeInfo.ConfigLocation);
		}

		private static void WriteMapKeyValue<T>(string name, T value, IDictionary<string, string> cfg)
		{
			string? valStr = value.ToString();

			if (!cfg.ContainsKey(name)) {
				cfg.Add(name, valStr);
			}
			else {
				cfg[name] = valStr;
			}

			//Update();
		}


		private static T ReadMapKeyValue<T>(string name,
			IDictionary<string, string> cfg,
			bool setDefaultIfNull = false,
			T defaultValue = default)
		{
			if (!cfg.ContainsKey(name)) cfg.Add(name, String.Empty);
			//Update();

			string rawValue = cfg[name];

			if (setDefaultIfNull && String.IsNullOrWhiteSpace(rawValue)) {
				WriteMapKeyValue(name, defaultValue.ToString(), cfg);
				rawValue = ReadMapKeyValue<string>(name, cfg);
			}

			var parse = Utilities.Read<T>(rawValue);
			return parse;
		}

		public override string ToString()
		{
			var sb = new StringBuilder();

			sb.AppendFormat("Search engines: {0}\n", Engines);
			sb.AppendFormat("Priority engines: {0}\n", PriorityEngines);
			sb.AppendFormat("Imgur auth: {0}\n", ImgurAuth);
			sb.AppendFormat("SauceNao auth: {0}\n", SauceNaoAuth);
			sb.AppendFormat("Image: {0}\n", Image);

			return sb.ToString();
		}


		public static void Cleanup()
		{
			if (Config.UpdateConfig) {
				CliOutput.WriteInfo("Updating cfg");
				Config.WriteToFile();
			}
		}

		/// <summary>
		/// Parse config arguments and options
		/// </summary>
		/// <param name="args">Command line arguments</param>
		public static void ReadSearchConfigArgs(string[] args)
		{
			bool noArgs = args == null || args.Length == 0;

			if (noArgs) {
				Config.NoArguments = true;
				return;
			}

			var queue = new Queue<string>(args);
			using var qe = queue.GetEnumerator();

			while (qe.MoveNext()) {
				string e = qe.Current;

				// todo: structure

				switch (e) {
					case "--search-engines":
						qe.MoveNext();
						string sestr = qe.Current;
						Config.Engines = Utilities.Read<SearchEngines>(sestr);
						break;
					case "--priority-engines":
						qe.MoveNext();
						string pestr = qe.Current;
						Config.PriorityEngines = Utilities.Read<SearchEngines>(pestr);
						break;
					case "--saucenao-auth":
						qe.MoveNext();
						string snastr = qe.Current;
						Config.SauceNaoAuth = snastr;
						break;
					case "--imgur-auth":
						qe.MoveNext();
						string imastr = qe.Current;
						Config.ImgurAuth = imastr;
						break;
					// case "--auto-exit":
					// 	qe.MoveNext();
					// 	SearchConfig.Config.AutoExit = true;
					// 	break;
					case "--update-cfg":
						qe.MoveNext();
						Config.UpdateConfig = true;
						break;


					default:
						Config.Image = e;
						break;
				}
			}
		}
	}
}